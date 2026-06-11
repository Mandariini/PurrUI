using UnityEngine;

namespace PurrNet.UI
{
#if UNITY_6000_0_OR_NEWER
    [AddComponentMenu("UI (Canvas)/PurrUI/Rectangle Graphic")]
#else
    [AddComponentMenu("UI/PurrUI/Rectangle Graphic")]
#endif
    public class RectangleGraphic : SignedDistanceFieldGraphic
    {
        [SerializeField] bool _useMaxRoundness;
        [SerializeField] bool _uniformRoundness;
        [SerializeField] Vector4 _roundnessInPixels;

        public bool useMaxRoundness
        {
            get => _useMaxRoundness;
            set { if (_useMaxRoundness == value) return; _useMaxRoundness = value; SetVerticesDirty(); }
        }

        public bool uniformRoundness
        {
            get => _uniformRoundness;
            set { if (_uniformRoundness == value) return; _uniformRoundness = value; SetVerticesDirty(); }
        }

        public Vector4 roundnessInPixels
        {
            get => _roundnessInPixels;
            set { if (_roundnessInPixels == value) return; _roundnessInPixels = value; SetVerticesDirty(); }
        }

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
