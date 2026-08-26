using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrNet.UI
{
  public enum PurrUIButtonVariant
  {
    Primary = 0,
    Secondary = 1,
    Tab = 2,
    Row = 3,
    Danger = 4
  }

  public enum PurrUITextStyle
  {
    Header = 0,
    Title = 1,
    Body = 2,
    Caption = 3,
    Label = 4,
    Button = 5
  }

  [Serializable]
  public struct PurrUIButtonStyleEntry
  {
    public PurrUIButtonVariant variant;

    [Tooltip("Base color of the button, resolved from the palette via ColoredGraphic.")]
    public ColorInfo baseColor;

    [Tooltip("Multiplier tints applied on top of the base color per state (keep grayscale).")]
    public ColorBlock colorBlock;

    [Range(0.5f, 1f)]
    public float pressScale;

    public float pressDuration;

    public AudioSessionPreset clickSound;
  }

  [Serializable]
  public struct PurrUITextStyleEntry
  {
    public PurrUITextStyle style;

    [Tooltip("Optional. When null, the authored font is kept.")]
    public TMP_FontAsset font;

    public float size;

    [Tooltip("Resolved from the palette when a PaletteProvider exists in the hierarchy; otherwise the authored color is kept.")]
    public ColorInfo color;

    public FontStyles fontStyle;

    public float lineSpacing;

    public TextAlignmentOptions alignment;
  }

  /// <summary>
  /// The single styling source for PurrUI: bundles a ColorPalette together with button and text styles.
  /// Assign it to a ViewStack (or any PaletteProvider) and every PurrUIStyle follows it.
  /// </summary>
  [CreateAssetMenu(fileName = "PurrUIStyleSheet", menuName = "PurrUI/Style Sheet")]
  public class PurrUIStyleSheet : ScriptableObject
  {
    [Header("Palette")]
    [Tooltip("Palette used by all ColoredGraphics and style entries. Referenced, not duplicated.")]
    public ColorPalette palette;

    [Header("Buttons")]
    [SerializeField] private PurrUIButtonStyleEntry[] _buttonStyles;

    [Header("Text")]
    [SerializeField] private PurrUITextStyleEntry[] _textStyles;

    public PurrUIButtonStyleEntry GetButtonStyle(PurrUIButtonVariant variant)
    {
      if (_buttonStyles != null)
      {
        for (int i = 0; i < _buttonStyles.Length; i++)
        {
          if (_buttonStyles[i].variant == variant)
            return _buttonStyles[i];
        }
      }

      return default;
    }

    public PurrUITextStyleEntry GetTextStyle(PurrUITextStyle style)
    {
      if (_textStyles != null)
      {
        for (int i = 0; i < _textStyles.Length; i++)
        {
          if (_textStyles[i].style == style)
            return _textStyles[i];
        }
      }

      return default;
    }

    public void SetButtonStyles(PurrUIButtonStyleEntry[] entries)
    {
      _buttonStyles = entries;
    }

    public void SetTextStyles(PurrUITextStyleEntry[] entries)
    {
      _textStyles = entries;
    }
  }
}
