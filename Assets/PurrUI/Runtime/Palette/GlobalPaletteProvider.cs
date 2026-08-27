using System;
using UnityEngine;

namespace PurrNet.UI
{
  /// <summary>
  /// Fallback IPaletteProvider used when no provider exists in the hierarchy.
  /// Backed by the global palette settings (see PurrUIPaletteSettings.global).
  /// </summary>
  internal class GlobalPaletteProvider : IPaletteProvider
  {
    public static readonly GlobalPaletteProvider Instance = new();

    private ColorPalette _subscribedPalette;

    static GlobalPaletteProvider()
    {
      PurrUIPaletteSettings.onGlobalChanged += () => Instance.onColorChange?.Invoke();
    }

    private GlobalPaletteProvider() { }

    public event Action onColorChange;

    public ColorPalette palette
    {
      get
      {
        var settings = PurrUIPaletteSettings.global;
        var resolved = settings != null ? settings.palette : null;

        if (_subscribedPalette == resolved)
          return resolved;

        if (_subscribedPalette)
          _subscribedPalette.onChange -= OnPaletteChanged;

        _subscribedPalette = resolved;

        if (_subscribedPalette)
          _subscribedPalette.onChange += OnPaletteChanged;

        return resolved;
      }
    }

    private void OnPaletteChanged()
    {
      onColorChange?.Invoke();
    }
  }
}
