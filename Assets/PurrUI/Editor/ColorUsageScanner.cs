using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PurrNet.UI;
using UnityEditor;
using UnityEngine;

namespace PurrNet.Editor.UI
{
    /// <summary>
    /// Finds where a palette's colors are referenced across the project, grouped per asset, so it
    /// is obvious which colors are safe to delete and which ids references point at but the palette
    /// no longer defines. Prefabs are scanned object-by-object; scenes are scanned textually
    /// (they are not opened).
    /// </summary>
    public static class ColorUsageScanner
    {
        public class AssetUsage
        {
            public string assetPath;
            public int count;
            public readonly List<string> locations = new();

            public string assetName => Path.GetFileNameWithoutExtension(assetPath);
        }

        public class Result
        {
            /// <summary>id -> per-asset usages, in the order assets were found.</summary>
            public readonly Dictionary<int, List<AssetUsage>> byId = new();

            /// <summary>Referenced ids the palette does not define (these render magenta).</summary>
            public readonly List<int> missingIds = new();

            public int filesScanned;

            public int Count(int id) => byId.TryGetValue(id, out var list) ? Sum(list) : 0;

            public int AssetCount(int id) => byId.TryGetValue(id, out var list) ? list.Count : 0;

            public List<AssetUsage> Usages(int id) => byId.TryGetValue(id, out var list) ? list : null;

            /// <summary>"Canvas.prefab x120, InventoryView.prefab x40, ..." - for tooltips.</summary>
            public string Describe(int id, int maxAssets = 4)
            {
                var list = Usages(id);
                if (list == null || list.Count == 0)
                    return "Not referenced anywhere.";

                var sb = new StringBuilder();
                sb.Append(Count(id)).Append(" reference(s) in ").Append(list.Count).Append(" asset(s): ");
                int shown = Mathf.Min(list.Count, maxAssets);
                for (int i = 0; i < shown; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(list[i].assetName).Append(" x").Append(list[i].count);
                }

                if (list.Count > shown)
                    sb.Append(", +").Append(list.Count - shown).Append(" more");
                return sb.ToString();
            }

            static int Sum(List<AssetUsage> list)
            {
                int total = 0;
                for (int i = 0; i < list.Count; i++)
                    total += list[i].count;
                return total;
            }
        }

        // ColorInfo serializes as "enabled / color / contrast" - that trio is distinctive enough
        // to find references in files without loading them. Only enabled entries count.
        static readonly Regex InfoRegex = new Regex(
            @"enabled: 1\s*\r?\n\s+color:\s*(\d+)\s*\r?\n\s+contrast:\s*\d", RegexOptions.Compiled);

        // Prefab instance overrides store "...color" + "value: N".
        static readonly Regex OverrideRegex = new Regex(
            @"propertyPath:\s*([^\r\n]*?color)\s*\r?\n\s+value:\s*(\d+)", RegexOptions.Compiled);

        public static Result Scan(ColorPalette palette)
        {
            var result = new Result();
            var knownIds = new HashSet<int>();
            for (int i = 0; i < palette.count; i++)
                knownIds.Add(palette.colors[i].id);

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;

                bool isScene = path.EndsWith(".unity");
                bool isPrefab = path.EndsWith(".prefab");
                if (!isScene && !isPrefab)
                    continue;

                string text;
                try { text = File.ReadAllText(path); }
                catch { continue; }

                result.filesScanned++;

                if (isPrefab)
                {
                    if (InfoRegex.IsMatch(text))
                        ScanPrefab(path, result);
                    continue;
                }

                // Scenes: count without opening them.
                foreach (Match match in InfoRegex.Matches(text))
                    Record(result, int.Parse(match.Groups[1].Value), path, string.Empty);

                foreach (Match match in OverrideRegex.Matches(text))
                {
                    if (!match.Groups[1].Value.TrimEnd().EndsWith("color"))
                        continue;
                    Record(result, int.Parse(match.Groups[2].Value), path, "(instance override)");
                }
            }

            foreach (int id in result.byId.Keys)
            {
                if (!knownIds.Contains(id))
                    result.missingIds.Add(id);
            }

            result.missingIds.Sort();
            return result;
        }

        static void ScanPrefab(string path, Result result)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                        continue;

                    SerializedObject so;
                    try { so = new SerializedObject(component); }
                    catch { continue; }

                    var property = so.GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.Enum &&
                            property.propertyType != SerializedPropertyType.Integer)
                        {
                            continue;
                        }

                        if (property.name != "color")
                            continue;

                        // Only count enabled entries that are part of an enabled/color/contrast trio.
                        string parentPath = property.propertyPath.Substring(0, property.propertyPath.Length - "color".Length).TrimEnd('.');
                        var enabledProp = so.FindProperty(parentPath + ".enabled");
                        if (enabledProp == null || !enabledProp.boolValue ||
                            so.FindProperty(parentPath + ".contrast") == null)
                        {
                            continue;
                        }

                        string where = PathOf(component.transform) + "  (" + component.GetType().Name + "." + property.propertyPath + ")";
                        Record(result, property.intValue, path, where);
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void Record(Result result, int id, string assetPath, string location)
        {
            if (!result.byId.TryGetValue(id, out var list))
            {
                list = new List<AssetUsage>();
                result.byId[id] = list;
            }

            AssetUsage usage = null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].assetPath == assetPath)
                {
                    usage = list[i];
                    break;
                }
            }

            if (usage == null)
            {
                usage = new AssetUsage { assetPath = assetPath };
                list.Add(usage);
            }

            usage.count++;
            if (!string.IsNullOrEmpty(location) && !usage.locations.Contains(location))
                usage.locations.Add(location);
        }

        static string PathOf(Transform transform)
        {
            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
