using UnityEngine;

namespace PurrNet.UI
{
    [ExecuteAlways]
    public class GlowModifier : MonoBehaviour
    {
        [SerializeField, HideInInspector] GlowGraphic _glowGraphic;

        [SerializeField] Vector2 _offset;
        [SerializeField] float _extraSize;

        [SerializeField] Color _glowColor = Color.white;
        [SerializeField] GradientType _glowGradientType;
        [SerializeField] Color _glowGradientColor = Color.white;
        [SerializeField, Range(2, 20)] int _glowGradientQuality = 8;
        [SerializeField] RadialMode _glowRadialMode;
        [SerializeField, Min(0.01f)] float _glowGradientSize = 1f;
        [SerializeField] Gradient _glowGradient;
        [SerializeField, Range(0f, 360f)] float _glowGradientAngle;

        [SerializeField, Min(0f)] float _spread;
        [SerializeField, Min(0f)] float _blur = 10f;
        [SerializeField, Min(0.01f)] float _power = 1f;

        [SerializeField] bool _useMaxRoundness;
        [SerializeField] bool _uniformRoundness;
        [SerializeField] Vector4 _roundnessInPixels;

        bool _dirty = true;

        public bool hasSource => GetComponent<RectangleGraphic>();

        public Vector2 offset
        {
            get => _offset;
            set { _offset = value; _dirty = true; }
        }

        public float extraSize
        {
            get => _extraSize;
            set { _extraSize = value; _dirty = true; }
        }

        public Color glowColor
        {
            get => _glowColor;
            set { _glowColor = value; _dirty = true; }
        }

        public GradientType glowGradientType
        {
            get => _glowGradientType;
            set { _glowGradientType = value; _dirty = true; }
        }

        public Color glowGradientColor
        {
            get => _glowGradientColor;
            set { _glowGradientColor = value; _dirty = true; }
        }

        public int glowGradientQuality
        {
            get => _glowGradientQuality;
            set { _glowGradientQuality = Mathf.Clamp(value, 2, 20); _dirty = true; }
        }

        public RadialMode glowRadialMode
        {
            get => _glowRadialMode;
            set { _glowRadialMode = value; _dirty = true; }
        }

        public float glowGradientSize
        {
            get => _glowGradientSize;
            set { _glowGradientSize = Mathf.Max(0.01f, value); _dirty = true; }
        }

        public Gradient glowGradient
        {
            get => _glowGradient;
            set { _glowGradient = value; _dirty = true; }
        }

        public float glowGradientAngle
        {
            get => _glowGradientAngle;
            set { _glowGradientAngle = value; _dirty = true; }
        }

        public float spread
        {
            get => _spread;
            set { _spread = Mathf.Max(0f, value); _dirty = true; }
        }

        public float blur
        {
            get => _blur;
            set { _blur = Mathf.Max(0f, value); _dirty = true; }
        }

        public float power
        {
            get => _power;
            set { _power = Mathf.Max(0.01f, value); _dirty = true; }
        }

        public bool useMaxRoundness
        {
            get => _useMaxRoundness;
            set { _useMaxRoundness = value; _dirty = true; }
        }

        public bool uniformRoundness
        {
            get => _uniformRoundness;
            set { _uniformRoundness = value; _dirty = true; }
        }

        public Vector4 roundnessInPixels
        {
            get => _roundnessInPixels;
            set { _roundnessInPixels = value; _dirty = true; }
        }

        void OnEnable()
        {
            EnsureGlowObject();
            _dirty = true;
        }

        void OnDisable()
        {
            if (_glowGraphic)
                _glowGraphic.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (!_glowGraphic) return;
            var go = _glowGraphic.gameObject;
            _glowGraphic = null;

            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            _dirty = true;

            // Delay to avoid issues during deserialization
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (!this) return;
                if (!isActiveAndEnabled)
                {
                    if (_glowGraphic)
                        _glowGraphic.gameObject.SetActive(false);
                    return;
                }
                EnsureGlowObject();
            };
        }
#endif

        void LateUpdate()
        {
            SyncTransform();

            if (_dirty)
            {
                _dirty = false;
                SyncAll();
            }
        }

        void EnsureGlowObject()
        {
            if (_glowGraphic && _glowGraphic.gameObject != gameObject)
            {
                _glowGraphic.gameObject.SetActive(true);
                return;
            }

            // Stale or missing — create new
            _glowGraphic = null;

            var go = new GameObject("~Glow")
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave
            };
            go.transform.SetParent(transform.parent, false);
            _glowGraphic = go.AddComponent<GlowGraphic>();
            _glowGraphic.raycastTarget = false;

            SyncTransform();
        }

        void SyncTransform()
        {
            if (!_glowGraphic) return;

            var glowT = _glowGraphic.transform;

            // Ensure same parent
            if (glowT.parent != transform.parent)
                glowT.SetParent(transform.parent, false);

            // Ensure glow renders before us (behind)
            int myIdx = transform.GetSiblingIndex();
            if (glowT.GetSiblingIndex() != myIdx - 1)
                glowT.SetSiblingIndex(myIdx);

            // Sync RectTransform
            var glowRT = _glowGraphic.rectTransform;
            var myRT = (RectTransform)transform;

            glowRT.anchorMin = myRT.anchorMin;
            glowRT.anchorMax = myRT.anchorMax;
            glowRT.pivot = myRT.pivot;
            glowRT.sizeDelta = myRT.sizeDelta;
            glowRT.anchoredPosition = myRT.anchoredPosition + _offset;
            glowRT.localRotation = myRT.localRotation;
            glowRT.localScale = myRT.localScale;
        }

        void SyncAll()
        {
            if (!_glowGraphic) return;

            // Auto-detect source
            var rect = GetComponent<RectangleGraphic>();
            _glowGraphic.source = rect;

            _glowGraphic.extraSize = _extraSize;
            _glowGraphic.glowColor = _glowColor;
            _glowGraphic.glowGradientType = _glowGradientType;
            _glowGraphic.glowGradientColor = _glowGradientColor;
            _glowGraphic.glowGradientQuality = _glowGradientQuality;
            _glowGraphic.glowRadialMode = _glowRadialMode;
            _glowGraphic.glowGradientSize = _glowGradientSize;
            _glowGraphic.glowGradient = _glowGradient;
            _glowGraphic.glowGradientAngle = _glowGradientAngle;
            _glowGraphic.spread = _spread;
            _glowGraphic.blur = _blur;
            _glowGraphic.power = _power;

            if (!rect)
            {
                _glowGraphic.useMaxRoundness = _useMaxRoundness;
                _glowGraphic.uniformRoundness = _uniformRoundness;
                _glowGraphic.roundnessInPixels = _roundnessInPixels;
            }
        }
    }
}
