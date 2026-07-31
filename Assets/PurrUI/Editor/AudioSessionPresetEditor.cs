using System.Linq;
using PurrNet.UI;
using UnityEditor;
using UnityEngine;

namespace PurrNet.Editor.UI
{
    [CustomEditor(typeof(AudioSessionPreset))]
    [CanEditMultipleObjects]
    public class AudioSessionPresetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            foreach (var t in targets)
            {
                var preset = (AudioSessionPreset)t;
                bool hasClips = preset.clips != null && preset.clips.Length > 0;
                var label = $"Test ({preset.name})";

                using (new EditorGUI.DisabledScope(!hasClips))
                {
                    if (GUILayout.Button(label, GUILayout.Height(24f)))
                        preset.Play();
                }

                if (!hasClips)
                    EditorGUILayout.HelpBox($"{preset.name}: add at least one AudioClip to preview.", MessageType.Info);
            }
        }

        [MenuItem("Assets/Create/PurrNet/PurrUI/AudioSessionPreset", false, 500)]
        static void CreateAudioSessionPreset()
        {
            var clips = Selection.objects.OfType<AudioClip>().ToArray();

            var preset = CreateInstance<AudioSessionPreset>();
            if (clips.Length > 0)
                preset.clips = clips;

            var folder = GetSelectionFolder();
            var defaultName = clips.Length == 1 ? clips[0].name : "NewAudioSessionPreset";
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{defaultName}.asset");

            ProjectWindowUtil.CreateAsset(preset, path);
        }

        static string GetSelectionFolder()
        {
            foreach (var obj in Selection.objects)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (AssetDatabase.IsValidFolder(path))
                    return path;

                var folder = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(folder))
                    return folder.Replace('\\', '/');
            }

            return "Assets";
        }
    }
}
