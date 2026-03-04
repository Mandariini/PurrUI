using UnityEngine;
using UnityEngine.UI;

namespace PurrNet.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class SignedDistanceFieldGraphic : MaskableGraphic
    {
        const string SHADER_NAME = "Hidden/PurrUI/RectangleRenderer";

        [SerializeField, HideInInspector] Shader _shader;

        [SerializeField] Texture _texture;
        [SerializeField] Color _graphicColor = Color.white;

        [SerializeField, Min(0f)] float _outlineSize;
        [SerializeField] Color _outlineColor = Color.black;

        [SerializeField, Min(0f)] float _shadowSize;
        [SerializeField, Min(0f)] float _shadowBlur;
        [SerializeField, Min(0f)] float _shadowPower = 1f;
        [SerializeField] Color _shadowColor = Color.black;

        [SerializeField, Min(0f)] float _embossSize;
        [SerializeField, Range(0f, 360f)] float _embossAngle = 135f;
        [SerializeField, Range(0f, 1f)] float _embossStrength = 0.5f;
        [SerializeField] Color _embossHighlightColor = Color.white;
        [SerializeField] Color _embossShadowColor = Color.black;

        static Material _sharedMaterial;

        public Texture texture
        {
            get => _texture;
            set { _texture = value; SetVerticesDirty(); SetMaterialDirty(); }
        }

        public Color graphicColor
        {
            get => _graphicColor;
            set { _graphicColor = value; SetVerticesDirty(); }
        }

        public float outlineSize
        {
            get => _outlineSize;
            set { _outlineSize = value; SetVerticesDirty(); }
        }

        public Color outlineColor
        {
            get => _outlineColor;
            set { _outlineColor = value; SetVerticesDirty(); }
        }

        public float shadowSize
        {
            get => _shadowSize;
            set { _shadowSize = value; SetVerticesDirty(); }
        }

        public float shadowBlur
        {
            get => _shadowBlur;
            set { _shadowBlur = value; SetVerticesDirty(); }
        }

        public float shadowPower
        {
            get => _shadowPower;
            set { _shadowPower = value; SetVerticesDirty(); }
        }

        public Color shadowColor
        {
            get => _shadowColor;
            set { _shadowColor = value; SetVerticesDirty(); }
        }

        public float embossSize
        {
            get => _embossSize;
            set { _embossSize = value; SetVerticesDirty(); }
        }

        public float embossAngle
        {
            get => _embossAngle;
            set { _embossAngle = value; SetVerticesDirty(); }
        }

        public float embossStrength
        {
            get => _embossStrength;
            set { _embossStrength = value; SetVerticesDirty(); }
        }

        public Color embossHighlightColor
        {
            get => _embossHighlightColor;
            set { _embossHighlightColor = value; SetVerticesDirty(); }
        }

        public Color embossShadowColor
        {
            get => _embossShadowColor;
            set { _embossShadowColor = value; SetVerticesDirty(); }
        }

        float extraMargin => _outlineSize + _shadowSize + _shadowBlur + 1f;

        public override Texture mainTexture => _texture ? _texture : s_WhiteTexture;

        public override Material defaultMaterial
        {
            get
            {
                if (_sharedMaterial == null && _shader != null)
                    _sharedMaterial = new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };
                return _sharedMaterial;
            }
        }

        protected override void OnEnable()
        {
            EnsureAdditionalCanvasChannels();
            base.OnEnable();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            _shader = Shader.Find(SHADER_NAME);
            EnsureAdditionalCanvasChannels();
        }

        protected override void OnValidate()
        {
            if (!_shader)
                _shader = Shader.Find(SHADER_NAME);
            base.OnValidate();
        }
#endif

        void EnsureAdditionalCanvasChannels()
        {
            if (!canvas) return;
            var rootCanvas = canvas.rootCanvas;

            const AdditionalCanvasShaderChannels required = AdditionalCanvasShaderChannels.TexCoord1
                                                            | AdditionalCanvasShaderChannels.TexCoord2
                                                            | AdditionalCanvasShaderChannels.TexCoord3
                                                            | AdditionalCanvasShaderChannels.Normal
                                                            | AdditionalCanvasShaderChannels.Tangent;

            rootCanvas.additionalShaderChannels |= required;
        }

        protected virtual Vector4 GetRoundness()
        {
            return Vector4.zero;
        }

        static Vector2 PackColor(Color color)
        {
            Color32 c = color;
            return new Vector2(c.r + c.g * 256f, c.b + c.a * 256f);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            if (width <= 0f || height <= 0f)
                return;

            float margin = extraMargin;

            var pivot = new Vector3(
                rectTransform.pivot.x * width,
                rectTransform.pivot.y * height, 0);

            // vertex.color = graphicColor x Graphic.color
            // .a serves as master alpha for all layers (CanvasGroup compatible)
            Color vertexColor = _graphicColor * color;

            var packedOutline = PackColor(_outlineColor);
            var packedShadow = PackColor(_shadowColor);

            // uv1: roundness (from subclass)
            // uv2: effect parameters
            // uv3: packed outline + shadow colors
            var roundness = GetRoundness();
            var effects = new Vector4(_outlineSize, _shadowSize, _shadowBlur, _shadowPower);
            var colors = new Vector4(
                packedOutline.x, packedOutline.y,
                packedShadow.x, packedShadow.y);

            var vertex = UIVertex.simpleVert;
            vertex.color = vertexColor;
            vertex.normal = new Vector3(_embossSize, _embossAngle * Mathf.Deg2Rad, _embossStrength);
            var packedHighlight = PackColor(_embossHighlightColor);
            var packedShadowEmb = PackColor(_embossShadowColor);
            vertex.tangent = new Vector4(packedHighlight.x, packedHighlight.y, packedShadowEmb.x, packedShadowEmb.y);
            vertex.uv1 = roundness;
            vertex.uv2 = effects;
            vertex.uv3 = colors;

            // uv0: texU, texV, width, height
            vertex.position = new Vector3(-margin, -margin) - pivot;
            vertex.uv0 = new Vector4(0, 0, width, height);
            vh.AddVert(vertex);

            vertex.position = new Vector3(-margin, height + margin) - pivot;
            vertex.uv0 = new Vector4(0, 1, width, height);
            vh.AddVert(vertex);

            vertex.position = new Vector3(width + margin, height + margin) - pivot;
            vertex.uv0 = new Vector4(1, 1, width, height);
            vh.AddVert(vertex);

            vertex.position = new Vector3(width + margin, -margin) - pivot;
            vertex.uv0 = new Vector4(1, 0, width, height);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}
