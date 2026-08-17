using UnityEngine;

namespace FlightSimulation
{
    public sealed class FlightDebugHud : MonoBehaviour
    {
        [SerializeField, InspectorName("飞机控制器")] private ArcadeJetController aircraft;
        [SerializeField, InspectorName("面板位置")] private Vector2 position = new Vector2(18f, 18f);
        [SerializeField, InspectorName("显示操作提示")] private bool showControls = true;

        private GUIStyle labelStyle;
        private GUIStyle warningStyle;

        public void SetAircraft(ArcadeJetController newAircraft)
        {
            aircraft = newAircraft;
        }

        private void OnGUI()
        {
            if (aircraft == null) return;

            EnsureStyles();
            float x = position.x;
            float y = position.y;
            GUI.Label(new Rect(x, y, 340f, 28f), $"空速  {aircraft.Airspeed * 3.6f:0} km/h", labelStyle);
            GUI.Label(new Rect(x, y + 26f, 340f, 28f), $"高度  {aircraft.Altitude:0} m", labelStyle);
            GUI.Label(new Rect(x, y + 52f, 340f, 28f), $"油门  {aircraft.Throttle * 100f:0}%", labelStyle);
            GUI.Label(new Rect(x, y + 78f, 340f, 28f), $"迎角  {aircraft.AngleOfAttack:0.0}°", labelStyle);
            GUI.Label(new Rect(x, y + 104f, 340f, 28f), $"侧滑  {aircraft.SideslipAngle:0.0}°", labelStyle);
            GUI.Label(new Rect(x, y + 130f, 420f, 28f), $"机头航向  {aircraft.NoseHeading:000}°", labelStyle);
            GUI.Label(new Rect(x, y + 156f, 420f, 28f), $"实际航迹  {aircraft.GroundTrackHeading:000}°", labelStyle);
            GUI.Label(new Rect(x, y + 182f, 420f, 28f), $"航迹偏差  {aircraft.TrackAngleError:+0.0;-0.0;0.0}°", labelStyle);
            GUI.Label(new Rect(x, y + 208f, 420f, 28f), $"起落架  {(aircraft.GearDown ? "放下" : "收起")}", labelStyle);

            string stallButtonText = aircraft.StallEnabled ? "失速：开启" : "失速：关闭";
            if (GUI.Button(new Rect(x, y + 240f, 150f, 34f), stallButtonText))
            {
                aircraft.SetStallEnabled(!aircraft.StallEnabled);
            }

            if (aircraft.IsStalled)
            {
                GUI.Label(new Rect(x, y + 282f, 340f, 38f), "失速警告", warningStyle);
            }

            if (showControls)
            {
                float bottom = Screen.height - 88f;
                GUI.Label(new Rect(18f, bottom, 950f, 28f), "W/S 油门    A/D 偏航/地面转向    Num 4/6 横滚    Num 8/5 俯仰    G 起落架    空格 刹车", labelStyle);
            }
        }

        private void EnsureStyles()
        {
            if (labelStyle != null) return;

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = Color.white;

            warningStyle = new GUIStyle(labelStyle)
            {
                fontSize = 28
            };
            warningStyle.normal.textColor = new Color(1f, 0.25f, 0.15f);
        }
    }
}
