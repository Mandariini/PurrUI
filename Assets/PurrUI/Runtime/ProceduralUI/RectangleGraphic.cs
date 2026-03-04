using UnityEngine;

namespace PurrNet.UI
{
    public class RectangleGraphic : SignedDistanceFieldGraphic
    {
        [Header("Shape")]
        [SerializeField] bool _useMaxRoundness;
        [SerializeField] bool _uniformRoundness;
        [SerializeField] Vector4 _roundnessInPixels;

        protected override Vector4 GetRoundness()
        {
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            float maxRoundness = Mathf.Min(width, height) * 0.5f;

            if (_useMaxRoundness)
                return new Vector4(maxRoundness, maxRoundness, maxRoundness, maxRoundness);

            if (_uniformRoundness)
            {
                float r = _roundnessInPixels.x;
                return new Vector4(r, r, r, r);
            }

            return _roundnessInPixels;
        }
    }
}
