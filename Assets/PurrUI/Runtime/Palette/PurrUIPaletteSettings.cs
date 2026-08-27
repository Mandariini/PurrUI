using System;
using UnityEngine;

namespace PurrNet.UI
{
  /// <summary>
  /// Project-wide palette configuration, used as a fallback when no IPaletteProvider
  /// exists in the hierarchy (see ColoredGraphic).
  ///
  /// Create one via PurrUI &gt; Global Palette Settings (writes to
  /// Assets/Resources/PurrUI/PaletteSettings.asset) or via
  /// Create &gt; PurrNet &gt; PurrUI &gt; Palette Settings. It is discovered automatically
  /// from Resources, or you can register it explicitly with
  /// PurrUIPaletteSettings.global = settings.
  /// </summary>
  [CreateAssetMenu(menuName = "PurrNet/PurrUI/Palette Settings", order = 501)]
  public class PurrUIPaletteSettings : ScriptableObject
  {
    [SerializeField] private ColorPalette _palette;

    private static PurrUIPaletteSettings _global;

    /// <summary>
    /// Raised when the global settings instance is replaced.
    /// </summary>
    public static event Action onGlobalChanged;

    /// <summary>
    /// The global palette settings. Assign a specific asset explicitly,
    /// or leave it null to auto-discover from Resources
    /// (Assets/Resources/PurrUI/PaletteSettings.asset first, then the
    /// package default at PurrUIDefaults/PaletteSettings).
    /// </summary>
    public static PurrUIPaletteSettings global
    {
      get
      {
        if (_global == null)
        {
          _global = Resources.Load<PurrUIPaletteSettings>("PurrUI/PaletteSettings");
          if (_global == null)
            _global = Resources.Load<PurrUIPaletteSettings>("PurrUIDefaults/PaletteSettings");
        }

        return _global;
      }
      set
      {
        if (_global == value)
          return;

        _global = value;
        onGlobalChanged?.Invoke();
      }
    }

    public ColorPalette palette
    {
      get => _palette;
      set => _palette = value;
    }
  }
}
