using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private GameObject deathCanvas;
    [Header("Wall Movement")]
    [SerializeField] private float wallHoldDuration = 0.15f;
    [SerializeField] private float wallFallSpeed = 3f;
    [SerializeField] private float wallJumpThrowbackForce = 4f;
    [SerializeField] private float wallJumpControlLockDuration = 0.15f;
    [SerializeField] private float wallJumpCoyoteTime = 0.1f;
    [SerializeField] private int wallJumpAnimationRestartBeforeFrame = 3;
    [SerializeField] private float groundCoyoteTime = 0.1f;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private GameObject bloodEffectPrefab;
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip runningClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip deathClip;
    private readonly HashSet<Collider2D> groundContacts = new HashSet<Collider2D>();
    private readonly Dictionary<Collider2D, float> wallContacts = new Dictionary<Collider2D, float>();
    private bool moveRight = true;
    public float currentMoveInput;
    private float scrollMoveInput;
    private float wallHoldTimer;
    private float wallJumpControlLockTimer;
    private float wallJumpRecoveryTimer;
    private float wallJumpCoyoteTimer;
    private float groundCoyoteTimer;
    private float jumpGroundLockTimer;
    private float lastWallNormalX;
    private bool jumpRequested;
    private bool restartedJumpAnimationOnWallContact;
    private bool wasGroundedLastFrame;
    private bool isPlayingRunSound;

    private static readonly int JumpTrigger = Animator.StringToHash("jump");
    private static readonly int JumpState = Animator.StringToHash("jump");

    private bool IsGrounded => groundContacts.Count > 0;
    private bool IsTouchingWall => wallContacts.Count > 0;
    public bool IsDead { get; private set; }
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        wasGroundedLastFrame = IsGrounded;
    }

    void Update()
    {
        if (IsDead)
        {
            currentMoveInput = 0f;
            jumpRequested = false;
            StopRunningSound();
            return;
        }

        if (Time.timeScale == 0f)
        {
            currentMoveInput = 0f;
            jumpRequested = false;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            anim.SetBool("run", false);
            anim.SetBool("grounded", false);
            return;
        }

        jumpGroundLockTimer = Mathf.Max(0f, jumpGroundLockTimer - Time.deltaTime);

        wasGroundedLastFrame = IsGrounded;

        if (Input.GetMouseButtonDown(2))
        {
            scrollMoveInput = 0f;
        }

        float scrollDelta = Input.mouseScrollDelta.y;

        if (scrollDelta > 0.01f)
        {
            scrollMoveInput = 1f;
        }
        else if (scrollDelta < -0.01f)
        {
            scrollMoveInput = -1f;
        }

        // Check A and D keys for movement (takes priority over scroll)
        if (Input.GetKey(KeyCode.A))
        {
            currentMoveInput = -1f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            currentMoveInput = 1f;
        }
        else
        {
            currentMoveInput = scrollMoveInput;
        }

        if (currentMoveInput > 0.01f && !moveRight)
        {
            Flip();
        }
        else if (currentMoveInput < -0.01f && moveRight)
        {
            Flip();
        }

        anim.SetBool("run", Mathf.Abs(currentMoveInput) > 0.01f);
        anim.SetBool("grounded", IsGrounded && jumpGroundLockTimer <= 0f);

        // Handle running sound
        if (IsGrounded && Mathf.Abs(currentMoveInput) > 0.01f)
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
            if (isPlayingRunSound)
            {
                audioSource.loop = false;
                audioSource.Stop();
                isPlayingRunSound = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
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

        wallJumpControlLockTimer = Mathf.Max(0f, wallJumpControlLockTimer - Time.fixedDeltaTime);
        wallJumpCoyoteTimer = Mathf.Max(0f, wallJumpCoyoteTimer - Time.fixedDeltaTime);
        wallJumpRecoveryTimer = Mathf.Max(0f, wallJumpRecoveryTimer - Time.fixedDeltaTime);
        groundCoyoteTimer = Mathf.Max(0f, groundCoyoteTimer - Time.fixedDeltaTime);

        if (jumpRequested)
        {
            if (IsGrounded)
            {
                Jump();
            }
            else if (CanCoyoteJump())
            {
                Jump();
            }
            else if (CanWallJump())
            {
                WallJump();
            }

            jumpRequested = false;
        }

        if (wallJumpControlLockTimer > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y);
            return;
        }

        float wallNormalX = GetWallNormalX();
        bool pressingIntoWall = !IsGrounded
            && IsTouchingWall
            && wallJumpRecoveryTimer <= 0f
            && Mathf.Abs(currentMoveInput) > 0.01f
            && Mathf.Sign(currentMoveInput) == -Mathf.Sign(wallNormalX);

        if (pressingIntoWall)
        {
            float verticalSpeed = rb.linearVelocity.y;

            if (verticalSpeed <= 0f)
            {
                wallHoldTimer += Time.fixedDeltaTime;
                verticalSpeed = wallHoldTimer < wallHoldDuration
                    ? 0f
                    : Mathf.Min(verticalSpeed, -wallFallSpeed);
            }

            rb.linearVelocity = new Vector2(0f, verticalSpeed);
            return;
        }

        wallHoldTimer = 0f;
        rb.linearVelocity = new Vector2(currentMoveInput * speed, rb.linearVelocity.y);
    }

    void Jump()
    {
        anim.ResetTrigger(JumpTrigger);
        anim.Play(JumpState, 0, 0f);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        groundContacts.Clear();
        groundCoyoteTimer = 0f;
        jumpGroundLockTimer = 0.12f;
        restartedJumpAnimationOnWallContact = false;
        StopRunningSound();
        PlayJumpSound();
    }

    void WallJump()
    {
        float wallNormalX = GetWallNormalX();
        float jumpDirection = wallNormalX == 0f ? 0f : Mathf.Sign(wallNormalX);
        float throwbackForce = Mathf.Min(wallJumpThrowbackForce, jumpForce * 0.9f);

        rb.linearVelocity = new Vector2(
            jumpDirection * throwbackForce,
            jumpForce);
        wallHoldTimer = 0f;
        wallJumpControlLockTimer = wallJumpControlLockDuration;
        wallJumpRecoveryTimer = wallJumpControlLockDuration;
        wallJumpCoyoteTimer = 0f;
        restartedJumpAnimationOnWallContact = false;
        StopRunningSound();
        PlayJumpSound();
    }

    bool CanWallJump()
    {
        if (IsGrounded)
        {
            return false;
        }

        return HasWallForJump();
    }

    bool CanCoyoteJump()
    {
        return groundCoyoteTimer > 0f;
    }

    void Flip()
    {
        moveRight = !moveRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Trap"))
        {
            Die();
            return;
        }

        UpdateSurfaceContact(collision);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Die();
            return;
        }
    }

    void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        PlayDeathSound();
        anim.SetTrigger("die");
        SpawnBloodEffect();
        ShowDeathCanvas();
        Invoke(nameof(ReloadScene), 3f);
    }

    void SpawnBloodEffect()
    {
        if (bloodEffectPrefab != null)
        {
            Instantiate(bloodEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    void ShowDeathCanvas()
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

    GameObject FindInactiveObjectInScene(string objectName)
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

    GameObject CreateFallbackDeathCanvas()
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

    void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    void OnCollisionStay2D(Collision2D collision)
    {
        UpdateSurfaceContact(collision);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        groundContacts.Remove(collision.collider);
        wallContacts.Remove(collision.collider);

        if (groundContacts.Count == 0)
        {
            groundCoyoteTimer = groundCoyoteTime;
        }

        if (wallContacts.Count == 0)
        {
            restartedJumpAnimationOnWallContact = false;
        }
    }

    void UpdateSurfaceContact(Collision2D collision)
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

    void TryRestartJumpAnimationFromWallContact()
    {
        if (restartedJumpAnimationOnWallContact || IsGrounded || rb.linearVelocity.y <= 0f)
        {
            return;
        }

        AnimatorStateInfo currentState = anim.GetCurrentAnimatorStateInfo(0);
        bool isJumpState = currentState.shortNameHash == Animator.StringToHash("jump")
            || currentState.shortNameHash == Animator.StringToHash("Jump");

        if (!isJumpState || !ShouldRestartJumpAnimation(currentState))
        {
            return;
        }

        anim.Play(currentState.shortNameHash, 0, 0f);
        restartedJumpAnimationOnWallContact = true;
    }

    bool ShouldRestartJumpAnimation(AnimatorStateInfo currentState)
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

    float GetWallNormalX()
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

    bool HasWallForJump()
    {
        return Mathf.Abs(GetWallNormalX()) > 0.01f;
    }

    void PlayJumpSound()
    {
        if (jumpClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpClip);
        }
    }

    void PlayDeathSound()
    {
        if (deathClip != null && audioSource != null)
        {
            StopRunningSound();
            audioSource.PlayOneShot(deathClip);
        }
    }

    void StopRunningSound()
    {
        if (!isPlayingRunSound || audioSource == null)
        {
            return;
        }

        audioSource.loop = false;
        audioSource.Stop();
        isPlayingRunSound = false;
    }
}
