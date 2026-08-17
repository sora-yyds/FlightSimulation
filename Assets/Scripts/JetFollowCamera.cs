using UnityEngine;

namespace FlightSimulation
{
    public sealed class JetFollowCamera : MonoBehaviour
    {
        [SerializeField, InspectorName("跟随目标")] private Transform target;
        [SerializeField, InspectorName("本地跟随偏移")] private Vector3 localOffset = new Vector3(0f, 4.5f, -17f);
        [SerializeField, InspectorName("前方观察距离")] private float lookAhead = 18f;
        [SerializeField, InspectorName("位置平滑时间")] private float positionSmoothTime = 0.18f;
        [SerializeField, InspectorName("旋转响应速度")] private float rotationSharpness = 8f;

        private Vector3 positionVelocity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.TransformPoint(localOffset);
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime);

            Vector3 lookTarget = target.position + target.forward * lookAhead;
            Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position, target.up);
            float blend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
        }
    }
}
