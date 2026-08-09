using UnityEngine;

using System.Collections.Generic;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("SplineArchitect/Examples/InstanceSystem/InstanceCountUi")]
    public class InstanceCountUi : MonoBehaviour
    {
        public float fpsUpdateInterval;

        private Texture2D backgroundTex;
        private GUIStyle style;
        private GUIStyle badgeStyle;

        private float deltaTime;
        private float fpsUpdateIntervalTimer;
        private int fpsContainerIndex = 0;
        private float averageFps;
        private int totalInstances;
        private int instanceUpdateInterval = 3;
        private int instanceUpdateCount;

        private float[] fpsContainer = new float[16];

        void Awake()
        {
            backgroundTex = new Texture2D(1, 1);
            backgroundTex.SetPixel(0, 0, Color.white);
            backgroundTex.Apply();
        }

        void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            fpsUpdateIntervalTimer += deltaTime;

            if (instanceUpdateInterval > instanceUpdateCount)
            {
                instanceUpdateCount = 0;
                totalInstances = 0;

                foreach (Spline spline in HandleRegistry.GetSplinesUnsafe())
                {
                    foreach (KeyValuePair<ulong, InstanceBatch> batch in spline.GetInstanceBatchesUnsafe())
                    {
                        totalInstances += batch.Value.Count;
                    }
                }
            }

            instanceUpdateCount++;
        }

        void OnGUI()
        {
            EnsureGuiStyles();
            UpdateAverageFps();

            int speed = (int)(InstanceHandler.instance.spawnAmount / 10);
            if(speed == 0) speed = 1;

            string objectTextColor =
                $"<color=#DDDDDD>Objects:</color> <color=#FFD3AE><b>{totalInstances:N0}</b></color>";

            string objectTextShadow =
                $"Objects: <b>{totalInstances:N0}</b>";

            string fpsTextColor =
                $"<color=#DDDDDD>FPS:</color> <color=#FFFFFF><b>{Mathf.RoundToInt(averageFps)}</b></color>";

            string fpsTextShadow =
                $"FPS: <b>{Mathf.RoundToInt(averageFps)}</b>";

            string multiplierTextColor =
                $"<color=#FFFFFF><b>X{speed:N0}</b></color>";

            string multiplierTextShadow =
                $"<b>X{speed:N0}</b>";

            // Panels (Objects slightly bigger)
            DrawPanel(new Rect(10f, 10f, 310f, 50f));   // wider objects box
            DrawPanel(new Rect(10f, 70f, 180f, 50f));   // FPS
            DrawPanel(new Rect(330f, 10f, 70f, 50f));   // multiplier

            // Labels
            DrawShadowLabel(new Rect(20f, 10f, 300f, 50f), objectTextShadow, objectTextColor, style);
            DrawShadowLabel(new Rect(20f, 70f, 170f, 50f), fpsTextShadow, fpsTextColor, style);
            DrawShadowLabel(new Rect(330f, 10f, 70f, 50f), multiplierTextShadow, multiplierTextColor, badgeStyle);
        }

        private void UpdateAverageFps()
        {
            if (fpsUpdateIntervalTimer <= fpsUpdateInterval)
                return;

            fpsUpdateIntervalTimer = 0f;

            if (deltaTime > 0.0001f)
                fpsContainer[fpsContainerIndex] = 1.0f / deltaTime;
            else
                fpsContainer[fpsContainerIndex] = 0f;

            fpsContainerIndex = (fpsContainerIndex + 1) % fpsContainer.Length;

            float total = 0f;
            int count = 0;

            for (int i = 0; i < fpsContainer.Length; i++)
            {
                float fps = fpsContainer[i];
                if (fps > 0.001f)
                {
                    total += fps;
                    count++;
                }
            }

            averageFps = count > 0 ? total / count : 0f;
        }

        private void EnsureGuiStyles()
        {
            if (style != null)
                return;

            style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            style.fontSize = 32;
            style.alignment = TextAnchor.MiddleLeft;
            style.normal.textColor = Color.white;

            badgeStyle = new GUIStyle(style);
            badgeStyle.alignment = TextAnchor.MiddleCenter;
            badgeStyle.fontSize = 30;
        }

        void DrawPanel(Rect rect)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, backgroundTex);
            GUI.color = previousColor;
        }

        void DrawShadowLabel(Rect rect, string shadowText, string colorText, GUIStyle guiStyle)
        {
            Color old = GUI.color;

            // Softer shadow than before
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), shadowText, guiStyle);

            // Optional second shadow layer, very soft
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), shadowText, guiStyle);

            // Main text
            GUI.color = Color.white;
            GUI.Label(rect, colorText, guiStyle);

            GUI.color = old;
        }
    }
}
