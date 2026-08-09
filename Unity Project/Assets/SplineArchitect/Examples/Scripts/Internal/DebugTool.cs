using UnityEngine;
using UnityEngine.SceneManagement;

namespace SplineArchitect.Examples
{
    [AddComponentMenu("")]
    public class DebugTool : MonoBehaviour
    {
        private static bool gShowDebugInfo;
        private static bool ghasSet;
        public int maxFps;
        public float fpsUpdateInterval;
        public int fpsCounterSamples = 64;
        public bool showDebugInfo;

        private float deltaTime;
        private float fpsUpdateIntervalTimer;
        private float averageFps;
        private float[] fpsContainer;
        private int fpsContainerIndex = 0;
        string currentSceneName;
        Texture2D backgroundTex;
        GUIStyle style;

        private void Start()
        {
            fpsContainer = new float[fpsCounterSamples];

            fpsUpdateIntervalTimer = fpsUpdateInterval;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = maxFps;
            if(Application.isMobilePlatform) Application.targetFrameRate = 999;

            backgroundTex = new Texture2D(1, 1);
            backgroundTex.SetPixel(0, 0, Color.white);
            backgroundTex.Apply();

            currentSceneName = SceneManager.GetActiveScene().name;

            if (!ghasSet)
            {
                ghasSet = true;
                gShowDebugInfo = showDebugInfo;
            }
        }

        void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            fpsUpdateIntervalTimer += deltaTime;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            if (SAHandleInput.IsAnyCommandKeyPressed() && SAHandleInput.IsHomeKeyDown())
                gShowDebugInfo = !gShowDebugInfo;

            if (SAHandleInput.IsAnyCommandKeyPressed() && SAHandleInput.IsPageUpKeyDown())
                LoadNextScene(true);

            if (SAHandleInput.IsAnyCommandKeyPressed() && SAHandleInput.IsPageDownKeyDown())
                LoadNextScene(false);
#else
            if (HandleInput.IsHomeKeyDown())
                gShowDebugInfo = !gShowDebugInfo;

            if (HandleInput.IsPageUpKeyDown())
                LoadNextScene(true);

            if (HandleInput.IsPageDownKeyDown())
                LoadNextScene(false);
#endif
        }

        public void LoadNextScene(bool forward)
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int totalScenes = SceneManager.sceneCountInBuildSettings;

            int next = 1;
            if (!forward) next = -1;
            if (next < 0) next = totalScenes - 1;

            SceneManager.LoadScene((currentIndex + next) % totalScenes);
            currentSceneName = SceneManager.GetActiveScene().name;
        }

        void OnGUI()
        {
            if (!gShowDebugInfo)
                return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.richText = true;
                style.fontSize = 16;
                style.normal.textColor = Color.black;
            }

            float textHeight = 25;
            float textWidth = 300;
            float textPaddingHeight = 18;
            float textPaddingWidth = 20;

            if (fpsUpdateIntervalTimer > fpsUpdateInterval)
            {
                fpsUpdateIntervalTimer = 0;

                fpsContainer[fpsContainerIndex] = 1.0f / deltaTime;
                fpsContainerIndex = (fpsContainerIndex + 1) % fpsContainer.Length;
                averageFps = 0;
                int count = 0;
                foreach (float f in fpsContainer)
                {
                    if (f > 0.001f)
                    {
                        averageFps += f;
                        count++;
                    }
                }
                averageFps = averageFps / count;
            }

            int totalSplineObjects = 0;
            int totalSplineInstances = 0;
            int totalRenderedInstances = 0;
            int totalRootSplineObjects = 0;

            foreach (Spline spline in HandleRegistry.GetSplinesUnsafe())
            {
                int instanceCount = spline.InstanceCount;

                totalSplineObjects += spline.AllSplineObjectCount;
                totalRootSplineObjects += spline.RootSplineObjectCount;
                totalSplineInstances += instanceCount;
                totalRenderedInstances += (instanceCount - spline.NoneRenderedInstanceCount);
            }

            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            GUI.DrawTexture(new Rect(10, 10, textPaddingHeight * 16, textPaddingHeight * 17), backgroundTex);

            GUI.color = Color.black;
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 1, textWidth, textHeight), $"<b>{currentSceneName} {SceneManager.GetActiveScene().buildIndex + 1}</b>", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 3, textWidth, textHeight), $"FPS: {averageFps:F1}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 4, textWidth, textHeight), $"Total splines length: {HandleRegistry.GetTotalLengthOfAllSplines()}", style);

            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 6, textWidth, textHeight), $"<b>Registry:</b>", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 7, textWidth, textHeight), $"Splines: {HandleRegistry.GetSplinesUnsafe().Count}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 8, textWidth, textHeight), $"Spline Objects: {totalSplineObjects}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 9, textWidth, textHeight), $"Spline Objects root: {totalRootSplineObjects}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 10, textWidth, textHeight), $"Spline Connectors: {HandleRegistry.GetSplineConnectorsUnsafe().Count}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 11, textWidth, textHeight), $"Spline Instances: {totalSplineInstances}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 12, textWidth, textHeight), $"Rendered instances: {totalRenderedInstances}", style);

            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 14, textWidth, textHeight), $"<b>Cached resources:</b>", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 15, textWidth, textHeight), $"Instance meshes: {HandleCachedResources.GetInstanceMeshCount()}", style);
            GUI.Label(new Rect(textPaddingWidth, textPaddingHeight * 16, textWidth, textHeight), $"Origin vertices: {HandleCachedResources.GetOriginMeshVerticesCount()}", style);
        }
    }
}
