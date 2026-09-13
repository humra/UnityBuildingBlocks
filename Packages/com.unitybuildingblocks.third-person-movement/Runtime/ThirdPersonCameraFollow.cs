using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityBuildingBlocks.ThirdPersonMovement
{
    public sealed class ThirdPersonCameraFollow : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private bool followPlayer = true;
        [SerializeField] private Transform target;
        [SerializeField] private bool followRotation = true;

        [Header("Camera Position")]
        [SerializeField, Min(0f)] private float distance = 6f;
        [SerializeField] private float heightOffset = 1f;
        [SerializeField, Range(-89f, 89f)] private float pitch = 20f;
        [SerializeField] private float yawOffset = 0f;

        [Header("Zoom")]
        [SerializeField] private bool zoomEnabled = true;
        [SerializeField, Min(0.1f)] private float minimumDistance = 2f;
        [SerializeField, Min(0.1f)] private float maximumDistance = 10f;
        [SerializeField, Min(0f)] private float zoomSensitivity = 0.02f;
        [SerializeField, Min(0f)] private float zoomSmoothTime = 0.08f;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float positionSmoothTime = 0.1f;
        [SerializeField, Min(0f)] private float rotationSmoothSpeed = 12f;

        [Header("Look At")]
        [SerializeField] private float lookAtHeight = 1f;

        private Vector3 positionVelocity;
        private float distanceVelocity;
        private float initialYaw;
        private float targetDistance;

        private void Awake()
        {
            initialYaw = transform.eulerAngles.y;

            minimumDistance = Mathf.Min(
                minimumDistance,
                maximumDistance);

            targetDistance = Mathf.Clamp(
                distance,
                minimumDistance,
                maximumDistance);

            distance = targetDistance;
        }

        private void Update()
        {
            if (!zoomEnabled || Mouse.current == null)
            {
                return;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;

            if (Mathf.Abs(scroll) > 0.001f)
            {
                targetDistance = Mathf.Clamp(
                    targetDistance - scroll * zoomSensitivity,
                    minimumDistance,
                    maximumDistance);
            }
        }

        private void LateUpdate()
        {
            if (!followPlayer || target == null)
            {
                return;
            }

            distance = Mathf.SmoothDamp(
                distance,
                targetDistance,
                ref distanceVelocity,
                zoomSmoothTime);

            float yaw = followRotation
                ? target.eulerAngles.y + yawOffset
                : initialYaw + yawOffset;

            Quaternion orbitRotation =
                Quaternion.Euler(pitch, yaw, 0f);

            Vector3 desiredPosition =
                target.position +
                Vector3.up * heightOffset +
                orbitRotation * (Vector3.back * distance);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime);

            Vector3 lookAtPosition =
                target.position + Vector3.up * lookAtHeight;

            Vector3 lookDirection =
                lookAtPosition - transform.position;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(lookDirection);

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSmoothSpeed * Time.deltaTime);
            }
        }
    }
}