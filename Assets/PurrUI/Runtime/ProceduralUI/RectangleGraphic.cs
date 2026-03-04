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
            const float minRoundness = 1f;

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            float maxRoundness = Mathf.Min(width, height) * 0.5f;

            if (_useMaxRoundness)
                return new Vector4(maxRoundness, maxRoundness, maxRoundness, maxRoundness);

            if (_uniformRoundness)
            {
                float r = _roundnessInPixels.x;
                if (r < minRoundness) r = minRoundness;
                return new Vector4(r, r, r, r);
            }

            if (_roundnessInPixels.x < minRoundness) _roundnessInPixels.x = minRoundness;
            if (_roundnessInPixels.y < minRoundness) _roundnessInPixels.y = minRoundness;
            if (_roundnessInPixels.z < minRoundness) _roundnessInPixels.z = minRoundness;
            if (_roundnessInPixels.w < minRoundness) _roundnessInPixels.w = minRoundness;

            return _roundnessInPixels;
        }
    }
}
