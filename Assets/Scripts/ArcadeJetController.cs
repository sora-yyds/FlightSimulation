using UnityEngine;

namespace FlightSimulation
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArcadeJetController : MonoBehaviour
    {
        [Header("飞机参数")]
        [SerializeField, InspectorName("质量（千克）")] private float mass = 16000f;
        [SerializeField, InspectorName("机翼面积（平方米）")] private float wingArea = 62f;
        [SerializeField, InspectorName("空气密度")] private float airDensity = 1.225f;
        [SerializeField, InspectorName("最大推力（牛）")] private float maxThrust = 85000f;

        [Header("空气动力")]
        [SerializeField, InspectorName("启用失速")] private bool stallEnabled = true;
        [SerializeField, InspectorName("每度升力斜率")] private float liftSlopePerDegree = 0.075f;
        [SerializeField, InspectorName("零升力迎角")] private float zeroLiftAngle = -2f;
        [SerializeField, InspectorName("失速迎角")] private float stallAngle = 18f;
        [SerializeField, InspectorName("失速后升力比例")] private float postStallLift = 0.35f;
        [SerializeField, InspectorName("基础阻力系数")] private float baseDragCoefficient = 0.06f;
        [SerializeField, InspectorName("诱导阻力系数")] private float inducedDrag = 0.075f;
        [SerializeField, InspectorName("侧向阻力")] private float sideDrag = 2.5f;
        [SerializeField, InspectorName("关闭失速时速度跟随强度")] private float noStallVelocityAlignment = 4f;
        [SerializeField, InspectorName("关闭失速时最大升力系数")] private float noStallMaxLiftCoefficient = 1.35f;

        [Header("飞行操纵")]
        [SerializeField, InspectorName("俯仰速率（度/秒）")] private float pitchRate = 65f;
        [SerializeField, InspectorName("偏航速率（度/秒）")] private float yawRate = 28f;
        [SerializeField, InspectorName("横滚速率（度/秒）")] private float rollRate = 115f;
        [SerializeField, InspectorName("操纵响应")] private float controlResponse = 6f;
        [SerializeField, InspectorName("协调转弯强度")] private float coordinatedTurnStrength = 1f;
        [SerializeField, InspectorName("侧滑修正强度")] private float sideslipCorrection = 1.8f;
        [SerializeField, InspectorName("最低操纵权")] private float minimumControlAuthority = 0.2f;
        [SerializeField, InspectorName("完整操纵权空速")] private float fullControlSpeed = 75f;
        [SerializeField, InspectorName("油门变化速率")] private float throttleChangeRate = 0.35f;

        [Header("初始状态")]
        [SerializeField, InspectorName("初始油门"), Range(0f, 1f)] private float initialThrottle = 0.72f;
        [SerializeField, InspectorName("初始空速（米/秒）")] private float initialAirspeed = 110f;

        private Rigidbody body;
        private AircraftLandingGear landingGear;
        private float pitchInput;
        private float yawInput;
        private float rollInput;
        private float throttleInput;
        private float angleOfAttack;
        private float sideslipAngle;
        private float liftCoefficient;
        private bool stalled;

        public float Throttle { get; private set; }
        public float Airspeed { get; private set; }
        public float Altitude => transform.position.y;
        public float AngleOfAttack => angleOfAttack;
        public float SideslipAngle => sideslipAngle;
        public float GroundSpeed { get; private set; }
        public float NoseHeading { get; private set; }
        public float GroundTrackHeading { get; private set; }
        public float TrackAngleError => Mathf.DeltaAngle(NoseHeading, GroundTrackHeading);
        public bool StallEnabled => stallEnabled;
        public bool IsStalled => stalled;
        public bool GearDown => landingGear != null && landingGear.GearDown;
        public bool IsGrounded => landingGear != null && landingGear.IsGrounded;

        public void SetStallEnabled(bool enabled)
        {
            stallEnabled = enabled;
            if (!stallEnabled) stalled = false;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            landingGear = GetComponent<AircraftLandingGear>();
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 8f;
            Throttle = initialThrottle;
        }

        private void Start()
        {
            if (body.velocity.sqrMagnitude < 1f)
            {
                body.velocity = transform.forward * initialAirspeed;
            }
        }

        private void Update()
        {
            throttleInput = GetAxis(KeyCode.W, KeyCode.S);
            yawInput = GetAxis(KeyCode.D, KeyCode.A);
            rollInput = GetAxis(KeyCode.Keypad6, KeyCode.Keypad4);
            pitchInput = GetAxis(KeyCode.Keypad5, KeyCode.Keypad8);

            Throttle = Mathf.Clamp01(Throttle + throttleInput * throttleChangeRate * Time.deltaTime);
        }

        private void FixedUpdate()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(body.velocity);
            Airspeed = localVelocity.magnitude;

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.velocity, Vector3.up);
            GroundSpeed = horizontalVelocity.magnitude;
            NoseHeading = HeadingFromDirection(Vector3.ProjectOnPlane(transform.forward, Vector3.up));
            if (horizontalVelocity.sqrMagnitude > 0.01f)
            {
                GroundTrackHeading = HeadingFromDirection(horizontalVelocity);
            }

            float forwardSpeed = Mathf.Max(0.1f, localVelocity.z);
            angleOfAttack = Mathf.Atan2(-localVelocity.y, forwardSpeed) * Mathf.Rad2Deg;
            sideslipAngle = Mathf.Atan2(localVelocity.x, forwardSpeed) * Mathf.Rad2Deg;

            ApplyThrust();
            ApplyAerodynamics(localVelocity);
            ApplyNoStallAssist();
            ApplyFlightControls();
        }

        private void ApplyThrust()
        {
            body.AddForce(transform.forward * (maxThrust * Throttle), ForceMode.Force);
        }

        private void ApplyAerodynamics(Vector3 localVelocity)
        {
            float dynamicPressure = 0.5f * airDensity * Airspeed * Airspeed;
            float effectiveAngle = angleOfAttack - zeroLiftAngle;
            float stallRatio = Mathf.Abs(effectiveAngle) / Mathf.Max(0.1f, stallAngle);
            float linearLiftCoefficient = liftSlopePerDegree * effectiveAngle;
            if (stallEnabled)
            {
                float stalledLiftCoefficient = Mathf.Sign(effectiveAngle) * liftSlopePerDegree * stallAngle * postStallLift;
                liftCoefficient = stallRatio <= 1f
                    ? linearLiftCoefficient
                    : Mathf.Lerp(linearLiftCoefficient, stalledLiftCoefficient, Mathf.Clamp01(stallRatio - 1f));
                stalled = stallRatio > 1f && Airspeed > 15f;
            }
            else
            {
                liftCoefficient = Mathf.Clamp(linearLiftCoefficient, -noStallMaxLiftCoefficient, noStallMaxLiftCoefficient);
                stalled = false;
            }

            float lift = dynamicPressure * wingArea * liftCoefficient;
            Vector3 airflowDirection = Airspeed > 0.1f ? body.velocity.normalized : transform.forward;
            Vector3 liftDirection = Vector3.ProjectOnPlane(transform.up, airflowDirection).normalized;
            if (liftDirection.sqrMagnitude < 0.01f) liftDirection = transform.up;
            body.AddForce(liftDirection * lift, ForceMode.Force);

            float inducedLiftCoefficient = stallEnabled ? liftCoefficient : Mathf.Clamp(liftCoefficient, -0.8f, 0.8f);
            float dragCoefficient = baseDragCoefficient + inducedDrag * inducedLiftCoefficient * inducedLiftCoefficient;
            if (Airspeed > 0.1f)
            {
                Vector3 drag = -body.velocity.normalized * (dynamicPressure * wingArea * dragCoefficient);
                body.AddForce(drag, ForceMode.Force);
            }

            float lateralForce = -localVelocity.x * Mathf.Abs(localVelocity.x) * sideDrag * airDensity;
            body.AddForce(transform.right * lateralForce, ForceMode.Force);
        }

        private void ApplyNoStallAssist()
        {
            if (stallEnabled || Airspeed < 1f) return;

            Vector3 targetVelocity = transform.forward * Airspeed;
            Vector3 velocityChange = (targetVelocity - body.velocity) * noStallVelocityAlignment;
            body.AddForce(velocityChange, ForceMode.Acceleration);
        }

        private void ApplyFlightControls()
        {
            float authority = Mathf.Lerp(minimumControlAuthority, 1f, Mathf.Clamp01(Airspeed / fullControlSpeed));
            bool grounded = landingGear != null && landingGear.IsGrounded;
            float flightControlBlend = grounded ? Mathf.InverseLerp(35f, 70f, Airspeed) : 1f;
            float manualYawRate = grounded ? 0f : yawInput * yawRate * authority;
            float coordinatedYawRate = grounded ? 0f : CalculateCoordinatedYawRate();
            float sideslipYawRate = grounded ? 0f : sideslipAngle * sideslipCorrection;
            float desiredYawRate = manualYawRate + coordinatedYawRate + sideslipYawRate;

            Vector3 desiredLocalAngularVelocity = new Vector3(
                -pitchInput * pitchRate * authority * flightControlBlend,
                desiredYawRate,
                -rollInput * rollRate * authority * flightControlBlend) * Mathf.Deg2Rad;

            Vector3 localAngularVelocity = transform.InverseTransformDirection(body.angularVelocity);
            Vector3 angularAcceleration = (desiredLocalAngularVelocity - localAngularVelocity) * controlResponse;
            body.AddRelativeTorque(angularAcceleration, ForceMode.Acceleration);
        }

        private float CalculateCoordinatedYawRate()
        {
            if (GroundSpeed < 10f) return 0f;

            Vector3 localWorldUp = transform.InverseTransformDirection(Vector3.up);
            float bankAngle = Mathf.Atan2(-localWorldUp.x, localWorldUp.y);
            float turnRateRadians = Physics.gravity.magnitude * Mathf.Tan(bankAngle) / GroundSpeed;
            return turnRateRadians * Mathf.Rad2Deg * coordinatedTurnStrength;
        }

        private static float HeadingFromDirection(Vector3 direction)
        {
            return Mathf.Repeat(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 360f);
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
