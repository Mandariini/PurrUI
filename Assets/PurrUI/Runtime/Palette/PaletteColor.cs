using System;
using UnityEngine;

namespace PurrNet.UI
{
    /// <summary>
    /// One named color in a <see cref="ColorPalette"/>. Entries carry a stable <see cref="id"/>;
    /// a <see cref="ColorInfo"/> stores that id (as its ColorType value), so colors can be added,
    /// removed or reordered without breaking existing references.
    /// </summary>
    [Serializable]
    public class PaletteColor
    {
        [Tooltip("Stable reference id. Referenced by ColorInfo; never change it.")]
        [SerializeField] private int _id;

        [Tooltip("Shown in color pickers and in the palette inspector.")]
        public string name = "Color";

        public Color color = Color.white;

        [Tooltip("Optional foreground to draw on top of this color (Material's 'on-' color). " +
                 "When off, a dark/light contrast is derived from the color's luminance.")]
        public bool hasContrast;
        public Color contrast = Color.white;

        [Tooltip("Keeps the entry out of color pickers. Existing references still resolve; it just " +
                 "won't be offered for new ones. Use this instead of deleting a color that is still used.")]
        public bool hidden;

        /// <summary>Stable id used by <see cref="ColorInfo"/>. Assigned automatically, never reused.</summary>
        public int id
        {
            get => _id;
            set => _id = value;
        }

        public PaletteColor() { }

        public PaletteColor(int id, string name, Color color, bool hasContrast = false, Color contrast = default, bool hidden = false)
        {
            _id = id;
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
