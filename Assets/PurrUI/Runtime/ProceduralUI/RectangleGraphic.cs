using UnityEngine;

namespace PurrNet.UI
{
    public class RectangleGraphic : SignedDistanceFieldGraphic
    {
        static readonly int ROUNDNESS = Shader.PropertyToID("_Roundness");

        [Header("Shape")]
        [SerializeField] bool _useMaxRoundness;
        [SerializeField] bool _uniformRoundness;
        [SerializeField] Vector4 _roundnessInPixels;

        protected override void UpdateMaterialProperties()
        {
            base.UpdateMaterialProperties();

            var mat = defaultMaterial;
            if (mat == null) return;

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            float maxRoundness = Mathf.Min(width, height) * 0.5f;

            Vector4 roundness;

            if (_useMaxRoundness)
            {
                roundness = new Vector4(maxRoundness, maxRoundness, maxRoundness, maxRoundness);
            }
            else if (_uniformRoundness)
            {
                float r = _roundnessInPixels.x;
                roundness = new Vector4(r, r, r, r);
            }
            else
            {
                roundness = _roundnessInPixels;
            }

            mat.SetVector(ROUNDNESS, roundness);
        }
    }
}
