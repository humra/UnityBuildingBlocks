using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityBuildingBlocks.IsometricMovement
{
    [RequireComponent(typeof(Camera))]
    public sealed class IsometricCameraController : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private bool followTarget = true;
        [SerializeField] private Transform target;
        [SerializeField] private bool followTargetRotation = false;

        [Header("Isometric Angle")]
        [SerializeField, Range(-89f, 89f)] private float pitch = 35f;
        [SerializeField] private float yaw = 45f;
        [SerializeField] private float yawOffset = 0f;
        [SerializeField] private float heightOffset = 0f;
        [SerializeField] private float lookAtHeight = 1f;

        [Header("Perspective Distance")]
        [SerializeField, Min(0.1f)] private float distance = 10f;
        [SerializeField, Min(0.1f)] private float minimumDistance = 5f;
        [SerializeField, Min(0.1f)] private float maximumDistance = 20f;

        [Header("Zoom")]
        [SerializeField] private bool zoomEnabled = true;
        [SerializeField] private InputActionReference zoomAction;
        [SerializeField, Min(0f)] private float zoomSensitivity = 0.02f;
        [SerializeField, Min(0f)] private float zoomSmoothTime = 0.08f;

        [Header("Orthographic Zoom")]
        [SerializeField, Min(0.1f)] private float minimumOrthographicSize = 3f;
        [SerializeField, Min(0.1f)] private float maximumOrthographicSize = 12f;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float positionSmoothTime = 0.1f;
        [SerializeField, Min(0f)] private float rotationSmoothSpeed = 12f;

        [Header("Look At")]
        [SerializeField] private bool lookAtTarget = true;

        private Camera controlledCamera;
        private Vector3 positionVelocity;
        private float distanceVelocity;
        private float orthographicSizeVelocity;
        private float currentYaw;
        private float targetDistance;
        private float targetOrthographicSize;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            currentYaw = yaw;

            minimumDistance = Mathf.Min(minimumDistance, maximumDistance);
            minimumOrthographicSize = Mathf.Min(minimumOrthographicSize, maximumOrthographicSize);
            targetDistance = Mathf.Clamp(distance, minimumDistance, maximumDistance);
            targetOrthographicSize = Mathf.Clamp(controlledCamera.orthographicSize, minimumOrthographicSize, maximumOrthographicSize);

            distance = targetDistance;
            controlledCamera.orthographicSize = targetOrthographicSize;
        }

        private void OnEnable()
        {
            if (zoomAction != null)
            {
                zoomAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (zoomAction != null)
            {
                zoomAction.action.Disable();
            }
        }

        private void Update()
        {
            if (!zoomEnabled || zoomAction == null)
            {
                return;
            }

            Vector2 scroll = zoomAction.action.ReadValue<Vector2>();

            if (Mathf.Abs(scroll.y) < 0.001f)
            {
                return;
            }

            if (controlledCamera.orthographic)
            {
                targetOrthographicSize = Mathf.Clamp(targetOrthographicSize - scroll.y * zoomSensitivity, minimumOrthographicSize, maximumOrthographicSize);
            }
            else
            {
                targetDistance = Mathf.Clamp(targetDistance - scroll.y * zoomSensitivity, minimumDistance, maximumDistance);
            }
        }

        private void LateUpdate()
        {
            if (!followTarget || target == null)
            {
                return;
            }

            if (followTargetRotation)
            {
                currentYaw = target.eulerAngles.y + yawOffset;
            }

            distance = Mathf.SmoothDamp(distance, targetDistance, ref distanceVelocity, zoomSmoothTime);

            controlledCamera.orthographicSize = Mathf.SmoothDamp(controlledCamera.orthographicSize, targetOrthographicSize, ref orthographicSizeVelocity, zoomSmoothTime);
            Quaternion orbitRotation = Quaternion.Euler(pitch, currentYaw, 0f);
            Vector3 desiredPosition = target.position + Vector3.up * heightOffset + orbitRotation * (Vector3.back * distance);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);

            if (lookAtTarget)
            {
                Vector3 lookAtPosition = target.position + Vector3.up * lookAtHeight;
                Vector3 lookDirection = lookAtPosition - transform.position;

                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
                }
            }
            else
            {
                transform.rotation = orbitRotation;
            }
        }
    }
}