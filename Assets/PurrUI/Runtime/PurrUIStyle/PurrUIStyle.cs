using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PurrNet.UI
{
  /// <summary>
  /// Single styling controller: applies the matching style to a Button and/or a TMP text on the same GameObject.
  /// Button: variant base color (pushed into ColoredGraphic, palette-resolved), state tints, press animation and click sound.
  /// Text: font/size from the style sheet, color resolved from the palette when a PaletteProvider exists.
  /// </summary>
  public class PurrUIStyle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
  {
    private bool _hasButton = false;
    private bool _hasText = false;

    // General
    [SerializeField] private PurrUIStyleSheet _overrideStyleSheet;

    // Button
    [SerializeField] private PurrUIButtonVariant _buttonVariant = PurrUIButtonVariant.Primary;
    [SerializeField] private RectTransform _pressTarget;

    // Text
    [SerializeField] private PurrUITextStyle _textStyle = PurrUITextStyle.Body;

    private Selectable _selectable;
    private TMP_Text _text;
    private ColoredGraphic _coloredGraphic;
    private Coroutine _pressRoutine;

    private void Awake()
    {
      _selectable = GetComponent<Selectable>();
      _text = GetComponent<TMP_Text>();
      _coloredGraphic = GetComponentInChildren<ColoredGraphic>(true);
    }

    private void OnEnable()
    {
      ApplyButtonStyle();
      ApplyTextStyle();
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

    private void ApplyButtonStyle()
    {
      if (_selectable == null)
        return;

      var entry = ResolveButtonEntry();

      // default(ColorBlock) is fully transparent - treat as "no config" and keep Unity defaults.
      if (entry.colorBlock.normalColor.a > 0f)
        _selectable.colors = entry.colorBlock;

      if (_coloredGraphic != null)
        _coloredGraphic.SetColor(0, entry.baseColor);
    }

    private void ApplyTextStyle()
    {
      if (_text == null)
        return;

      var entry = ResolveTextEntry();

      if (entry.font != null)
      {
        _text.font = entry.font;
        _text.fontSize = entry.size;
        _text.fontStyle = entry.fontStyle;
        _text.lineSpacing = entry.lineSpacing;
        _text.alignment = entry.alignment;
      }

      // Color comes from the palette when a provider exists; otherwise the authored color is kept.
      IPaletteProvider provider = GetComponentInParent<IPaletteProvider>();
      if (provider != null && provider.palette != null)
        _text.color = entry.color.GetColor(provider.palette);
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
