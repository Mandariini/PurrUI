using PurrNet.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PurrNet.Editor.UI
{
    /// <summary>
    /// Editor menu that (re)creates the project-level style sheet (and its palette) at Assets/Resources/PurrUI/
    /// with the default values defined in PurrUIStyleDefaults.
    /// Project-level assets take precedence over the defaults shipped with the package.
    /// </summary>
    public static class PurrUIStyleGenerator
    {
        private const string ProjectStylesFolder = "Assets/Resources/PurrUI";

        [MenuItem("PurrUI/Create Style Sheet")]
        public static void CreateStyleAssets()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(ProjectStylesFolder);

            var palette = PurrUIStyleDefaults.CreatePalette();
            palette = (ColorPalette)SaveOrUpdateAsset(palette, ProjectStylesFolder + "/Palette.asset");

            var sheet = PurrUIStyleDefaults.CreateStyleSheet(palette, ResolveDefaultFont());
            SaveOrUpdateAsset(sheet, ProjectStylesFolder + "/StyleSheet.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PurrUI] Style sheet created/updated in " + ProjectStylesFolder + " - assign the palette (or the sheet) to your ViewStack.");
        }

        private static TMP_FontAsset ResolveDefaultFont()
        {
            // Prefer the PublicPixel font when present in the project.
            string[] guids = AssetDatabase.FindAssets("PublicPixel SDF t:TMP_FontAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font != null)
                    return font;
            }

            if (TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset;

            Debug.LogWarning("[PurrUI] No 'PublicPixel SDF' font asset and no TMP default font found - text styles will have no font.");
            return null;
        }

        private static ScriptableObject SaveOrUpdateAsset(ScriptableObject fresh, string path)
        {
            // Set the object name before saving, otherwise the asset's main object name stays empty
            // and Unity warns "Main Object Name '' does not match filename".
            fresh.name = System.IO.Path.GetFileNameWithoutExtension(path);

            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(fresh, existing);
                Object.DestroyImmediate(fresh);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(fresh, path);
            return fresh;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string name = path.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
