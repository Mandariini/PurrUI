using PurrNet.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PurrNet.Editor.UI
{
    static class PurrUIMenuItems
    {
#if UNITY_6000_0_OR_NEWER
        const string UI_CREATE_PATH = "GameObject/UI (Canvas)/";
#else
        const string UI_CREATE_PATH = "GameObject/UI/";
#endif

        const string PALETTE_SETTINGS_PATH = "Assets/Resources/PurrUI/PaletteSettings.asset";

        [MenuItem("PurrUI/Global Palette Settings", false, 100)]
        static void OpenGlobalPaletteSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PurrUIPaletteSettings>(PALETTE_SETTINGS_PATH);

            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                if (!AssetDatabase.IsValidFolder("Assets/Resources/PurrUI"))
                    AssetDatabase.CreateFolder("Assets/Resources", "PurrUI");

                settings = ScriptableObject.CreateInstance<PurrUIPaletteSettings>();
                AssetDatabase.CreateAsset(settings, PALETTE_SETTINGS_PATH);
                AssetDatabase.SaveAssets();
                PurrUIPaletteSettings.global = settings;
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem(UI_CREATE_PATH + "PurrUI Rectangle", false, 2100)]
        static void CreateRectangle(MenuCommand menuCommand)
        {
            CreateUIElement<RectangleGraphic>("Rectangle", menuCommand);
        }

        [MenuItem(UI_CREATE_PATH + "PurrUI Glow", false, 2101)]
        static void CreateGlow(MenuCommand menuCommand)
        {
            CreateUIElement<GlowGraphic>("Glow", menuCommand);
        }

        static void CreateUIElement<T>(string name, MenuCommand menuCommand) where T : Graphic
        {
            var parent = menuCommand.context as GameObject;

            if (parent == null || parent.GetComponentInParent<Canvas>() == null)
            {
#if UNITY_2022_1_OR_NEWER
                var canvas = Object.FindAnyObjectByType<Canvas>();
#else
                var canvas = Object.FindObjectOfType<Canvas>();
#endif
                if (canvas != null && canvas.gameObject.activeInHierarchy)
                    parent = canvas.gameObject;
                else
                    parent = CreateCanvas();
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            GameObjectUtility.SetParentAndAlign(go, parent);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100f, 100f);

            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            Selection.activeGameObject = go;
        }

        static GameObject CreateCanvas()
        {
            var canvasGo = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            EnsureEventSystem();

            return canvasGo;
        }

        static void EnsureEventSystem()
        {
#if UNITY_2022_1_OR_NEWER
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;
#else
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;
#endif

            var esGo = new GameObject("EventSystem",
                typeof(EventSystem), typeof(StandaloneInputModule));

            Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
        }
    }
}
