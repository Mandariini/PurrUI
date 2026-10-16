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
        [Tooltip("The project's colors. Order defines the numbering (ColorInfo stores an index), " +
                 "so append new colors at the end rather than inserting them in the middle.")]
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

        /// <summary>Number of colors in the palette (= the valid ColorInfo indices).</summary>
        public int count => _colors?.Count ?? 0;

        /// <summary>The palette's colors, in index order.</summary>
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
        /// Fills in the default colors when the list is still empty, so palettes authored before
        /// the list existed keep their values.
        /// </summary>
        public void EnsureSeeded()
        {
            if (_colors != null && _colors.Count > 0)
                return;

            _colors = new List<PaletteColor>
            {
                new PaletteColor("Accent", _accent, true, _accentForeground),
                new PaletteColor("Surface", _surface, true, _surfaceForeground),
                new PaletteColor("Success", _success, true, _successForeground),
                new PaletteColor("Warning", _warning, true, _warningForeground),
                new PaletteColor("Danger", _danger, true, _dangerForeground),
                new PaletteColor("Background", _background, true, _foreground),
                new PaletteColor("Black", _black),
                new PaletteColor("White", _white),
                new PaletteColor("Muted", _muted),
            };
        }

        public bool IsValid(int index) => index >= 0 && index < count;

        public string GetName(int index) => IsValid(index) ? _colors[index].name : "â€”";

        public bool IsHidden(int index) => IsValid(index) && _colors[index].hidden;

        public Color GetColor(ColorType type) => GetColor((int)type);

        public Color GetColor(int index)
        {
            if (!IsValid(index))
            {
                Debug.LogWarning($"[PurrUI] ColorPalette '{name}' has no color at index {index}; using magenta.", this);
                return Color.magenta;
            }

            return _colors[index].color;
        }

        public Color GetContrast(ColorType type) => GetContrast((int)type);

        public Color GetContrast(int index)
        {
            if (!IsValid(index))
                return Color.white;

            return _colors[index].ResolveContrast();
        }

        public void SetColor(ColorType type, Color color) => SetColor((int)type, color);

        public void SetColor(int index, Color color)
        {
            if (!IsValid(index))
                return;

            _colors[index].color = color;
            onChange?.Invoke();
        }

        public void SetContrast(ColorType type, Color color) => SetContrast((int)type, color);

        /// <summary>Sets (and enables) the entry's foreground color.</summary>
        public void SetContrast(int index, Color color)
        {
            if (!IsValid(index))
                return;

            _colors[index].hasContrast = true;
            _colors[index].contrast = color;
            onChange?.Invoke();
        }

        /// <summary>Appends a project color. Appending keeps every existing index valid.</summary>
        public int AddColor(string name, Color color, bool hasContrast = false, Color contrast = default)
        {
            _colors.Add(new PaletteColor(name, color, hasContrast, contrast));
            onChange?.Invoke();
            return _colors.Count - 1;
        }

        /// <summary>
        /// Removes a color. Everything after it shifts down, so any ColorInfo pointing past
        /// this index starts referencing a different color â€” only do this at the end, or remap usages.
        /// </summary>
        public void RemoveColor(int index)
        {
            if (!IsValid(index))
                return;

            _colors.RemoveAt(index);
            onChange?.Invoke();
        }

        public void SetStyles(ThemeStyles value)
        {
            _styles = value.Sanitize();
            onChange?.Invoke();
        }
    }
}
