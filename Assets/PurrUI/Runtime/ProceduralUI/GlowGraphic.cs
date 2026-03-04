using UnityEngine;
using UnityEngine.UI;

namespace PurrNet.UI
{
    public class GlowGraphic : SignedDistanceFieldGraphic
    {
        [SerializeField] RectangleGraphic _source;
        [SerializeField, Min(0f)] float _extraSize;

        [SerializeField] bool _useMaxRoundness;
        [SerializeField] bool _uniformRoundness;
        [SerializeField] Vector4 _roundnessInPixels;

        [SerializeField] Color _glowColor = Color.white;
        [SerializeField] GradientType _glowGradientType;
        [SerializeField] Color _glowGradientColor = Color.white;
        [SerializeField, Range(2, 20)] int _glowGradientQuality = 8;
        [SerializeField] RadialMode _glowRadialMode;
        [SerializeField, Min(0.01f)] float _glowGradientSize = 1f;
        [SerializeField] Gradient _glowGradient;
        [SerializeField, Range(0f, 360f)] float _glowGradientAngle;

        [SerializeField, Min(0f)] float _spread;
        [SerializeField, Min(0f)] float _blur;
        [SerializeField, Min(0f)] float _power = 1f;

        public RectangleGraphic source
        {
            get => _source;
            set { _source = value; TrackSource(); SetVerticesDirty(); }
        }

        public float extraSize
        {
            get => _extraSize;
            set { _extraSize = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        public bool useMaxRoundness
        {
            get => _useMaxRoundness;
            set { _useMaxRoundness = value; SetVerticesDirty(); }
        }

        public bool uniformRoundness
        {
            get => _uniformRoundness;
            set { _uniformRoundness = value; SetVerticesDirty(); }
        }

        public Vector4 roundnessInPixels
        {
            get => _roundnessInPixels;
            set { _roundnessInPixels = value; SetVerticesDirty(); }
        }

        public Color glowColor
        {
            get => _glowColor;
            set { _glowColor = value; SetVerticesDirty(); }
        }

        public GradientType glowGradientType
        {
            get => _glowGradientType;
            set { _glowGradientType = value; SetVerticesDirty(); }
        }

        public Color glowGradientColor
        {
            get => _glowGradientColor;
            set { _glowGradientColor = value; SetVerticesDirty(); }
        }

        public int glowGradientQuality
        {
            get => _glowGradientQuality;
            set { _glowGradientQuality = Mathf.Clamp(value, 2, 20); SetVerticesDirty(); }
        }

        public RadialMode glowRadialMode
        {
            get => _glowRadialMode;
            set { _glowRadialMode = value; SetVerticesDirty(); }
        }

        public float glowGradientSize
        {
            get => _glowGradientSize;
            set { _glowGradientSize = Mathf.Max(0.01f, value); SetVerticesDirty(); }
        }

        public Gradient glowGradient
        {
            get => _glowGradient;
            set { _glowGradient = value; SetVerticesDirty(); }
        }

        public float glowGradientAngle
        {
            get => _glowGradientAngle;
            set { _glowGradientAngle = value; SetVerticesDirty(); }
        }

        public float spread
        {
            get => _spread;
            set { _spread = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        public float blur
        {
            get => _blur;
            set { _blur = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        public float power
        {
            get => _power;
            set { _power = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        RectangleGraphic _trackedSource;

        void TrackSource()
        {
            if (_trackedSource == _source)
                return;

            if (_trackedSource)
                _trackedSource.UnregisterDirtyVerticesCallback(OnSourceChanged);

            _trackedSource = _source;

            if (_trackedSource)
                _trackedSource.RegisterDirtyVerticesCallback(OnSourceChanged);
        }

        void OnSourceChanged()
        {
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            TrackSource();
        }

        protected override void OnDisable()
        {
            if (_trackedSource)
                _trackedSource.UnregisterDirtyVerticesCallback(OnSourceChanged);
            _trackedSource = null;
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            TrackSource();
        }
#endif

        protected override Vector4 GetRoundness()
        {
            if (_source)
            {
                float sw = _source.rectTransform.rect.width + _extraSize * 2f;
                float sh = _source.rectTransform.rect.height + _extraSize * 2f;
                float maxR = Mathf.Min(sw, sh) * 0.5f;

                if (_source.useMaxRoundness)
                    return new Vector4(maxR, maxR, maxR, maxR);

                if (_source.uniformRoundness)
                {
                    float r = _source.roundnessInPixels.x;
                    return new Vector4(r, r, r, r);
                }

                return _source.roundnessInPixels;
            }

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

        void GetEffectiveDimensions(out float width, out float height)
        {
            if (_source)
            {
                width = _source.rectTransform.rect.width + _extraSize * 2f;
                height = _source.rectTransform.rect.height + _extraSize * 2f;
            }
            else
            {
                width = rectTransform.rect.width + _extraSize * 2f;
                height = rectTransform.rect.height + _extraSize * 2f;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            float rectW = rectTransform.rect.width;
            float rectH = rectTransform.rect.height;

            if (rectW <= 0f || rectH <= 0f)
                return;

            GetEffectiveDimensions(out float width, out float height);

            if (_glowGradientType == GradientType.Radial ||
                _glowGradientType == GradientType.Gradient)
            {
                PopulateGlowSubdivided(vh, width, height);
                return;
            }

            PopulateGlowQuad(vh, width, height);
        }

        UIVertex BuildGlowVertex(Color glowCol)
        {
            var packedGlow = PackColor(glowCol);
            var roundness = GetRoundness();

            var vertex = UIVertex.simpleVert;
            vertex.uv1 = roundness;
            vertex.uv2 = new Vector4(0f, _spread, _blur, _power);
            vertex.uv3 = new Vector4(0f, 0f, packedGlow.x, packedGlow.y);
            vertex.normal = new Vector3(-_blur, 0f, 0f); // negative = fill softness
            vertex.tangent = Vector4.zero;
            return vertex;
        }

        void PopulateGlowQuad(VertexHelper vh, float width, float height)
        {
            float margin = _spread + _blur + 1f;

            float rectW = rectTransform.rect.width;
            float rectH = rectTransform.rect.height;

            // Center the shape on the rect
            float offX = (rectW - width) * 0.5f;
            float offY = (rectH - height) * 0.5f;

            var pivot = new Vector3(
                rectTransform.pivot.x * rectW,
                rectTransform.pivot.y * rectH, 0);

            Color colorA = _glowColor * color;
            Color colorB = _glowGradientType != GradientType.None
                ? _glowGradientColor * color
                : colorA;

            Color c0, c1, c2, c3;
            switch (_glowGradientType)
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

            AddGlowVert(vh, c0, new Vector3(offX - margin, offY - margin) - pivot,
                new Vector4(0, 0, width, height));
            AddGlowVert(vh, c1, new Vector3(offX - margin, offY + height + margin) - pivot,
                new Vector4(0, 1, width, height));
            AddGlowVert(vh, c2, new Vector3(offX + width + margin, offY + height + margin) - pivot,
                new Vector4(1, 1, width, height));
            AddGlowVert(vh, c3, new Vector3(offX + width + margin, offY - margin) - pivot,
                new Vector4(1, 0, width, height));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        void AddGlowVert(VertexHelper vh, Color col, Vector3 pos, Vector4 uv0)
        {
            var vertex = BuildGlowVertex(col);
            vertex.color = col;
            vertex.position = pos;
            vertex.uv0 = uv0;
            vh.AddVert(vertex);
        }

        void PopulateGlowSubdivided(VertexHelper vh, float width, float height)
        {
            int n = Mathf.Clamp(_glowGradientQuality, 2, 20);
            float margin = _spread + _blur + 1f;

            float rectW = rectTransform.rect.width;
            float rectH = rectTransform.rect.height;

            float offX = (rectW - width) * 0.5f;
            float offY = (rectH - height) * 0.5f;

            var pivot = new Vector3(
                rectTransform.pivot.x * rectW,
                rectTransform.pivot.y * rectH, 0);

            Color masterColor = color;
            Color colorA = _glowColor * masterColor;
            Color colorB = _glowGradientColor * masterColor;

            float angleRad = _glowGradientAngle * Mathf.Deg2Rad;
            var gradDir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            var roundness = GetRoundness();

            int cols = n + 1;
            for (int y = 0; y <= n; y++)
            {
                float fy = (float)y / n;
                for (int x = 0; x <= n; x++)
                {
                    float fx = (float)x / n;

                    float posX = Mathf.Lerp(offX - margin, offX + width + margin, fx);
                    float posY = Mathf.Lerp(offY - margin, offY + height + margin, fy);

                    float texU = fx;
                    float texV = fy;

                    Color col;
                    switch (_glowGradientType)
                    {
                        case GradientType.Radial:
                        {
                            float t;
                            if (_glowRadialMode == RadialMode.Ellipse)
                            {
                                float dx = texU - 0.5f;
                                float dy = texV - 0.5f;
                                t = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                            }
                            else
                            {
                                float refDim = _glowRadialMode == RadialMode.CircleCover
                                    ? Mathf.Max(width, height)
                                    : Mathf.Min(width, height);
                                float dx = (texU - 0.5f) * width;
                                float dy = (texV - 0.5f) * height;
                                t = Mathf.Sqrt(dx * dx + dy * dy) / (refDim * 0.5f);
                            }
                            col = Color.Lerp(colorA, colorB, Mathf.Clamp01(t / _glowGradientSize));
                            break;
                        }
                        case GradientType.Gradient:
                        {
                            float t = Mathf.Clamp01(
                                (texU - 0.5f) * gradDir.x + (texV - 0.5f) * gradDir.y + 0.5f);
                            col = (_glowGradient != null ? _glowGradient.Evaluate(t) : Color.white)
                                  * masterColor;
                            break;
                        }
                        default:
                            col = colorA;
                            break;
                    }

                    var packedGlow = PackColor(col);

                    var vertex = UIVertex.simpleVert;
                    vertex.color = col;
                    vertex.position = new Vector3(posX, posY, 0) - pivot;
                    vertex.uv0 = new Vector4(texU, texV, width, height);
                    vertex.uv1 = roundness;
                    vertex.uv2 = new Vector4(0f, _spread, _blur, _power);
                    vertex.uv3 = new Vector4(0f, 0f, packedGlow.x, packedGlow.y);
                    vertex.normal = new Vector3(-_blur, 0f, 0f);
                    vertex.tangent = Vector4.zero;
                    vh.AddVert(vertex);
                }
            }

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
