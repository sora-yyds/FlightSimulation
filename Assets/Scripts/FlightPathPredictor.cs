using UnityEngine;

namespace FlightSimulation
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FlightPathPredictor : MonoBehaviour
    {
        [Header("轨迹目标")]
        [SerializeField, InspectorName("飞机刚体")] private Rigidbody aircraftBody;
        [SerializeField, InspectorName("轨迹材质")] private Material lineMaterial;

        [Header("轨迹显示")]
        [SerializeField, InspectorName("预测时长（秒）"), Min(0.5f)] private float predictionTime = 1.5f;
        [SerializeField, InspectorName("采样点数"), Range(8, 64)] private int sampleCount = 32;
        [SerializeField, InspectorName("起点前移距离"), Min(0f)] private float startOffset = 12f;
        [SerializeField, InspectorName("静止显示长度"), Min(10f)] private float stationaryDisplaySpeed = 60f;
        [SerializeField, InspectorName("线条宽度"), Min(0.02f)] private float lineWidth = 0.5f;
        [SerializeField, InspectorName("箭头长度"), Min(0.5f)] private float arrowLength = 8f;
        [SerializeField, InspectorName("箭头宽度"), Min(0.5f)] private float arrowWidth = 4f;
        [SerializeField, InspectorName("转向预测强度"), Min(0f)] private float turnPredictionStrength = 1f;
        [SerializeField, InspectorName("曲率衰减强度"), Min(0f)] private float curvatureDecay = 3f;
        [SerializeField, InspectorName("轨迹平滑速度"), Min(0f)] private float smoothingSpeed = 12f;

        private LineRenderer pathLine;
        private LineRenderer leftArrowLine;
        private LineRenderer rightArrowLine;
        private Vector3[] points;
        private Vector3 smoothedVelocity;
        private Vector3 smoothedAngularVelocity;

        private void Awake()
        {
            if (aircraftBody == null) aircraftBody = GetComponent<Rigidbody>();
            points = new Vector3[sampleCount];
            pathLine = CreateLine("Future Path", sampleCount);
            leftArrowLine = CreateLine("Future Path Arrow Left", 2);
            rightArrowLine = CreateLine("Future Path Arrow Right", 2);
            smoothedVelocity = aircraftBody.velocity;
            smoothedAngularVelocity = aircraftBody.angularVelocity;
        }

        private void LateUpdate()
        {
            if (aircraftBody == null) return;
            if (points.Length != sampleCount)
            {
                points = new Vector3[sampleCount];
                pathLine.positionCount = sampleCount;
            }

            float smoothBlend = 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
            smoothedVelocity = Vector3.Lerp(smoothedVelocity, aircraftBody.velocity, smoothBlend);
            smoothedAngularVelocity = Vector3.Lerp(smoothedAngularVelocity, aircraftBody.angularVelocity, smoothBlend);

            float speed = smoothedVelocity.magnitude;
            Vector3 velocityDirection = speed > 1f ? smoothedVelocity.normalized : transform.forward;
            Vector3 direction = Vector3.Slerp(transform.forward, velocityDirection, Mathf.InverseLerp(0f, 25f, speed)).normalized;
            float displaySpeed = Mathf.Max(speed, stationaryDisplaySpeed);
            float stepTime = predictionTime / (sampleCount - 1);
            Vector3 position = transform.position + transform.forward * startOffset;
            Vector3 angularVelocity = smoothedAngularVelocity * turnPredictionStrength;
            float angularSpeed = angularVelocity.magnitude;
            Vector3 angularAxis = angularSpeed > 0.001f ? angularVelocity / angularSpeed : Vector3.up;

            for (int i = 0; i < sampleCount; i++)
            {
                points[i] = position;
                position += direction * (displaySpeed * stepTime);

                float progress = i / (float)(sampleCount - 1);
                float curvatureWeight = Mathf.Exp(-curvatureDecay * progress);
                if (angularSpeed > 0.001f)
                {
                    direction = Quaternion.AngleAxis(
                        angularSpeed * Mathf.Rad2Deg * stepTime * curvatureWeight,
                        angularAxis) * direction;
                }
            }

            pathLine.SetPositions(points);
            UpdateArrow(points[sampleCount - 2], points[sampleCount - 1]);
        }

        private LineRenderer CreateLine(string objectName, int pointCount)
        {
            var lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(transform, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = pointCount;
            line.sharedMaterial = lineMaterial;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void UpdateArrow(Vector3 previous, Vector3 tip)
        {
            Vector3 tangent = (tip - previous).normalized;
            Vector3 side = Vector3.Cross(tangent, transform.up);
            if (side.sqrMagnitude < 0.01f) side = transform.right;
            side.Normalize();
            Vector3 basePoint = tip - tangent * arrowLength;
            leftArrowLine.SetPosition(0, tip);
            leftArrowLine.SetPosition(1, basePoint + side * arrowWidth);
            rightArrowLine.SetPosition(0, tip);
            rightArrowLine.SetPosition(1, basePoint - side * arrowWidth);
        }
    }
}
