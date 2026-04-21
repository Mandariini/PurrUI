using System;
using UnityEngine;

namespace PurrNet.UI
{
    public class PaletteProvider : MonoBehaviour, IPaletteProvider
    {
        [SerializeField] private ColorPalette _colorPalette;

        public event Action onColorChange;

        public ColorPalette palette
        {
            get
            {
                return _colorPalette;
            }
            set
            {
                var old = _colorPalette;
                if (old)
                    old.onChange -= OnColorPaletteDirty;
                _colorPalette = value;
                onColorChange?.Invoke();
                _colorPalette.onChange += OnColorPaletteDirty;
            }
        }

        private void Awake()
        {
            if (_colorPalette)
                _colorPalette.onChange += OnColorPaletteDirty;
        }

        private void OnColorPaletteDirty()
        {
            onColorChange?.Invoke();
        }

        private void OnDestroy()
        {
            if (_colorPalette)
                _colorPalette.onChange -= OnColorPaletteDirty;
        }
    }
}
