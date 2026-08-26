using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrNet.UI
{
  /// <summary>
  /// Single source of truth for the default PurrUI style values.
  /// Used by the editor generator to (re)create project-level style assets.
  /// </summary>
  public static class PurrUIStyleDefaults
  {
    public static ColorPalette CreatePalette()
    {
      // Field initializers provide sensible defaults.
      return ScriptableObject.CreateInstance<ColorPalette>();
    }

    public static PurrUIStyleSheet CreateStyleSheet(ColorPalette palette, TMP_FontAsset font)
    {
      var sheet = ScriptableObject.CreateInstance<PurrUIStyleSheet>();
      sheet.palette = palette;

      var buttonStyles = new PurrUIButtonStyleEntry[5];
      for (int i = 0; i < buttonStyles.Length; i++)
      {
        var variant = (PurrUIButtonVariant)i;
        buttonStyles[i] = new PurrUIButtonStyleEntry
        {
          variant = variant,
          baseColor = new ColorInfo { enabled = true, color = DefaultBaseColorType(variant), contrast = false },
          colorBlock = DefaultTintBlock(),
          pressScale = 0.95f,
          pressDuration = 0.08f,
          clickSound = null
        };
      }
      sheet.SetButtonStyles(buttonStyles);

      var textStyles = new PurrUITextStyleEntry[6];
      float[] sizes = { 40f, 32f, 28f, 20f, 32f, 26f };
      for (int i = 0; i < textStyles.Length; i++)
      {
        textStyles[i] = new PurrUITextStyleEntry
        {
          style = (PurrUITextStyle)i,
          font = font,
          size = sizes[i],
          color = new ColorInfo { enabled = true, color = ColorType.White, contrast = false },
          fontStyle = FontStyles.Normal,
          lineSpacing = 0f,
          alignment = TextAlignmentOptions.Center
        };
      }
      sheet.SetTextStyles(textStyles);

      return sheet;
    }

    private static ColorType DefaultBaseColorType(PurrUIButtonVariant variant)
    {
      switch (variant)
      {
        case PurrUIButtonVariant.Secondary: return ColorType.Surface;
        case PurrUIButtonVariant.Tab: return ColorType.White;
        case PurrUIButtonVariant.Danger: return ColorType.Danger;
        default: return ColorType.Accent;
      }
    }

    private static ColorBlock DefaultTintBlock()
    {
      return new ColorBlock
      {
        normalColor = Color.white,
        highlightedColor = new Color(0.9608f, 0.9608f, 0.9608f, 1f),
        pressedColor = new Color(0.7843f, 0.7843f, 0.7843f, 1f),
        selectedColor = new Color(0.9608f, 0.9608f, 0.9608f, 1f),
        disabledColor = new Color(0.7843f, 0.7843f, 0.7843f, 0.502f),
        colorMultiplier = 1f,
        fadeDuration = 0.1f
      };
    }
  }
}
