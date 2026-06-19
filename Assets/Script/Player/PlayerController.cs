using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float jumpForce = 11f;

    [Header("Wall Movement")]
    [SerializeField] private float wallHoldDuration = 0.15f;
    [SerializeField] private float wallFallSpeed = 3f;
    [SerializeField] private float wallJumpThrowbackForce = 4f;
    [SerializeField] private float wallJumpControlLockDuration = 0.15f;

    [Header("Jump Assist")]
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Header("Better Jump")]
    [SerializeField] private float fallMultiplier = 2f;
    [SerializeField] private float lowJumpMultiplier = 2.5f;

    [SerializeField] private Rigidbody2D rb;

    private readonly HashSet<Collider2D> groundContacts = new();
    private readonly Dictionary<Collider2D, float> wallContacts = new();

    private bool moveRight = true;
    private float currentMoveInput;
    private float wallHoldTimer;
    private float wallJumpControlLockTimer;

    private float coyoteTimer;
    private float jumpBufferTimer;

    public float MoveDirection =>
        Mathf.Abs(currentMoveInput) > 0.01f
            ? Mathf.Sign(currentMoveInput)
            : 0f;

    private bool IsGrounded => groundContacts.Count > 0;
    private bool IsTouchingWall => wallContacts.Count > 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
        {
            currentMoveInput = 0f;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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

        if (IsGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f)
        {
            return;
        }

        wallJumpControlLockTimer =
            Mathf.Max(0f, wallJumpControlLockTimer - Time.fixedDeltaTime);

        if (jumpBufferTimer > 0f)
        {
            if (coyoteTimer > 0f)
            {
                Jump();
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }
            else if (CanWallJump())
            {
                WallJump();
                jumpBufferTimer = 0f;
            }
        }

        if (wallJumpControlLockTimer > 0f)
        {
            ApplyBetterJump();
            return;
        }

        float wallNormalX = GetWallNormalX();

        bool pressingIntoWall =
            !IsGrounded
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

            ApplyBetterJump();
            return;
        }

        wallHoldTimer = 0f;

        rb.linearVelocity = new Vector2(
            currentMoveInput * speed,
            rb.linearVelocity.y);

        ApplyBetterJump();
    }

    private void ApplyBetterJump()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up *
                                 Physics2D.gravity.y *
                                 (fallMultiplier - 1f) *
                                 Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 &&
                 !Input.GetKey(KeyCode.Space))
        {
            rb.linearVelocity += Vector2.up *
                                 Physics2D.gravity.y *
                                 (lowJumpMultiplier - 1f) *
                                 Time.fixedDeltaTime;
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            jumpForce);

        groundContacts.Clear();
    }

    private void WallJump()
    {
        float jumpDirection = Mathf.Sign(GetWallNormalX());

        float throwbackForce =
            Mathf.Min(wallJumpThrowbackForce, jumpForce * 0.9f);

        rb.linearVelocity = new Vector2(
            jumpDirection * throwbackForce,
            jumpForce);

        wallHoldTimer = 0f;
        wallJumpControlLockTimer = wallJumpControlLockDuration;
    }

    private bool CanWallJump()
    {
        if (!IsTouchingWall || Mathf.Abs(currentMoveInput) < 0.01f)
        {
            return false;
        }

        float directionAwayFromWall = Mathf.Sign(GetWallNormalX());

        return Mathf.Sign(currentMoveInput) == directionAwayFromWall;
    }

    private void Flip()
    {
        moveRight = !moveRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Trap"))
        {
            SceneManager.LoadScene(
                SceneManager.GetActiveScene().buildIndex);

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

    private float GetWallNormalX()
    {
        foreach (float wallNormalX in wallContacts.Values)
        {
            return wallNormalX;
        }

        return 0f;
    }
}