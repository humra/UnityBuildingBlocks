using UnityEngine;

namespace UnityBuildingBlocks.Platformer2D
{
    [RequireComponent(typeof(Camera))]
    public sealed class Platformer2DCameraFollow : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private bool followPlayer = true;
        [SerializeField] private Transform target;
        [SerializeField] private bool followHorizontal = true;
        [SerializeField] private bool followVertical = true;
        [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);

        [Header("Look Ahead")]
        [SerializeField] private bool lookAheadEnabled = true;
        [SerializeField, Min(0f)] private float lookAheadDistance = 2f;
        [SerializeField, Min(0f)] private float minimumHorizontalSpeed = 0.1f;
        [SerializeField, Min(0f)] private float lookAheadSmoothTime = 0.2f;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float positionSmoothTime = 0.1f;

        [Header("Camera Bounds")]
        [SerializeField] private bool constrainToBounds = false;
        [SerializeField] private BoxCollider2D cameraBounds;

        private Camera controlledCamera;
        private Rigidbody2D targetBody;
        private Transform cachedTarget;
        private Vector3 positionVelocity;
        private float currentLookAhead;
        private float lookAheadVelocity;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            CacheTargetBody();
        }

        private void LateUpdate()
        {
            if (!followPlayer || target == null)
            {
                return;
            }

            if (cachedTarget != target)
            {
                CacheTargetBody();
            }

            UpdateLookAhead();

            Vector3 desiredPosition = target.position + offset;
            desiredPosition.x += currentLookAhead;

            if (!followHorizontal)
            {
                desiredPosition.x = transform.position.x;
            }

            if (!followVertical)
            {
                desiredPosition.y = transform.position.y;
            }

            desiredPosition = ConstrainPositionToBounds(desiredPosition);

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);
        }

        private void UpdateLookAhead()
        {
            float targetLookAhead = 0f;

            if (lookAheadEnabled && targetBody != null && Mathf.Abs(targetBody.linearVelocity.x) >= minimumHorizontalSpeed)
            {
                targetLookAhead = Mathf.Sign(targetBody.linearVelocity.x) * lookAheadDistance;
            }

            currentLookAhead = Mathf.SmoothDamp(currentLookAhead, targetLookAhead, ref lookAheadVelocity, lookAheadSmoothTime);
        }

        private Vector3 ConstrainPositionToBounds(Vector3 desiredPosition)
        {
            if (!constrainToBounds || cameraBounds == null || !controlledCamera.orthographic)
            {
                return desiredPosition;
            }

            Bounds bounds = cameraBounds.bounds;
            float halfHeight = controlledCamera.orthographicSize;
            float halfWidth = halfHeight * controlledCamera.aspect;

            if (bounds.size.x <= halfWidth * 2f)
            {
                desiredPosition.x = bounds.center.x;
            }
            else
            {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth);
            }

            if (bounds.size.y <= halfHeight * 2f)
            {
                desiredPosition.y = bounds.center.y;
            }
            else
            {
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, bounds.min.y + halfHeight, bounds.max.y - halfHeight);
            }

            return desiredPosition;
        }

        private void CacheTargetBody()
        {
            cachedTarget = target;
            targetBody = target != null ? target.GetComponent<Rigidbody2D>() : null;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            CacheTargetBody();
        }
    }
}