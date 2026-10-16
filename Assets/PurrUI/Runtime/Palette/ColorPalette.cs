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

        // Legacy value fields: only used to seed _colors for palettes created before the list
        // existed. Safe to delete once every palette asset has been re-saved.
        [SerializeField, HideInInspector] private Color _black = Color.black;
        [SerializeField, HideInInspector] private Color _white = Color.white;
        [SerializeField, HideInInspector] private Color _muted = Color.gray;
        [SerializeField, HideInInspector] private Color _background = Color.black;
        [SerializeField, HideInInspector] private Color _foreground = Color.white;
        [SerializeField, HideInInspector] private Color _surface = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField, HideInInspector] private Color _surfaceForeground = Color.white;
        [SerializeField, HideInInspector] private Color _accent = new Color(0.1176471f, 0.5647059f, 1f, 1f);
        [SerializeField, HideInInspector] private Color _accentForeground = Color.white;
        [SerializeField, HideInInspector] private Color _success = new Color(0.2352941f, 0.7019608f, 0.4431373f, 1f);
        [SerializeField, HideInInspector] private Color _successForeground = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField, HideInInspector] private Color _warning = new Color(0.854902f, 0.6470588f, 0.1254902f, 1f);
        [SerializeField, HideInInspector] private Color _warningForeground = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField, HideInInspector] private Color _danger = new Color(0.6470588f, 0.1647059f, 0.1647059f, 1f);
        [SerializeField, HideInInspector] private Color _dangerForeground = Color.white;

        [Header("Default Styles")]
        [Tooltip("Shared styling for elements using theme defaults. Custom element settings remain local.")]
        [SerializeField] private ThemeStyles _styles = ThemeStyles.Default;

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
        /// Fills in default colors when the list is empty (palettes authored before the list
        /// existed) and assigns ids to entries that don't have one yet. Seeded ids match the
        /// legacy indices, so existing references keep resolving to the same color.
        /// </summary>
        public void EnsureSeeded()
        {
            if (_colors == null)
                _colors = new List<PaletteColor>();

            if (_colors.Count == 0)
            {
                _colors = new List<PaletteColor>
                {
                    new PaletteColor(0, "Accent", _accent, true, _accentForeground),
                    new PaletteColor(1, "Surface", _surface, true, _surfaceForeground),
                    new PaletteColor(2, "Success", _success, true, _successForeground),
                    new PaletteColor(3, "Warning", _warning, true, _warningForeground),
                    new PaletteColor(4, "Danger", _danger, true, _dangerForeground),
                    new PaletteColor(5, "Background", _background, true, _foreground),
                    new PaletteColor(6, "Black", _black),
                    new PaletteColor(7, "White", _white),
                    new PaletteColor(8, "Muted", _muted),
                };
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

        public string GetName(int id) => GetEntry(id)?.name ?? "â€”";

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
