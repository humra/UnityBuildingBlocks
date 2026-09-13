using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityBuildingBlocks.IsometricMovement
{
    public enum IsometricMovementMode
    {
        ClickToMove,
        Keyboard
    }

    [RequireComponent(typeof(CharacterController))]
    public sealed class IsometricMovementController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private IsometricMovementMode movementMode = IsometricMovementMode.ClickToMove;
        [SerializeField] private bool movementEnabled = true;
        [SerializeField, Min(0f)] private float movementSpeed = 5f;
        [SerializeField, Min(0f)] private float rotationSpeed = 720f;
        [SerializeField, Min(0f)] private float stoppingDistance = 0.1f;
        [SerializeField] private bool rotateTowardsMovement = true;

        [Header("Click To Move")]
        [SerializeField] private bool updateDestinationWhileHeld = true;
        [SerializeField] private bool stopWhenMoveButtonReleased = false;
        [SerializeField, Min(0f)] private float holdToMoveThreshold = 0.15f;
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference pointerPositionAction;

        [Header("Keyboard Movement")]
        [SerializeField] private bool useCameraRelativeKeyboardMovement = true;
        [SerializeField] private InputActionReference keyboardMoveAction;

        [Header("Movement Mode Switching")]
        [SerializeField] private bool allowMovementModeSwitching = true;
        [SerializeField] private InputActionReference toggleMovementModeAction;

        [Header("Raycast")]
        [SerializeField] private Camera movementCamera;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Min(0f)] private float maxRaycastDistance = 1000f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Obstacle Handling")]
        [SerializeField] private bool stopWhenBlocked = true;
        [SerializeField, Min(0f)] private float blockedTimeBeforeStop = 0.15f;
        [SerializeField, Min(0f)] private float minimumMovementWhileBlocked = 0.001f;

        [Header("Gravity")]
        [SerializeField] private bool gravityEnabled = true;
        [SerializeField, Min(0f)] private float gravity = 20f;
        [SerializeField, Range(-10f, 0f)] private float groundedVerticalVelocity = -2f;

        private CharacterController characterController;
        private Vector3 destination;
        private float verticalVelocity;
        private float blockedTime;
        private float moveButtonHeldTime;
        private bool hasDestination;
        private bool continuousMoveActive;
        private bool previousMoveButtonPressed;

        public bool HasDestination => hasDestination;
        public Vector3 Destination => destination;
        public IsometricMovementMode MovementMode => movementMode;

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
            EnableAction(keyboardMoveAction);
            EnableAction(toggleMovementModeAction);
        }

        private void OnDisable()
        {
            DisableAction(moveAction);
            DisableAction(pointerPositionAction);
            DisableAction(keyboardMoveAction);
            DisableAction(toggleMovementModeAction);

            previousMoveButtonPressed = false;
            continuousMoveActive = false;
            moveButtonHeldTime = 0f;
        }

        private void Update()
        {
            if (allowMovementModeSwitching && toggleMovementModeAction != null && toggleMovementModeAction.action.WasPressedThisFrame())
            {
                ToggleMovementMode();
            }

            if (movementMode == IsometricMovementMode.ClickToMove)
            {
                HandleClickToMoveInput();
            }

            MoveCharacter();
        }

        private void HandleClickToMoveInput()
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

        private Vector3 GetKeyboardMovementDirection()
        {
            if (keyboardMoveAction == null)
            {
                return Vector3.zero;
            }

            Vector2 input = keyboardMoveAction.action.ReadValue<Vector2>();
            Transform reference = useCameraRelativeKeyboardMovement && movementCamera != null ? movementCamera.transform : transform;

            Vector3 forward = reference.forward;
            Vector3 right = reference.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            return Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
        }

        private Vector3 GetClickMovementDirection()
        {
            if (!movementEnabled || !hasDestination)
            {
                return Vector3.zero;
            }

            Vector3 toDestination = destination - transform.position;
            toDestination.y = 0f;

            float stoppingDistanceToUse = Mathf.Max(stoppingDistance, 0.001f);

            if (toDestination.sqrMagnitude <= stoppingDistanceToUse * stoppingDistanceToUse)
            {
                ClearDestination();
                return Vector3.zero;
            }

            return toDestination.normalized;
        }

        private void MoveCharacter()
        {
            Vector3 movementDirection = movementMode == IsometricMovementMode.Keyboard ? GetKeyboardMovementDirection() : GetClickMovementDirection();

            if (movementDirection.sqrMagnitude > 0.001f && rotateTowardsMovement)
            {
                RotateTowards(movementDirection);
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

        public void ToggleMovementMode()
        {
            movementMode = movementMode == IsometricMovementMode.ClickToMove ? IsometricMovementMode.Keyboard : IsometricMovementMode.ClickToMove;
            ClearDestination();
            moveButtonHeldTime = 0f;
            continuousMoveActive = false;
            previousMoveButtonPressed = false;
        }

        public void SetMovementMode(IsometricMovementMode newMovementMode)
        {
            movementMode = newMovementMode;
            ClearDestination();
            moveButtonHeldTime = 0f;
            continuousMoveActive = false;
            previousMoveButtonPressed = false;
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