using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityBuildingBlocks.ThirdPersonMovement
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonMovementController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float rotationSpeed = 180f;
        [SerializeField] private bool useCameraRelativeMovement = false;

        [Header("Jumping")]
        [SerializeField] private bool jumpEnabled = true;
        [SerializeField, Min(1)] private int maximumJumps = 1;
        [SerializeField, Min(0f)] private float jumpHeight = 1.5f;
        [SerializeField, Min(0f)] private float gravity = 20f;

        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference rotateAction;
        [SerializeField] private InputActionReference jumpAction;

        [Header("References")]
        [SerializeField] private Transform movementReference;

        private CharacterController characterController;
        private float verticalVelocity;
        private int jumpCount;
        private bool wasGrounded;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (movementReference == null && Camera.main != null)
            {
                movementReference = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            EnableAction(moveAction);
            EnableAction(rotateAction);
            EnableAction(jumpAction);
        }

        private void OnDisable()
        {
            DisableAction(moveAction);
            DisableAction(rotateAction);
            DisableAction(jumpAction);
        }

        private void Update()
        {
            Vector2 movementInput = moveAction != null
                ? moveAction.action.ReadValue<Vector2>()
                : Vector2.zero;

            float rotationInput = rotateAction != null
                ? rotateAction.action.ReadValue<float>()
                : 0f;

            Rotate(rotationInput);
            HandleJumping();

            Vector3 moveDirection =
                GetMovementDirection(movementInput);

            Move(moveDirection);
        }

        private Vector3 GetMovementDirection(Vector2 input)
        {
            Transform reference = useCameraRelativeMovement &&
                                  movementReference != null
                ? movementReference
                : transform;

            Vector3 forward = reference.forward;
            Vector3 right = reference.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            return Vector3.ClampMagnitude(
                right * input.x + forward * input.y,
                1f);
        }

        private void Rotate(float rotationInput)
        {
            if (Mathf.Abs(rotationInput) < 0.001f)
            {
                return;
            }

            transform.Rotate(
                Vector3.up,
                rotationInput * rotationSpeed * Time.deltaTime,
                Space.World);
        }

        private void HandleJumping()
        {
            bool isGrounded = characterController.isGrounded;

            if (isGrounded && !wasGrounded)
            {
                jumpCount = 0;
            }

            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            bool jumpPressed = jumpAction != null &&
                               jumpAction.action.WasPressedThisFrame();

            bool canJump = isGrounded || jumpCount < maximumJumps;

            if (jumpEnabled && jumpPressed && canJump)
            {
                verticalVelocity =
                    Mathf.Sqrt(2f * jumpHeight * gravity);

                jumpCount++;
            }

            verticalVelocity -= gravity * Time.deltaTime;
            wasGrounded = isGrounded;
        }

        private void Move(Vector3 moveDirection)
        {
            Vector3 velocity = moveDirection * moveSpeed;
            velocity.y = verticalVelocity;

            characterController.Move(
                velocity * Time.deltaTime);
        }

        private static void EnableAction(
            InputActionReference actionReference)
        {
            if (actionReference != null)
            {
                actionReference.action.Enable();
            }
        }

        private static void DisableAction(
            InputActionReference actionReference)
        {
            if (actionReference != null)
            {
                actionReference.action.Disable();
            }
        }
    }
}