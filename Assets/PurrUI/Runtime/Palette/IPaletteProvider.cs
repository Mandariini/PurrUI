using System;

namespace PurrNet.UI
{
    public interface IPaletteProvider
    {
        public ColorPalette palette { get; }

        public event Action onColorChange;
    }
}
