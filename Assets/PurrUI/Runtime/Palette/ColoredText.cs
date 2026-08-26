using TMPro;
using UnityEngine;

namespace PurrNet.UI
{
    /// <summary>
    /// Exposes a TMP text's face color through IColored, so a ColoredGraphic component can drive it
    /// with the exact same palette pipeline used for rectangle graphics (ColorInfo resolution,
    /// palette-change updates, transitions and editor-time applying all come from ColoredGraphic).
    /// Add ColoredGraphic to the same GameObject and assign this component as its target.
    /// </summary>
    public class ColoredText : MonoBehaviour, IColored
    {
        [SerializeField] private TMP_Text _text;

        private static readonly string[] _keys = { "Text" };

        public string[] keys => _keys;

        public Color GetColor(int keyIndex)
        {
            if (keyIndex != 0)
                throw new System.ArgumentOutOfRangeException(nameof(keyIndex), keyIndex, "ColoredText exposes a single color slot (index must be 0).");
            return _text != null ? _text.color : Color.white;
        }

        public void SetColor(int keyIndex, Color color)
        {
            if (keyIndex != 0)
                throw new System.ArgumentOutOfRangeException(nameof(keyIndex), keyIndex, "ColoredText exposes a single color slot (index must be 0).");
            if (_text != null)
                _text.color = color;
        }

        private void Reset()
        {
            _text = GetComponent<TMP_Text>();
        }
    }
}
