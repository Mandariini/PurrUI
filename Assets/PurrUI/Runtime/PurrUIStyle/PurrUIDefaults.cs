using UnityEngine;

namespace PurrNet.UI
{
  /// <summary>
  /// Auto-loads the PurrUI style sheet from Resources.
  /// Project-level overrides at Assets/Resources/PurrUI/ take precedence over the defaults shipped with the package.
  /// </summary>
  public static class PurrUIDefaults
  {
    private static PurrUIStyleSheet _styleSheet;

    public static PurrUIStyleSheet StyleSheet
    {
      get
      {
        if (_styleSheet == null)
        {
          // Project override first, package defaults second.
          _styleSheet = Resources.Load<PurrUIStyleSheet>("PurrUI/StyleSheet");
          if (_styleSheet == null)
            _styleSheet = Resources.Load<PurrUIStyleSheet>("PurrUIDefaults/StyleSheet");
        }

        return _styleSheet;
      }
    }
  }
}
