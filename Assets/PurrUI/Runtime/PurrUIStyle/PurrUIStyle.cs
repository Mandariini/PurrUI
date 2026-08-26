using System.Collections;
using TMPro;
using TriInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PurrNet.UI
{
  /// <summary>
  /// Single styling controller: applies the matching style to a Button and/or a TMP text on the same GameObject.
  /// Button: variant base color (pushed into ColoredGraphic, palette-resolved), state tints, press animation and click sound.
  /// Text: font/size from the style sheet, color resolved from the palette when a PaletteProvider exists.
  /// Styles are also applied in edit mode via OnValidate; changed components are collected and the
  /// PurrUIStyleEditor marks them dirty so the changes persist.
  /// </summary>
  [ExecuteAlways]
  public class PurrUIStyle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
  {
    // General
    [Header("General")]
    [SerializeField] private PurrUIStyleSheet _overrideStyleSheet;

    // Button
    [Header("Button"), ShowIf(nameof(_hasButton))]
    [SerializeField, ShowIf(nameof(_hasButton))] private PurrUIButtonVariant _buttonVariant = PurrUIButtonVariant.Primary;
    [SerializeField, ShowIf(nameof(_hasButton))] private RectTransform _pressTarget;

    // Text
    [Header("Text"), ShowIf(nameof(_hasText))]
    [SerializeField, ShowIf(nameof(_hasText))] private PurrUITextStyle _textStyle = PurrUITextStyle.Body;


    private Selectable _selectable;
    private bool _hasButton => _selectable != null;
    private TMP_Text _text;
    private bool _hasText => _text != null;
    private ColoredGraphic _coloredGraphic;
    private Coroutine _pressRoutine;

    // Editor
    private PurrUIStyleSheet _subscribedSheet;
    private ColorPalette _subscribedPalette;

    private void Awake()
    {
      EnsureInit();
    }

    private void OnValidate()
    {
      if (Application.isPlaying)
        return;

      ApplyStyles();
    }

    private void OnEnable()
    {
      ApplyStyles();
    }

    private void OnDisable()
    {
      UnsubscribeFromSheet();
    }

    private void ApplyStyles()
    {
      EnsureInit();
      SubscribeToSheet(ResolveSheet());
      ApplyButtonStyle();
      ApplyTextStyle();
    }

    private void SubscribeToSheet(PurrUIStyleSheet sheet)
    {
      ColorPalette palette = sheet != null ? sheet.palette : null;

      if (_subscribedSheet == sheet && _subscribedPalette == palette)
        return;

      UnsubscribeFromSheet();

      _subscribedSheet = sheet;
      _subscribedPalette = palette;

      if (_subscribedSheet != null)
        _subscribedSheet.onChange += ApplyStyles;

      if (_subscribedPalette != null)
        _subscribedPalette.onChange += ApplyStyles;
    }

    private void UnsubscribeFromSheet()
    {
      if (_subscribedSheet != null)
      {
        _subscribedSheet.onChange -= ApplyStyles;
        _subscribedSheet = null;
      }

      if (_subscribedPalette != null)
      {
        _subscribedPalette.onChange -= ApplyStyles;
        _subscribedPalette = null;
      }
    }

    public void SetButtonVariant(PurrUIButtonVariant variant)
    {
      _buttonVariant = variant;
      ApplyButtonStyle();
    }

    public void SetTextStyle(PurrUITextStyle style)
    {
      _textStyle = style;
      ApplyTextStyle();
    }

    private void RegisterEditorDirty(Object obj)
    {
#if UNITY_EDITOR
      UnityEditor.EditorUtility.SetDirty(obj);
#endif
    }

    private PurrUIStyleSheet ResolveSheet()
    {
      if (_overrideStyleSheet != null)
        return _overrideStyleSheet;
      return PurrUIDefaults.StyleSheet;
    }

    private PurrUIButtonStyleEntry ResolveButtonEntry()
    {
      PurrUIStyleSheet sheet = ResolveSheet();
      if (sheet == null)
        return default;
      return sheet.GetButtonStyle(_buttonVariant);
    }

    private PurrUITextStyleEntry ResolveTextEntry()
    {
      PurrUIStyleSheet sheet = ResolveSheet();
      if (sheet == null)
        return default;
      return sheet.GetTextStyle(_textStyle);
    }

    private void EnsureInit()
    {
      if (_selectable == null)
        TryGetComponent(out _selectable);

      if (_text == null)
        TryGetComponent(out _text);

      if (_coloredGraphic == null)
        TryGetComponent(out _coloredGraphic);
    }

    private void ApplyButtonStyle()
    {
      if (_selectable == null)
        return;

      var entry = ResolveButtonEntry();

      // default(ColorBlock) is fully transparent, treat as "no config" and keep Unity defaults.
      if (entry.colorBlock.normalColor.a > 0f && _selectable.colors != entry.colorBlock)
      {
        _selectable.colors = entry.colorBlock;
        RegisterEditorDirty(_selectable);
      }

      if (_coloredGraphic != null)
      {
        var palette = ResolveSheet().palette;
        if (palette != null)
        {
          // Compare the stored ColorInfo, not the resolved color - palette changes flow
          // through ColoredGraphic's own palette subscription.
          ColorInfo currentInfo = _coloredGraphic.GetColor(0);
          if (currentInfo.enabled != entry.baseColor.enabled ||
              currentInfo.color != entry.baseColor.color ||
              currentInfo.contrast != entry.baseColor.contrast)
          {
            _coloredGraphic.SetColor(0, entry.baseColor);
            RegisterEditorDirty(_coloredGraphic);
          }
        }
        else if (Application.isPlaying)
        {
          _coloredGraphic.SetColor(0, entry.baseColor);
        }
      }
    }

    private void ApplyTextStyle()
    {
      if (_text == null)
        return;

      var entry = ResolveTextEntry();

      bool anyChanged = false;

      if (entry.font != null)
      {
        if (_text.font != entry.font)
        {
          _text.font = entry.font;
          anyChanged = true;
        }

        if (_text.fontSize != entry.size)
        {
          _text.fontSize = entry.size;
          anyChanged = true;
        }

        if (_text.fontStyle != entry.fontStyle)
        {
          _text.fontStyle = entry.fontStyle;
          anyChanged = true;
        }

        if (_text.lineSpacing != entry.lineSpacing)
        {
          _text.lineSpacing = entry.lineSpacing;
          anyChanged = true;
        }

        if (_text.alignment != entry.alignment)
        {
          _text.alignment = entry.alignment;
          anyChanged = true;
        }
      }

      // Color comes from the palette when a provider exists; otherwise the authored color is kept.
      var palette = ResolveSheet().palette;
      if (palette != null)
      {
        Color targetColor = entry.color.GetColor(palette);
        if (_text.color != targetColor)
        {
          _text.color = targetColor;
          anyChanged = true;
        }
      }

      if (anyChanged)
      {
        RegisterEditorDirty(_text);
      }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
      if (_selectable == null || !_selectable.interactable)
        return;

      var entry = ResolveButtonEntry();
      if (entry.clickSound != null)
        entry.clickSound.Play();

      if (entry.pressScale > 0f)
        AnimatePress(entry.pressScale, entry.pressDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
      var entry = ResolveButtonEntry();
      if (entry.pressScale > 0f)
        AnimatePress(1f, entry.pressDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      var entry = ResolveButtonEntry();
      if (entry.pressScale > 0f)
        AnimatePress(1f, entry.pressDuration);
    }

    private void AnimatePress(float targetScale, float duration)
    {
      RectTransform target = _pressTarget != null ? _pressTarget : transform as RectTransform;
      if (target == null)
        return;

      if (_pressRoutine != null)
        StopCoroutine(_pressRoutine);

      if (isActiveAndEnabled)
        _pressRoutine = StartCoroutine(PressRoutine(target, targetScale, duration));
    }

    private static IEnumerator PressRoutine(RectTransform target, float targetScale, float duration)
    {
      Vector3 from = target.localScale;
      Vector3 to = new Vector3(targetScale, targetScale, targetScale);
      float t = 0f;
      while (t < duration)
      {
        t += Time.unscaledDeltaTime;
        target.localScale = Vector3.Lerp(from, to, duration <= 0f ? 1f : Mathf.Clamp01(t / duration));
        yield return null;
      }

      target.localScale = to;
    }
  }
}
