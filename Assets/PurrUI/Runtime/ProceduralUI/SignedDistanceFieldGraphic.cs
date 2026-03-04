using UnityEngine;
using UnityEngine.UI;

namespace PurrNet.UI
{
    public enum GradientType
    {
        None,
        Vertical,
        Horizontal,
        Radial,
        Gradient
    }

    public enum RadialMode
    {
        CircleCover,
        CircleFit,
        Ellipse
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public class SignedDistanceFieldGraphic : MaskableGraphic
    {
        const string SHADER_NAME = "Hidden/PurrUI/RectangleRenderer";

        [SerializeField, HideInInspector] Shader _shader;

        [SerializeField] Texture _texture;
        [SerializeField] Color _graphicColor = Color.white;
        [SerializeField] GradientType _gradientType;
        [SerializeField] Color _gradientColor = Color.white;
        [SerializeField, Range(2, 20)] int _gradientQuality = 8;
        [SerializeField] RadialMode _radialMode;
        [SerializeField, Min(0.01f)] float _gradientSize = 1f;
        [SerializeField] Gradient _gradient;
        [SerializeField, Range(0f, 360f)] float _gradientAngle;

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

        public GradientType gradientType
        {
            get => _gradientType;
            set { _gradientType = value; SetVerticesDirty(); }
        }

        public Color gradientColor
        {
            get => _gradientColor;
            set { _gradientColor = value; SetVerticesDirty(); }
        }

        public int gradientQuality
        {
            get => _gradientQuality;
            set { _gradientQuality = Mathf.Clamp(value, 2, 20); SetVerticesDirty(); }
        }

        public RadialMode radialMode
        {
            get => _radialMode;
            set { _radialMode = value; SetVerticesDirty(); }
        }

        public float gradientSize
        {
            get => _gradientSize;
            set { _gradientSize = Mathf.Max(0.01f, value); SetVerticesDirty(); }
        }

        public Gradient gradient
        {
            get => _gradient;
            set { _gradient = value; SetVerticesDirty(); }
        }

        public float gradientAngle
        {
            get => _gradientAngle;
            set { _gradientAngle = value; SetVerticesDirty(); }
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

        public static Vector2 PackColor(Color color)
        {
            Color32 c = color;
            return new Vector2(c.r + c.g * 256f, c.b + c.a * 256f);
        }

        UIVertex BuildBaseVertex()
        {
            var packedOutline = PackColor(_outlineColor);
            var packedShadow = PackColor(_shadowColor);

            var roundness = GetRoundness();
            var effects = new Vector4(_outlineSize, _shadowSize, _shadowBlur, _shadowPower);
            var colors = new Vector4(
                packedOutline.x, packedOutline.y,
                packedShadow.x, packedShadow.y);

            var vertex = UIVertex.simpleVert;
            vertex.normal = new Vector3(_embossSize, _embossAngle * Mathf.Deg2Rad, _embossStrength);
            var packedHighlight = PackColor(_embossHighlightColor);
            var packedShadowEmb = PackColor(_embossShadowColor);
            vertex.tangent = new Vector4(packedHighlight.x, packedHighlight.y, packedShadowEmb.x, packedShadowEmb.y);
            vertex.uv1 = roundness;
            vertex.uv2 = effects;
            vertex.uv3 = colors;

            return vertex;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            if (width <= 0f || height <= 0f)
                return;

            if (_gradientType == GradientType.Radial ||
                _gradientType == GradientType.Gradient)
            {
                PopulateSubdividedMesh(vh, width, height);
                return;
            }

            float margin = extraMargin;

            var pivot = new Vector3(
                rectTransform.pivot.x * width,
                rectTransform.pivot.y * height, 0);

            Color colorA = _graphicColor * color;
            Color colorB = _gradientType != GradientType.None ? _gradientColor * color : colorA;

            Color c0, c1, c2, c3;
            switch (_gradientType)
            {
                case GradientType.Vertical:
                    c0 = colorB; c1 = colorA; c2 = colorA; c3 = colorB;
                    break;
                case GradientType.Horizontal:
                    c0 = colorA; c1 = colorA; c2 = colorB; c3 = colorB;
                    break;
                default:
                    c0 = c1 = c2 = c3 = colorA;
                    break;
            }

            var vertex = BuildBaseVertex();

            vertex.color = c0;
            vertex.position = new Vector3(-margin, -margin) - pivot;
            vertex.uv0 = new Vector4(0, 0, width, height);
            vh.AddVert(vertex);

            vertex.color = c1;
            vertex.position = new Vector3(-margin, height + margin) - pivot;
            vertex.uv0 = new Vector4(0, 1, width, height);
            vh.AddVert(vertex);

            vertex.color = c2;
            vertex.position = new Vector3(width + margin, height + margin) - pivot;
            vertex.uv0 = new Vector4(1, 1, width, height);
            vh.AddVert(vertex);

            vertex.color = c3;
            vertex.position = new Vector3(width + margin, -margin) - pivot;
            vertex.uv0 = new Vector4(1, 0, width, height);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        void PopulateSubdividedMesh(VertexHelper vh, float width, float height)
        {
            int n = Mathf.Clamp(_gradientQuality, 2, 20);
            float margin = extraMargin;

            var pivot = new Vector3(
                rectTransform.pivot.x * width,
                rectTransform.pivot.y * height, 0);

            Color masterColor = color;
            Color colorA = _graphicColor * masterColor;
            Color colorB = _gradientColor * masterColor;

            float angleRad = _gradientAngle * Mathf.Deg2Rad;
            var gradDir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            var vertex = BuildBaseVertex();

            // Build (n+1) x (n+1) vertex grid
            int cols = n + 1;
            for (int y = 0; y <= n; y++)
            {
                float fy = (float)y / n;
                for (int x = 0; x <= n; x++)
                {
                    float fx = (float)x / n;

                    // Position: lerp across padded rect
                    float posX = Mathf.Lerp(-margin, width + margin, fx);
                    float posY = Mathf.Lerp(-margin, height + margin, fy);
                    vertex.position = new Vector3(posX, posY, 0) - pivot;

                    // UV: normalized [0,1] mapped through same padding as the 4-vert path
                    float texU = Mathf.Lerp(0f, 1f, fx);
                    float texV = Mathf.Lerp(0f, 1f, fy);
                    vertex.uv0 = new Vector4(texU, texV, width, height);

                    // Compute vertex color based on gradient type
                    switch (_gradientType)
                    {
                        case GradientType.Radial:
                        {
                            float t;
                            if (_radialMode == RadialMode.Ellipse)
                            {
                                float dx = texU - 0.5f;
                                float dy = texV - 0.5f;
                                t = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                            }
                            else
                            {
                                float refDim = _radialMode == RadialMode.CircleCover
                                    ? Mathf.Max(width, height)
                                    : Mathf.Min(width, height);
                                float dx = (texU - 0.5f) * width;
                                float dy = (texV - 0.5f) * height;
                                t = Mathf.Sqrt(dx * dx + dy * dy) / (refDim * 0.5f);
                            }
                            vertex.color = Color.Lerp(colorA, colorB, Mathf.Clamp01(t / _gradientSize));
                            break;
                        }
                        case GradientType.Gradient:
                        {
                            // Project UV onto angle direction, remap from [-0.5,0.5] to [0,1]
                            float t = Mathf.Clamp01((texU - 0.5f) * gradDir.x + (texV - 0.5f) * gradDir.y + 0.5f);
                            vertex.color = (_gradient != null ? _gradient.Evaluate(t) : Color.white) * masterColor;
                            break;
                        }
                    }

                    vh.AddVert(vertex);
                }
            }

            // Triangles: standard grid
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = y * cols + x;
                    vh.AddTriangle(i, i + cols, i + cols + 1);
                    vh.AddTriangle(i + cols + 1, i + 1, i);
                }
            }
        }
    }
}
