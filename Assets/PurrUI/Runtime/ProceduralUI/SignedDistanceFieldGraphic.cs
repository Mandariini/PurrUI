using UnityEngine;
using UnityEngine.UI;

namespace PurrNet.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class SignedDistanceFieldGraphic : MaskableGraphic
    {
        static readonly int SIZE = Shader.PropertyToID("_Size");
        static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");
        static readonly int GRAPHIC_BLUR = Shader.PropertyToID("_GraphicBlur");
        static readonly int OUTLINE_SIZE = Shader.PropertyToID("_OutlineSize");
        static readonly int OUTLINE_COLOR = Shader.PropertyToID("_OutlineColor");
        static readonly int SHADOW_SIZE = Shader.PropertyToID("_ShadowSize");
        static readonly int SHADOW_BLUR = Shader.PropertyToID("_ShadowBlur");
        static readonly int SHADOW_POW = Shader.PropertyToID("_ShadowPow");
        static readonly int SHADOW_COLOR = Shader.PropertyToID("_ShadowColor");
        static readonly int PADDING = Shader.PropertyToID("_Padding");

        const string SHADER_NAME = "Hidden/PurrUI/RectangleRenderer";

        [SerializeField, HideInInspector] Shader _shader;

        [Header("Graphic")]
        [SerializeField] Texture _texture;
        [SerializeField] float _graphicBlur;

        [Header("Outline")]
        [SerializeField, Min(0f)] float _outlineSize;
        [SerializeField] Color _outlineColor = Color.black;

        [Header("Shadow")]
        [SerializeField, Min(0f)] float _shadowSize;
        [SerializeField, Min(0f)] float _shadowBlur;
        [SerializeField, Min(0f)] float _shadowPower = 1f;
        [SerializeField] Color _shadowColor = Color.black;

        Material _material;

        float extraMargin => Mathf.Max(_outlineSize, _shadowSize);

        public override Texture mainTexture => _texture ? _texture : s_WhiteTexture;

        public override Material defaultMaterial
        {
            get
            {
                if (_material == null && _shader != null)
                    _material = new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };
                return _material;
            }
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            _shader = Shader.Find(SHADER_NAME);
        }

        protected override void OnValidate()
        {
            if (!_shader)
                _shader = Shader.Find(SHADER_NAME);
            base.OnValidate();
        }
#endif

        protected override void OnDestroy()
        {
            if (_material != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(_material);
#else
                Destroy(_material);
#endif
                _material = null;
            }
            base.OnDestroy();
        }

        protected virtual void UpdateMaterialProperties()
        {
            var mat = defaultMaterial;
            if (mat == null) return;

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            mat.SetVector(SIZE, new Vector2(width, height));
            mat.SetTexture(MAIN_TEX, mainTexture);
            mat.SetFloat(GRAPHIC_BLUR, _graphicBlur);
            mat.SetFloat(OUTLINE_SIZE, _outlineSize);
            mat.SetColor(OUTLINE_COLOR, _outlineColor);
            mat.SetFloat(SHADOW_SIZE, _shadowSize);
            mat.SetFloat(SHADOW_BLUR, _shadowBlur);
            mat.SetFloat(SHADOW_POW, _shadowPower);
            mat.SetColor(SHADOW_COLOR, _shadowColor);
            mat.SetFloat(PADDING, extraMargin);
        }

        public override void SetMaterialDirty()
        {
            base.SetMaterialDirty();
            UpdateMaterialProperties();
        }

        public override void SetLayoutDirty()
        {
            base.SetLayoutDirty();
            UpdateMaterialProperties();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            UpdateMaterialProperties();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            float margin = extraMargin;

            Vector3 pivot = new Vector3(
                rectTransform.pivot.x * width,
                rectTransform.pivot.y * height, 0);

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            vertex.position = new Vector3(-margin, -margin) - pivot;
            vertex.uv0 = new Vector2(0, 0);
            vh.AddVert(vertex);

            vertex.position = new Vector3(-margin, height + margin) - pivot;
            vertex.uv0 = new Vector2(0, 1);
            vh.AddVert(vertex);

            vertex.position = new Vector3(width + margin, height + margin) - pivot;
            vertex.uv0 = new Vector2(1, 1);
            vh.AddVert(vertex);

            vertex.position = new Vector3(width + margin, -margin) - pivot;
            vertex.uv0 = new Vector2(1, 0);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}
