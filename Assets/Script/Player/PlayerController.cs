using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [Header("Submission Summary")]
    [SerializeField] private bool showSummaryOnStart = true;
    [SerializeField] private float summaryAutoHideSeconds = 10f;
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
    private bool showSummary;
    private float summaryTimer;
    private GUIStyle summaryBoxStyle;
    private GUIStyle summaryTitleStyle;
    private GUIStyle summaryTextStyle;

    private static readonly int JumpTrigger = Animator.StringToHash("jump");
    private static readonly int JumpState = Animator.StringToHash("jump");

    public float MoveDirection => currentMoveInput;

    private bool IsGrounded => groundContacts.Count > 0;
    private bool IsTouchingWall => wallContacts.Count > 0;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        wasGroundedLastFrame = IsGrounded;
        showSummary = showSummaryOnStart;
        summaryTimer = summaryAutoHideSeconds;
    }

    void Update()
    {
        UpdateSummaryInput();

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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }
    }

    void UpdateSummaryInput()
    {
        if (Input.GetKeyDown(KeyCode.F1) || Input.GetKeyDown(KeyCode.H))
        {
            showSummary = !showSummary;
            summaryTimer = summaryAutoHideSeconds;
        }

        if (showSummary && summaryAutoHideSeconds > 0f)
        {
            summaryTimer -= Time.unscaledDeltaTime;

            if (summaryTimer <= 0f)
            {
                showSummary = false;
            }
        }
    }

    void OnGUI()
    {
        EnsureSummaryStyles();

        float width = Mathf.Min(520f, Screen.width - 32f);
        Rect hintRect = new Rect(16f, 16f, width, 34f);
        GUI.Box(hintRect, "H / F1: Objective and Controls", summaryBoxStyle);

        if (!showSummary)
        {
            return;
        }

        Rect panelRect = new Rect(16f, 56f, width, 190f);
        GUILayout.BeginArea(panelRect, summaryBoxStyle);
        GUILayout.Label("Break The Curse", summaryTitleStyle);
        GUILayout.Label(GetLevelSummary(), summaryTextStyle);
        GUILayout.Space(6f);
        GUILayout.Label("Controls: A/D move, mouse wheel keeps movement direction, Space jumps, wall jump from walls, E flips the world, R restarts.", summaryTextStyle);
        GUILayout.Label("Goal: survive traps and enemies, use inversion to reveal the safe path, then reach the portal.", summaryTextStyle);
        GUILayout.Label("World: " + (Inversion.IsInverted ? "Inverted curse realm" : "Actual realm"), summaryTextStyle);
        GUILayout.EndArea();
    }

    void EnsureSummaryStyles()
    {
        if (summaryBoxStyle != null)
        {
            return;
        }

        summaryBoxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 14,
            wordWrap = true,
            padding = new RectOffset(14, 14, 10, 10)
        };

        summaryTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            wordWrap = true
        };

        summaryTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white },
            wordWrap = true
        };
    }

    string GetLevelSummary()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName.Contains("Level 1"))
        {
            return "Level 1 introduces the curse: swap worlds, avoid spikes, and learn how enemies react to the inverted realm.";
        }

        if (sceneName.Contains("Level 2"))
        {
            return "Level 2 raises the pressure with storm effects, tighter traps, and longer inversion routes.";
        }

        if (sceneName.Contains("Level 3"))
        {
            return "Level 3 is the final escape: use the movement skills you learned and break the curse at the last portal.";
        }

        return "A short cursed platformer about switching between two versions of the world to find a survivable path.";
    }

    void FixedUpdate()
    {
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
    }

    void WallJump()
    {
        float jumpDirection = Mathf.Sign(GetWallNormalX());
        float throwbackForce = Mathf.Min(wallJumpThrowbackForce, jumpForce * 0.9f);

        rb.linearVelocity = new Vector2(
            jumpDirection * throwbackForce,
            jumpForce);
        wallHoldTimer = 0f;
        wallJumpControlLockTimer = wallJumpControlLockDuration;
        wallJumpRecoveryTimer = wallJumpControlLockDuration;
        wallJumpCoyoteTimer = 0f;
        restartedJumpAnimationOnWallContact = false;
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
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        UpdateSurfaceContact(collision);
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
}
