using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityBuildingBlocks.ThirdPersonMovement
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonMovementController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float rotationSpeed = 12f;
        [SerializeField, Min(0f)] private float gravity = 20f;

        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;

        [Header("References")]
        [SerializeField] private Transform movementReference;

        private CharacterController characterController;
        private float verticalVelocity;

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
            if (moveAction != null)
            {
                moveAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.action.Disable();
            }
        }

        private void Update()
        {
            Vector2 input = moveAction != null
                ? moveAction.action.ReadValue<Vector2>()
                : Vector2.zero;

            Vector3 moveDirection = GetCameraRelativeDirection(input);
            Move(moveDirection);
        }

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            Vector3 forward = movementReference != null
                ? movementReference.forward
                : Vector3.forward;

            Vector3 right = movementReference != null
                ? movementReference.right
                : Vector3.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            return Vector3.ClampMagnitude(
                right * input.x + forward * input.y,
                1f);
        }

        private void Move(Vector3 moveDirection)
        {
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(moveDirection);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity -= gravity * Time.deltaTime;

            Vector3 velocity = moveDirection * moveSpeed;
            velocity.y = verticalVelocity;

            characterController.Move(velocity * Time.deltaTime);
        }
    }
}