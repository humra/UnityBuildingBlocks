using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityBuildingBlocks.IsometricMovement
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class IsometricMovementController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private bool movementEnabled = true;
        [SerializeField, Min(0f)] private float movementSpeed = 5f;
        [SerializeField, Min(0f)] private float rotationSpeed = 720f;
        [SerializeField, Min(0f)] private float stoppingDistance = 0.1f;
        [SerializeField] private bool rotateTowardsDestination = true;

        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference pointerPositionAction;
        [SerializeField] private bool updateDestinationWhileHeld = true;
        [SerializeField] private bool stopWhenMoveButtonReleased = false;
        [SerializeField, Min(0f)] private float holdToMoveThreshold = 0.15f;

        [Header("Raycast")]
        [SerializeField] private Camera movementCamera;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Min(0f)] private float maxRaycastDistance = 1000f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction =
            QueryTriggerInteraction.Ignore;

        [Header("Gravity")]
        [SerializeField] private bool gravityEnabled = true;
        [SerializeField, Min(0f)] private float gravity = 20f;
        [SerializeField, Range(-10f, 0f)] private float groundedVerticalVelocity = -2f;

        [Header("Obstacle Handling")]
        [SerializeField] private bool stopWhenBlocked = true;
        [SerializeField, Min(0f)] private float blockedTimeBeforeStop = 0.15f;
        [SerializeField, Min(0f)] private float minimumMovementWhileBlocked = 0.001f;

        private CharacterController characterController;
        private Vector3 destination;
        private float verticalVelocity;
        private bool hasDestination;
        private float blockedTime;
        private float moveButtonHeldTime;
        private bool continuousMoveActive;
        private bool previousMoveButtonPressed;

        public bool HasDestination => hasDestination;
        public Vector3 Destination => destination;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (movementCamera == null)
            {
                movementCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            EnableAction(moveAction);
            EnableAction(pointerPositionAction);
        }

        private void OnDisable()
        {
            DisableAction(moveAction);
            DisableAction(pointerPositionAction);
        }

        private void Update()
        {
            bool moveButtonPressed = moveAction != null && moveAction.action.IsPressed();
            bool moveButtonPressedThisFrame = moveAction != null && moveAction.action.WasPressedThisFrame();

            if (movementEnabled && moveButtonPressedThisFrame)
            {
                SetDestinationFromPointer();
                moveButtonHeldTime = 0f;
                continuousMoveActive = false;
            }

            if (moveButtonPressed && !moveButtonPressedThisFrame && updateDestinationWhileHeld)
            {
                moveButtonHeldTime += Time.deltaTime;

                if (moveButtonHeldTime >= holdToMoveThreshold)
                {
                    continuousMoveActive = true;
                }

                if (continuousMoveActive)
                {
                    SetDestinationFromPointer();
                }
            }

            if (stopWhenMoveButtonReleased && previousMoveButtonPressed && !moveButtonPressed && continuousMoveActive)
            {
                ClearDestination();
            }

            if (!moveButtonPressed)
            {
                moveButtonHeldTime = 0f;
                continuousMoveActive = false;
            }

            previousMoveButtonPressed = moveButtonPressed;

            MoveCharacter();
        }

        private void SetDestinationFromPointer()
        {
            Camera cameraToUse = movementCamera != null ? movementCamera : Camera.main;

            if (cameraToUse == null || pointerPositionAction == null)
            {
                return;
            }

            Vector2 pointerPosition = pointerPositionAction.action.ReadValue<Vector2>();
            Ray ray = cameraToUse.ScreenPointToRay(pointerPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, groundLayers, triggerInteraction))
            {
                SetDestination(hit.point);
            }
        }

        public void SetDestination(Vector3 worldPosition)
        {
            if (!hasDestination || Vector3.Distance(destination, worldPosition) > 0.01f)
            {
                blockedTime = 0f;
            }

            destination = worldPosition;
            hasDestination = true;
        }

        public void ClearDestination()
        {
            hasDestination = false;
            blockedTime = 0f;
        }

        private void MoveCharacter()
        {
            Vector3 movementDirection = Vector3.zero;

            if (movementEnabled && hasDestination)
            {
                Vector3 toDestination = destination - transform.position;
                toDestination.y = 0f;

                float stoppingDistanceToUse = Mathf.Max(stoppingDistance, 0.001f);

                if (toDestination.sqrMagnitude <= stoppingDistanceToUse * stoppingDistanceToUse)
                {
                    ClearDestination();
                }
                else
                {
                    movementDirection = toDestination.normalized;

                    if (rotateTowardsDestination)
                    {
                        RotateTowards(movementDirection);
                    }
                }
            }

            if (gravityEnabled)
            {
                if (characterController.isGrounded && verticalVelocity < 0f)
                {
                    verticalVelocity = groundedVerticalVelocity;
                }

                verticalVelocity -= gravity * Time.deltaTime;
            }
            else
            {
                verticalVelocity = 0f;
            }

            Vector3 velocity = movementDirection * movementSpeed;
            velocity.y = verticalVelocity;
            Vector3 positionBeforeMove = transform.position;
            CollisionFlags collisionFlags = characterController.Move(velocity * Time.deltaTime);
            Vector3 movementDuringFrame = transform.position - positionBeforeMove;
            movementDuringFrame.y = 0f;

            bool isBlocked = hasDestination && movementDirection.sqrMagnitude > 0.001f && (collisionFlags & CollisionFlags.Sides) != 0 && movementDuringFrame.magnitude <= minimumMovementWhileBlocked;

            if (stopWhenBlocked && isBlocked)
            {
                blockedTime += Time.deltaTime;

                if (blockedTime >= blockedTimeBeforeStop)
                {
                    ClearDestination();
                }
            }
            else
            {
                blockedTime = 0f;
            }

            if (gravityEnabled && (collisionFlags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
            {
                verticalVelocity = groundedVerticalVelocity;
            }
        }

        private void RotateTowards(Vector3 direction)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
    }
}