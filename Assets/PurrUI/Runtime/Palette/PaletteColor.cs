using System;
using UnityEngine;

namespace PurrNet.UI
{
    /// <summary>
    /// One named color in a <see cref="ColorPalette"/>. A palette stores an ordered list of these,
    /// and a <see cref="ColorInfo"/> references one by index (the value of its ColorType).
    /// </summary>
    [Serializable]
    public class PaletteColor
    {
        [Tooltip("Shown in color pickers and in the palette inspector.")]
        public string name = "Color";

        public Color color = Color.white;

        [Tooltip("Optional foreground to draw on top of this color (Material's 'on-' color). " +
                 "When off, a dark/light contrast is derived from the color's luminance.")]
        public bool hasContrast;
        public Color contrast = Color.white;

        [Tooltip("Keeps the entry out of color pickers. Existing references still resolve; it just " +
                 "won't be offered for new ones.")]
        public bool hidden;

        public PaletteColor() { }

        public PaletteColor(string name, Color color, bool hasContrast = false, Color contrast = default, bool hidden = false)
        {
            this.name = name;
            this.color = color;
            this.hasContrast = hasContrast;
            this.contrast = contrast;
            this.hidden = hidden;
        }

        /// <summary>Explicit contrast when set, otherwise a black/white foreground by luminance.</summary>
        public Color ResolveContrast()
        {
            if (hasContrast)
                return contrast;

            float luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
            return luminance > 0.5f ? new Color(0.1f, 0.1f, 0.1f, 1f) : Color.white;
        }
    }
}
