using System;
using System.Collections.Generic;
using UnityEngine;

namespace PurrNet.UI
{
    [Serializable]
    [CreateAssetMenu(menuName = "PurrNet/PurrUI/Color Palette", order = 500)]
    public class ColorPalette : ScriptableObject
    {
        [Header("Colors")]
        [Tooltip("The project's colors. Each entry has a stable id that references point at, " +
                 "so entries can be added, removed or reordered freely.")]
        [SerializeField] private List<PaletteColor> _colors = new();

        [Header("Default Styles")]
        [Tooltip("Shared styling for elements using theme defaults. Custom element settings remain local.")]
        [SerializeField] private ThemeStyles _styles = ThemeStyles.Default;

        /// <summary>
        /// Roles the palette preview can illustrate. A palette assigns one of its colors to each
        /// (or leaves it empty); this only affects the preview, never runtime rendering.
        /// </summary>
        public enum PreviewRole
        {
            None = -1,
            Scrim = 0,
            Shadow = 1,
            Border = 2,
            Trail = 3
        }

        [Header("Preview roles")]
        [Tooltip("Color id illustrated as each preview role, or -1 for none. Preview only.")]
        [SerializeField] private int _previewScrim = -1;
        [SerializeField] private int _previewShadow = -1;
        [SerializeField] private int _previewBorder = -1;
        [SerializeField] private int _previewTrail = -1;

        /// <summary>The color id assigned to a preview role, or -1 when unassigned.</summary>
        public int GetPreviewColor(PreviewRole role)
        {
            switch (role)
            {
                case PreviewRole.Scrim: return _previewScrim;
                case PreviewRole.Shadow: return _previewShadow;
                case PreviewRole.Border: return _previewBorder;
                case PreviewRole.Trail: return _previewTrail;
                default: return -1;
            }
        }

        public void SetPreviewColor(PreviewRole role, int colorId)
        {
            switch (role)
            {
                case PreviewRole.Scrim: _previewScrim = colorId; break;
                case PreviewRole.Shadow: _previewShadow = colorId; break;
                case PreviewRole.Border: _previewBorder = colorId; break;
                case PreviewRole.Trail: _previewTrail = colorId; break;
                default: return;
            }

            onChange?.Invoke();
        }

        public ThemeStyles styles => _styles;

        public event Action onChange;

        /// <summary>Number of colors in the palette.</summary>
        public int count => _colors?.Count ?? 0;

        /// <summary>The palette's colors, in display order.</summary>
        public IReadOnlyList<PaletteColor> colors => _colors;

        private void OnEnable()
        {
            EnsureSeeded();
        }

        private void OnValidate()
        {
            EnsureSeeded();
            _styles = _styles.Sanitize();
            onChange?.Invoke();
        }

        /// <summary>
        /// Fills in the default colors when the list is empty, and assigns ids to entries that
        /// don't have one yet. Seeded ids match the conventional role numbers, so they line up
        /// with the ColorType values code uses.
        /// </summary>
        public void EnsureSeeded()
        {
            if (_colors == null)
                _colors = new List<PaletteColor>();

            if (_colors.Count == 0)
            {
                _colors = CreateDefaultColors();
                return;
            }

            // Backfill ids for entries saved before ids existed: by position, which equals the
            // index their references used.
            bool needsBackfill = true;
            for (int i = 0; i < _colors.Count; i++)
            {
                if (_colors[i].id != 0)
                {
                    needsBackfill = false;
                    break;
                }
            }

            if (needsBackfill)
            {
                for (int i = 0; i < _colors.Count; i++)
                    _colors[i].id = i;
            }
        }

        /// <summary>
        /// The palette a fresh asset starts with: the conventional core roles, matching
        /// ColorType's numbering. Projects add their own colors after these.
        /// </summary>
        public static List<PaletteColor> CreateDefaultColors()
        {
            return new List<PaletteColor>
            {
                new PaletteColor(0, "Accent", new Color(0.24313726f, 0.47843137f, 0.45490196f, 1f), true, new Color(0.90588236f, 0.93333334f, 0.9254902f, 1f)),
                new PaletteColor(1, "Surface", new Color(0.13725491f, 0.16862746f, 0.1882353f, 1f), true, new Color(0.7882353f, 0.8156863f, 0.8039216f, 1f)),
                new PaletteColor(2, "Success", new Color(0.43137255f, 0.6117647f, 0.44705883f, 1f), true, new Color(0.047058824f, 0.0627451f, 0.07450981f, 1f)),
                new PaletteColor(3, "Warning", new Color(0.7254902f, 0.56078434f, 0.2901961f, 1f), true, new Color(0.047058824f, 0.0627451f, 0.07450981f, 1f)),
                new PaletteColor(4, "Danger", new Color(0.6313726f, 0.23529412f, 0.22352941f, 1f), true, new Color(0.92941177f, 0.9019608f, 0.89411765f, 1f)),
                new PaletteColor(5, "Background", new Color(0.039215688f, 0.05490196f, 0.0627451f, 1f), true, new Color(0.8784314f, 0.8980392f, 0.8862745f, 1f)),
                new PaletteColor(6, "Black", new Color(0.023529412f, 0.039215688f, 0.047058824f, 1f)),
                new PaletteColor(7, "White", new Color(0.8784314f, 0.8980392f, 0.8862745f, 1f)),
                new PaletteColor(8, "Muted", new Color(0.4392157f, 0.4862745f, 0.4745098f, 1f)),
            };
        }

        /// <summary>The entry with this id, or null.</summary>
        public PaletteColor GetEntry(int id)
        {
            if (_colors == null)
                return null;

            for (int i = 0; i < _colors.Count; i++)
            {
                if (_colors[i].id == id)
                    return _colors[i];
            }

            return null;
        }

        public bool IsValid(int id) => GetEntry(id) != null;

        public string GetName(int id) => GetEntry(id)?.name ?? "—";

        public bool IsHidden(int id) => GetEntry(id)?.hidden ?? false;

        public Color GetColor(ColorType type) => GetColor((int)type);

        public Color GetColor(int id)
        {
            var entry = GetEntry(id);
            if (entry == null)
            {
                Debug.LogWarning($"[PurrUI] ColorPalette '{name}' has no color with id {id}; using magenta.", this);
                return Color.magenta;
            }

            return entry.color;
        }

        public Color GetContrast(ColorType type) => GetContrast((int)type);

        public Color GetContrast(int id)
        {
            var entry = GetEntry(id);
            return entry != null ? entry.ResolveContrast() : Color.white;
        }

        public void SetColor(ColorType type, Color color) => SetColor((int)type, color);

        public void SetColor(int id, Color color)
        {
            var entry = GetEntry(id);
            if (entry == null)
                return;

            entry.color = color;
            onChange?.Invoke();
        }

        public void SetContrast(ColorType type, Color color) => SetContrast((int)type, color);

        /// <summary>Sets (and enables) the entry's foreground color.</summary>
        public void SetContrast(int id, Color color)
        {
            var entry = GetEntry(id);
            if (entry == null)
                return;

            entry.hasContrast = true;
            entry.contrast = color;
            onChange?.Invoke();
        }

        /// <summary>
        /// Appends a project color with a fresh id. Never reuses ids, so existing references are
        /// unaffected by the addition.
        /// </summary>
        public PaletteColor AddColor(string name, Color color, bool hasContrast = false, Color contrast = default)
        {
            var entry = new PaletteColor(NextId(), name, color, hasContrast, contrast);
            _colors.Add(entry);
            onChange?.Invoke();
            return entry;
        }

        /// <summary>
        /// Removes a color by id. References to it stop resolving (magenta + a warning at runtime),
        /// so prefer hiding a color that is still used.
        /// </summary>
        public bool RemoveColor(int id)
        {
            for (int i = 0; i < _colors.Count; i++)
            {
                if (_colors[i].id != id)
                    continue;

                _colors.RemoveAt(i);
                onChange?.Invoke();
                return true;
            }

            return false;
        }

        public void SetStyles(ThemeStyles value)
        {
            _styles = value.Sanitize();
            onChange?.Invoke();
        }

        private int NextId()
        {
            int max = -1;
            for (int i = 0; i < _colors.Count; i++)
                max = Mathf.Max(max, _colors[i].id);

            return max + 1;
        }
    }
}
