using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityBuildingBlocks.Platformer2D
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Platformer2DController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private bool movementEnabled = true;
        [SerializeField, Min(0f)] private float maximumMoveSpeed = 7f;
        [SerializeField, Min(0f)] private float groundAcceleration = 60f;
        [SerializeField, Min(0f)] private float groundDeceleration = 80f;
        [SerializeField, Min(0f)] private float airAcceleration = 40f;
        [SerializeField, Min(0f)] private float airDeceleration = 20f;

        [Header("Jumping")]
        [SerializeField] private bool jumpEnabled = true;
        [SerializeField, Min(1)] private int maximumJumps = 1;
        [SerializeField, Min(0f)] private float jumpVelocity = 12f;
        [SerializeField] private bool variableJumpHeight = true;
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.1f;

        [Header("Falling")]
        [SerializeField] private bool limitFallSpeed = true;
        [SerializeField, Min(0f)] private float maximumFallSpeed = 20f;

        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;

        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheck;
        [SerializeField, Min(0f)] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Visuals")]
        [SerializeField] private bool flipSprite = true;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Rigidbody2D body;
        private Vector2 moveInput;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private int jumpsRemaining;
        private bool isGrounded;
        private bool jumpHeld;
        private bool jumpCutApplied;

        public bool IsGrounded => isGrounded;
        public int JumpsRemaining => jumpsRemaining;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            jumpsRemaining = maximumJumps;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            EnableAction(moveAction);
            EnableAction(jumpAction);
        }

        private void OnDisable()
        {
            DisableAction(moveAction);
            DisableAction(jumpAction);
        }

        private void Update()
        {
            moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            jumpHeld = jumpAction != null && jumpAction.action.IsPressed();

            if (jumpAction != null && jumpAction.action.WasPressedThisFrame())
            {
                jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                jumpBufferTimer -= Time.deltaTime;
            }

            isGrounded = CheckGrounded();

            if (isGrounded)
            {
                coyoteTimer = coyoteTime;
                jumpsRemaining = maximumJumps;
            }
            else
            {
                coyoteTimer -= Time.deltaTime;
            }

            UpdateSpriteDirection();
        }

        private void FixedUpdate()
        {
            MoveHorizontally();
            HandleJump();
            HandleVariableJumpHeight();
            LimitFallSpeed();
        }

        private void MoveHorizontally()
        {
            float targetSpeed = movementEnabled ? moveInput.x * maximumMoveSpeed : 0f;
            bool isAccelerating = Mathf.Abs(targetSpeed) > 0.001f;
            bool useGroundMovement = isGrounded;
            float acceleration;

            if (useGroundMovement)
            {
                acceleration = isAccelerating ? groundAcceleration : groundDeceleration;
            }
            else
            {
                acceleration = isAccelerating ? airAcceleration : airDeceleration;
            }

            float newHorizontalSpeed = Mathf.MoveTowards(body.linearVelocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(newHorizontalSpeed, body.linearVelocity.y);
        }

        private void HandleJump()
        {
            if (!movementEnabled || !jumpEnabled || jumpBufferTimer <= 0f)
            {
                return;
            }

            bool canUseCoyoteJump = coyoteTimer > 0f;
            bool canUseAdditionalJump = jumpsRemaining > 0;

            if (!canUseCoyoteJump && !canUseAdditionalJump)
            {
                return;
            }

            if (canUseCoyoteJump)
            {
                coyoteTimer = 0f;
            }

            jumpsRemaining--;
            jumpBufferTimer = 0f;
            jumpCutApplied = false;
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
        }

        private void HandleVariableJumpHeight()
        {
            if (!variableJumpHeight || jumpHeld || jumpCutApplied || body.linearVelocity.y <= 0f)
            {
                return;
            }

            body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * jumpCutMultiplier);
            jumpCutApplied = true;
        }

        private void LimitFallSpeed()
        {
            if (!limitFallSpeed || body.linearVelocity.y >= -maximumFallSpeed)
            {
                return;
            }

            body.linearVelocity = new Vector2(body.linearVelocity.x, -maximumFallSpeed);
        }

        private bool CheckGrounded()
        {
            Vector2 checkPosition = groundCheck != null ? groundCheck.position : (Vector2)transform.position + Vector2.down * 0.5f;
            return Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayers) != null;
        }

        private void UpdateSpriteDirection()
        {
            if (!flipSprite || spriteRenderer == null || Mathf.Abs(moveInput.x) <= 0.001f)
            {
                return;
            }

            spriteRenderer.flipX = moveInput.x < 0f;
        }

        private static void EnableAction(InputActionReference actionReference)
        {
            if (actionReference != null)
            {
                actionReference.action.Enable();
            }
        }

        private static void DisableAction(InputActionReference actionReference)
        {
            if (actionReference != null)
            {
                actionReference.action.Disable();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 checkPosition = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.5f;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
        }
    }
}