using UnityEngine;

namespace UnityBuildingBlocks.ThirdPersonMovement
{
    public sealed class ThirdPersonCameraFollow : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private bool followPlayer = true;
        [SerializeField] private Transform target;

        [Tooltip("Automatically rotate the camera behind the player's heading. " +
                 "Disable this when using camera-relative movement.")]
        [SerializeField] private bool followRotation = false;

        [Header("Camera Position")]
        [SerializeField, Min(0f)] private float distance = 6f;
        [SerializeField] private float heightOffset = 1f;
        [SerializeField, Range(-89f, 89f)] private float pitch = 20f;
        [SerializeField] private float yawOffset = 0f;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float positionSmoothTime = 0.1f;
        [SerializeField, Min(0f)] private float rotationSmoothSpeed = 12f;

        [Header("Look At")]
        [SerializeField] private float lookAtHeight = 1f;

        private Vector3 positionVelocity;
        private float initialYaw;

        private void Awake()
        {
            initialYaw = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (!followPlayer || target == null)
            {
                return;
            }

            float yaw = followRotation
                ? target.eulerAngles.y + yawOffset
                : initialYaw + yawOffset;

            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);

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

            Vector3 lookDirection = lookAtPosition - transform.position;

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