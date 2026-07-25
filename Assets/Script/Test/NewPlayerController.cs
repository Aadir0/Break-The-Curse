using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// NOTE: Requires the "Input System" package (com.unity.inputsystem) and
// Project Settings > Player > Active Input Handling set to "Input System Package (New)"
// or "Both".
[RequireComponent(typeof(Rigidbody2D))]
public class NewPlayerController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Movement")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float groundCoyoteTime = 0.1f;

    [Header("Wall Movement")]
    [Tooltip("How long the player must be touching a wall before they start sliding down it.")]
    [SerializeField] private float wallHoldDuration = 0.15f;
    [Tooltip("Max downward speed while sliding on a wall.")]
    [SerializeField] private float wallFallSpeed = 3f;
    [Tooltip("Horizontal force applied when jumping away from a wall.")]
    [SerializeField] private float wallJumpThrowbackForce = 4f;
    [Tooltip("How long horizontal input is ignored after a wall jump, so the throwback isn't cancelled out.")]
    [SerializeField] private float wallJumpControlLockDuration = 0.15f;
    [Tooltip("Grace period after leaving a wall during which a wall jump is still allowed.")]
    [SerializeField] private float wallJumpCoyoteTime = 0.1f;
    [SerializeField] private int wallJumpAnimationRestartBeforeFrame = 3;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private GameObject deathCanvas;
    [SerializeField] private GameObject bloodEffectPrefab;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip runningClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip deathClip;

    #endregion

    #region Constants

    private const float GroundedLockAfterJumpDuration = 0.12f;

    #endregion

    #region Runtime State

    private readonly HashSet<Collider2D> groundContacts = new HashSet<Collider2D>();
    private readonly Dictionary<Collider2D, float> wallContacts = new Dictionary<Collider2D, float>();

    private bool moveRight = true;
    public float currentMoveInput;
    private float scrollMoveInput;

    private float wallSlideHoldTimer;
    private float wallJumpControlLockTimer;
    private float wallJumpCoyoteTimer;
    private float groundCoyoteTimer;
    private float jumpGroundLockTimer;
    private float lastWallNormalX;

    private bool jumpRequested;
    private bool restartedJumpAnimationOnWallContact;
    private bool isPlayingRunSound;

    private static readonly int RunParam = Animator.StringToHash("run");
    private static readonly int GroundedParam = Animator.StringToHash("grounded");
    private static readonly int JumpTrigger = Animator.StringToHash("jump");
    private static readonly int DieTrigger = Animator.StringToHash("die");
    private static readonly int JumpStateHashLower = Animator.StringToHash("jump");
    private static readonly int JumpStateHashUpper = Animator.StringToHash("Jump");

    public bool IsGrounded => groundContacts.Count > 0;
    private bool IsTouchingWall => wallContacts.Count > 0;
    public bool IsDead { get; private set; }

    #endregion

    #region Input System

    private InputAction moveLeftAction;
    private InputAction moveRightAction;
    private InputAction jumpAction;
    private InputAction resetScrollAction;
    private InputAction scrollAction;

    private void SetupInputActions()
    {
        moveLeftAction = new InputAction("MoveLeft", InputActionType.Button, "<Keyboard>/a");
        moveRightAction = new InputAction("MoveRight", InputActionType.Button, "<Keyboard>/d");
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        resetScrollAction = new InputAction("ResetScroll", InputActionType.Button, "<Mouse>/middleButton");
        scrollAction = new InputAction("Scroll", InputActionType.Value, "<Mouse>/scroll/y");

        jumpAction.performed += OnJumpPerformed;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        jumpRequested = true;
    }

    private void OnEnable()
    {
        moveLeftAction?.Enable();
        moveRightAction?.Enable();
        jumpAction?.Enable();
        resetScrollAction?.Enable();
        scrollAction?.Enable();
    }

    private void OnDisable()
    {
        moveLeftAction?.Disable();
        moveRightAction?.Disable();
        jumpAction?.Disable();
        resetScrollAction?.Disable();
        scrollAction?.Disable();
    }

    private void OnDestroy()
    {
        jumpAction.performed -= OnJumpPerformed;

        moveLeftAction?.Dispose();
        moveRightAction?.Dispose();
        jumpAction?.Dispose();
        resetScrollAction?.Dispose();
        scrollAction?.Dispose();
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        SetupInputActions();
    }

    private void Update()
    {
        if (IsDead)
        {
            HandleDeadState();
            return;
        }

        if (Time.timeScale == 0f)
        {
            HandlePausedState();
            return;
        }

        jumpGroundLockTimer = Mathf.Max(0f, jumpGroundLockTimer - Time.deltaTime);

        ReadMovementInput();
        UpdateFacing();
        UpdateAnimatorState();
        HandleRunningAudio();
    }

    private void FixedUpdate()
    {
        if (IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.timeScale == 0f)
        {
            return;
        }

        TickTimers(Time.fixedDeltaTime);
        HandleJumpRequest();

        // Preserve the wall-jump throwback velocity for a short window instead
        // of immediately overwriting it with normal horizontal movement.
        if (wallJumpControlLockTimer > 0f)
        {
            return;
        }

        if (!HandleWallSlide())
        {
            ApplyHorizontalMovement();
        }
    }

    private void TickTimers(float deltaTime)
    {
        wallJumpControlLockTimer = Mathf.Max(0f, wallJumpControlLockTimer - deltaTime);
        wallJumpCoyoteTimer = Mathf.Max(0f, wallJumpCoyoteTimer - deltaTime);
        groundCoyoteTimer = Mathf.Max(0f, groundCoyoteTimer - deltaTime);
    }

    private void HandleDeadState()
    {
        currentMoveInput = 0f;
        jumpRequested = false;
        StopRunningSound();
    }

    private void HandlePausedState()
    {
        currentMoveInput = 0f;
        jumpRequested = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        anim.SetBool(RunParam, false);
        anim.SetBool(GroundedParam, false);
    }

    #endregion

    #region Movement & Facing

    private void ReadMovementInput()
    {
        if (resetScrollAction.WasPressedThisFrame())
        {
            scrollMoveInput = 0f;
        }

        float scrollDelta = scrollAction.ReadValue<float>();

        if (scrollDelta > 0.01f)
        {
            scrollMoveInput = 1f;
        }
        else if (scrollDelta < -0.01f)
        {
            scrollMoveInput = -1f;
        }

        // Keyboard input takes priority over scroll-wheel input.
        if (moveLeftAction.IsPressed())
        {
            currentMoveInput = -1f;
        }
        else if (moveRightAction.IsPressed())
        {
            currentMoveInput = 1f;
        }
        else
        {
            currentMoveInput = scrollMoveInput;
        }
    }

    private void UpdateFacing()
    {
        if (currentMoveInput > 0.01f && !moveRight)
        {
            Flip();
        }
        else if (currentMoveInput < -0.01f && moveRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        moveRight = !moveRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1f;
        transform.localScale = scaler;
    }

    private void ApplyHorizontalMovement()
    {
        wallSlideHoldTimer = 0f;
        rb.linearVelocity = new Vector2(currentMoveInput * speed, rb.linearVelocity.y);
    }

    #endregion

    #region Jumping

    private void HandleJumpRequest()
    {
        if (!jumpRequested)
        {
            return;
        }

        jumpRequested = false;

        if (IsGrounded || CanCoyoteJump())
        {
            PerformGroundJump();
            return;
        }

        // Wall jump only fires if the player holds the direction that leads
        // AWAY from the wall while pressing jump. Holding INTO the wall (the
        // same side the wall is on) does nothing at all here - no boost, no
        // jump - so the only way off is to hold away + jump, or wait out the
        // hold timer and slide down.
        if (CanWallJump() && TryGetWallJumpDirection(out float wallJumpDirection))
        {
            PerformWallJump(wallJumpDirection);
        }
    }

    private bool CanCoyoteJump()
    {
        return groundCoyoteTimer > 0f;
    }

    private bool CanWallJump()
    {
        if (IsGrounded)
        {
            return false;
        }

        return HasWallForJump();
    }

    /// <summary>
    /// Reads whichever direction key is currently held and checks that it
    /// points away from the wall (matches the wall's outward normal). If the
    /// player is holding INTO the wall, or holding nothing, this returns
    /// false and no wall jump should occur.
    /// </summary>
    private bool TryGetWallJumpDirection(out float direction)
    {
        direction = 0f;

        float heldDirection;
        if (moveLeftAction.IsPressed())
        {
            heldDirection = -1f;
        }
        else if (moveRightAction.IsPressed())
        {
            heldDirection = 1f;
        }
        else
        {
            return false;
        }

        float wallNormalX = GetWallNormalX();
        bool pointsAwayFromWall = Mathf.Sign(heldDirection) == Mathf.Sign(wallNormalX);

        if (!pointsAwayFromWall)
        {
            return false;
        }

        direction = heldDirection;
        return true;
    }

    private void PerformGroundJump()
    {
        anim.ResetTrigger(JumpTrigger);
        anim.Play(JumpStateHashLower, 0, 0f);

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        groundContacts.Clear();
        groundCoyoteTimer = 0f;
        jumpGroundLockTimer = GroundedLockAfterJumpDuration;
        wallSlideHoldTimer = 0f;
        restartedJumpAnimationOnWallContact = false;

        StopRunningSound();
        PlayJumpSound();
    }

    private void PerformWallJump(float jumpDirection)
    {
        // Face the direction we're jumping toward. Without this the player
        // visually keeps facing the wall while being pushed away from it.
        bool jumpingRight = jumpDirection > 0f;

        if (jumpingRight != moveRight)
        {
            Flip();
        }

        float throwbackForce = Mathf.Min(wallJumpThrowbackForce, jumpForce * 0.9f);

        rb.linearVelocity = new Vector2(jumpDirection * throwbackForce, jumpForce);

        wallSlideHoldTimer = 0f;
        wallJumpControlLockTimer = wallJumpControlLockDuration;
        wallJumpCoyoteTimer = 0f;
        restartedJumpAnimationOnWallContact = false;

        StopRunningSound();
        PlayJumpSound();
    }

    #endregion

    #region Wall Slide

    /// <summary>
    /// Handles sliding down a wall while airborne. Returns true if wall-slide
    /// logic drove the rigidbody velocity this step (so normal horizontal
    /// movement should be skipped).
    /// </summary>
    private bool HandleWallSlide()
    {
        if (IsGrounded || !IsTouchingWall)
        {
            wallSlideHoldTimer = 0f;
            return false;
        }

        float wallNormalX = GetWallNormalX();
        float verticalSpeed = rb.linearVelocity.y;

        if (verticalSpeed <= 0f)
        {
            // Player isn't moving upward: they cling to the wall briefly
            // before gravity is allowed to take over (clamped to wallFallSpeed).
            wallSlideHoldTimer += Time.fixedDeltaTime;
            verticalSpeed = wallSlideHoldTimer < wallHoldDuration
                ? 0f
                : Mathf.Max(verticalSpeed, -wallFallSpeed);
        }
        else
        {
            // Still moving upward (e.g. just jumped) - no slide yet.
            wallSlideHoldTimer = 0f;
        }

        // Let the player peel off the wall if they push away from it;
        // otherwise keep them pinned against it while sliding.
        bool pushingAwayFromWall = Mathf.Abs(currentMoveInput) > 0.01f
            && Mathf.Sign(currentMoveInput) == Mathf.Sign(wallNormalX);
        float horizontalSpeed = pushingAwayFromWall ? currentMoveInput * speed : 0f;

        rb.linearVelocity = new Vector2(horizontalSpeed, verticalSpeed);
        return true;
    }

    private float GetWallNormalX()
    {
        if (IsTouchingWall)
        {
            foreach (float wallNormalX in wallContacts.Values)
            {
                return wallNormalX;
            }
        }

        if (wallJumpCoyoteTimer > 0f && Mathf.Abs(lastWallNormalX) > 0.01f)
        {
            return lastWallNormalX;
        }

        return 0f;
    }

    private bool HasWallForJump()
    {
        return Mathf.Abs(GetWallNormalX()) > 0.01f;
    }

    #endregion

    #region Animation

    private void UpdateAnimatorState()
    {
        anim.SetBool(RunParam, Mathf.Abs(currentMoveInput) > 0.01f);
        anim.SetBool(GroundedParam, IsGrounded && jumpGroundLockTimer <= 0f);
    }

    private void TryRestartJumpAnimationFromWallContact()
    {
        if (restartedJumpAnimationOnWallContact || IsGrounded || rb.linearVelocity.y <= 0f)
        {
            return;
        }

        AnimatorStateInfo currentState = anim.GetCurrentAnimatorStateInfo(0);
        bool isJumpState = currentState.shortNameHash == JumpStateHashLower
            || currentState.shortNameHash == JumpStateHashUpper;

        if (!isJumpState || !ShouldRestartJumpAnimation(currentState))
        {
            return;
        }

        anim.Play(currentState.shortNameHash, 0, 0f);
        restartedJumpAnimationOnWallContact = true;
    }

    private bool ShouldRestartJumpAnimation(AnimatorStateInfo currentState)
    {
        AnimatorClipInfo[] clipInfos = anim.GetCurrentAnimatorClipInfo(0);

        if (clipInfos == null || clipInfos.Length == 0 || clipInfos[0].clip == null)
        {
            return false;
        }

        AnimationClip clip = clipInfos[0].clip;
        float clipProgress = currentState.normalizedTime;

        if (clip.length > 0f)
        {
            clipProgress = currentState.normalizedTime * clip.length * clip.frameRate;
        }

        int currentFrame = Mathf.FloorToInt(clipProgress);
        return currentFrame < wallJumpAnimationRestartBeforeFrame;
    }

    #endregion

    #region Audio

    private void HandleRunningAudio()
    {
        bool shouldPlayRunSound = IsGrounded && Mathf.Abs(currentMoveInput) > 0.01f;

        if (shouldPlayRunSound)
        {
            if (!isPlayingRunSound && runningClip != null)
            {
                audioSource.clip = runningClip;
                audioSource.loop = true;
                audioSource.Play();
                isPlayingRunSound = true;
            }
        }
        else
        {
            StopRunningSound();
        }
    }

    private void PlayJumpSound()
    {
        if (jumpClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpClip);
        }
    }

    private void PlayDeathSound()
    {
        if (deathClip != null && audioSource != null)
        {
            StopRunningSound();
            audioSource.PlayOneShot(deathClip);
        }
    }

    private void StopRunningSound()
    {
        if (!isPlayingRunSound || audioSource == null)
        {
            return;
        }

        audioSource.loop = false;
        audioSource.Stop();
        isPlayingRunSound = false;
    }

    #endregion

    #region Collisions

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Trap"))
        {
            Die();
            return;
        }

        UpdateSurfaceContact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        UpdateSurfaceContact(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        groundContacts.Remove(collision.collider);
        wallContacts.Remove(collision.collider);

        if (groundContacts.Count == 0)
        {
            groundCoyoteTimer = groundCoyoteTime;
        }

        if (wallContacts.Count == 0)
        {
            wallSlideHoldTimer = 0f;
            restartedJumpAnimationOnWallContact = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Die();
        }
    }

    private void UpdateSurfaceContact(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ground"))
        {
            return;
        }

        bool hasGroundContact = false;
        float wallNormalX = 0f;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                hasGroundContact = true;
            }

            // Allow wall contact on actual vertical walls as well as slightly slanted ones.
            if (Mathf.Abs(contact.normal.x) > 0.5f)
            {
                wallNormalX = contact.normal.x;
            }
        }

        if (hasGroundContact)
        {
            groundContacts.Add(collision.collider);
            groundCoyoteTimer = groundCoyoteTime;
        }
        else
        {
            groundContacts.Remove(collision.collider);
        }

        if (Mathf.Abs(wallNormalX) > 0.01f)
        {
            wallContacts[collision.collider] = wallNormalX;
            lastWallNormalX = wallNormalX;
            wallJumpCoyoteTimer = wallJumpCoyoteTime;

            TryRestartJumpAnimationFromWallContact();
        }
        else
        {
            wallContacts.Remove(collision.collider);
        }
    }

    #endregion

    #region Death & UI

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        PlayDeathSound();
        anim.SetTrigger(DieTrigger);
        SpawnBloodEffect();
        ShowDeathCanvas();
        Invoke(nameof(ReloadScene), 3f);
    }

    private void SpawnBloodEffect()
    {
        if (bloodEffectPrefab != null)
        {
            Instantiate(bloodEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    private void ShowDeathCanvas()
    {
        if (deathCanvas == null)
        {
            deathCanvas = FindInactiveObjectInScene("Loss");
        }

        if (deathCanvas == null)
        {
            deathCanvas = CreateFallbackDeathCanvas();
        }

        if (deathCanvas != null)
        {
            deathCanvas.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Death canvas is not assigned and no scene object named Loss was found.");
        }
    }

    private GameObject FindInactiveObjectInScene(string objectName)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        Scene activeScene = SceneManager.GetActiveScene();

        foreach (GameObject sceneObject in allObjects)
        {
            if (sceneObject.name == objectName && sceneObject.scene == activeScene)
            {
                return sceneObject;
            }
        }

        return null;
    }

    private GameObject CreateFallbackDeathCanvas()
    {
        GameObject canvasObject = new GameObject("Loss");
        RectTransform canvasRect = canvasObject.AddComponent<RectTransform>();
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        Image panelImage = panelObject.AddComponent<Image>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelImage.color = new Color(0f, 0f, 0f, 0.85f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (font != null)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(canvasObject.transform, false);

            RectTransform textRect = textObject.AddComponent<RectTransform>();
            Text text = textObject.AddComponent<Text>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            text.text = "YOU DIED";
            text.font = font;
            text.fontSize = 80;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }

        canvasObject.SetActive(false);
        return canvasObject;
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #endregion
}