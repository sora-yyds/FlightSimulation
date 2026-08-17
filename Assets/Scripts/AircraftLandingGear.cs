using UnityEngine;

namespace FlightSimulation
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class AircraftLandingGear : MonoBehaviour
    {
        [Header("起落架状态")]
        [SerializeField, InspectorName("初始放下起落架")] private bool startGearDown = true;
        [SerializeField, InspectorName("起落架视觉根节点")] private GameObject gearVisualRoot;

        [Header("轮位（本地坐标）")]
        [SerializeField, InspectorName("前轮位置")] private Vector3 noseGearPosition = new Vector3(0f, -0.5f, 6f);
        [SerializeField, InspectorName("左主轮位置")] private Vector3 leftGearPosition = new Vector3(-3f, -0.5f, -3.5f);
        [SerializeField, InspectorName("右主轮位置")] private Vector3 rightGearPosition = new Vector3(3f, -0.5f, -3.5f);

        [Header("悬挂")]
        [SerializeField, InspectorName("悬挂行程（米）"), Min(0.5f)] private float suspensionLength = 2.8f;
        [SerializeField, InspectorName("弹簧强度"), Min(1000f)] private float springStrength = 75000f;
        [SerializeField, InspectorName("减震强度"), Min(0f)] private float damperStrength = 18000f;
        [SerializeField, InspectorName("地面检测层")] private LayerMask groundMask = -5;

        [Header("地面操纵")]
        [SerializeField, InspectorName("前轮转向速率（度/秒）")] private float steeringRate = 24f;
        [SerializeField, InspectorName("转向响应")] private float steeringResponse = 5f;
        [SerializeField, InspectorName("侧向抓地力")] private float lateralGrip = 3.5f;
        [SerializeField, InspectorName("滚动阻力")] private float rollingResistance = 0.02f;
        [SerializeField, InspectorName("刹车强度")] private float brakeStrength = 7f;

        private readonly RaycastHit[] raycastHits = new RaycastHit[8];
        private Rigidbody body;
        private bool gearDown;
        private bool brakeInput;
        private float steeringInput;

        public bool GearDown => gearDown;
        public bool IsGrounded { get; private set; }
        public bool BrakeApplied => brakeInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            gearDown = startGearDown;
            UpdateGearVisual();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                ToggleGear();
            }

            steeringInput = GetAxis(KeyCode.D, KeyCode.A);
            brakeInput = Input.GetKey(KeyCode.Space);
        }

        private void FixedUpdate()
        {
            IsGrounded = false;
            if (!gearDown) return;

            int contactCount = 0;
            contactCount += ApplyWheel(noseGearPosition, true);
            contactCount += ApplyWheel(leftGearPosition, false);
            contactCount += ApplyWheel(rightGearPosition, false);
            IsGrounded = contactCount > 0;

            if (IsGrounded)
            {
                ApplyGroundSteering();
            }
        }

        private int ApplyWheel(Vector3 localPosition, bool steerable)
        {
            Vector3 origin = transform.TransformPoint(localPosition);
            Vector3 direction = -transform.up;
            int hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                raycastHits,
                suspensionLength,
                groundMask,
                QueryTriggerInteraction.Ignore);

            bool found = false;
            RaycastHit closestHit = default;
            float closestDistance = suspensionLength;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = raycastHits[i];
                if (hit.collider.attachedRigidbody == body || hit.distance >= closestDistance) continue;
                closestDistance = hit.distance;
                closestHit = hit;
                found = true;
            }

            if (!found) return 0;

            Vector3 pointVelocity = body.GetPointVelocity(origin);
            float compression = suspensionLength - closestHit.distance;
            float normalSpeed = Vector3.Dot(pointVelocity, closestHit.normal);
            float suspensionForce = Mathf.Max(0f, compression * springStrength - normalSpeed * damperStrength);
            body.AddForceAtPosition(closestHit.normal * suspensionForce, origin, ForceMode.Force);

            Vector3 wheelForward = Vector3.ProjectOnPlane(transform.forward, closestHit.normal).normalized;
            if (steerable && Mathf.Abs(steeringInput) > 0.001f)
            {
                wheelForward = Quaternion.AngleAxis(steeringInput * 20f, closestHit.normal) * wheelForward;
            }
            Vector3 wheelRight = Vector3.Cross(closestHit.normal, wheelForward).normalized;
            float wheelMass = body.mass / 3f;
            float sideSpeed = Vector3.Dot(pointVelocity, wheelRight);
            float forwardSpeed = Vector3.Dot(pointVelocity, wheelForward);
            Vector3 tireForce = -wheelRight * (sideSpeed * lateralGrip * wheelMass);
            tireForce -= wheelForward * (forwardSpeed * rollingResistance * wheelMass);
            if (brakeInput)
            {
                tireForce -= wheelForward * (forwardSpeed * brakeStrength * wheelMass);
            }
            body.AddForceAtPosition(tireForce, origin, ForceMode.Force);
            return 1;
        }

        private void ApplyGroundSteering()
        {
            Vector3 localAngularVelocity = transform.InverseTransformDirection(body.angularVelocity);
            float desiredYawRate = steeringInput * steeringRate * Mathf.Deg2Rad;
            float yawAcceleration = (desiredYawRate - localAngularVelocity.y) * steeringResponse;
            body.AddRelativeTorque(0f, yawAcceleration, 0f, ForceMode.Acceleration);
        }

        public void ToggleGear()
        {
            gearDown = !gearDown;
            UpdateGearVisual();
        }

        private void UpdateGearVisual()
        {
            if (gearVisualRoot != null)
            {
                gearVisualRoot.SetActive(gearDown);
            }
        }

        private static float GetAxis(KeyCode positive, KeyCode negative)
        {
            float value = 0f;
            if (Input.GetKey(positive)) value += 1f;
            if (Input.GetKey(negative)) value -= 1f;
            return value;
        }
    }
}
