using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 10f;
    [Header("Wall Movement")]
    [SerializeField] private float wallHoldDuration = 0.15f;
    [SerializeField] private float wallFallSpeed = 3f;
    [SerializeField] private float wallJumpThrowbackForce = 4f;
    [SerializeField] private float wallJumpControlLockDuration = 0.15f;
    [SerializeField] private Rigidbody2D rb;
    // [SerializeField] private Animator anim;
    private readonly HashSet<Collider2D> groundContacts = new HashSet<Collider2D>();
    private readonly Dictionary<Collider2D, float> wallContacts = new Dictionary<Collider2D, float>();
    private bool moveRight = true;
    private float currentMoveInput;
    private float wallHoldTimer;
    private float wallJumpControlLockTimer;
    private bool jumpRequested;

    public float MoveDirection => Mathf.Abs(currentMoveInput) > 0.01f ? Mathf.Sign(currentMoveInput) : 0f;
    private bool IsGrounded => groundContacts.Count > 0;
    private bool IsTouchingWall => wallContacts.Count > 0;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (Time.timeScale == 0f)
        {
            currentMoveInput = 0f;
            jumpRequested = false;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            // anim.SetBool("run", false);
            return;
        }

        currentMoveInput = Input.GetAxisRaw("Horizontal");

        if (currentMoveInput > 0.01f && !moveRight)
        {
            Flip();
        }
        else if (currentMoveInput < -0.01f && moveRight)
        {
            Flip();
        }

        // anim.SetBool("run", Mathf.Abs(currentMoveInput) > 0.01f);
        // anim.SetBool("grounded", IsGrounded);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        wallJumpControlLockTimer = Mathf.Max(0f, wallJumpControlLockTimer - Time.fixedDeltaTime);

        if (jumpRequested)
        {
            if (IsGrounded)
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
            return;
        }

        float wallNormalX = GetWallNormalX();
        bool pressingIntoWall = !IsGrounded
            && IsTouchingWall
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
        // anim.SetTrigger("jump");
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        groundContacts.Clear();
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
    }

    bool CanWallJump()
    {
        if (!IsTouchingWall || Mathf.Abs(currentMoveInput) < 0.01f)
        {
            return false;
        }

        float directionAwayFromWall = Mathf.Sign(GetWallNormalX());
        return Mathf.Sign(currentMoveInput) == directionAwayFromWall;
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
            // anim.SetTrigger("die");
            // GameManager.Instance.GameOver();
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
        }
        else
        {
            groundContacts.Remove(collision.collider);
        }

        if (Mathf.Abs(wallNormalX) > 0.01f)
        {
            wallContacts[collision.collider] = wallNormalX;
        }
        else
        {
            wallContacts.Remove(collision.collider);
        }
    }

    float GetWallNormalX()
    {
        foreach (float wallNormalX in wallContacts.Values)
        {
            return wallNormalX;
        }

        return 0f;
    }
}
