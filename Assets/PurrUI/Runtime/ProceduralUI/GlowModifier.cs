using UnityEngine;

namespace PurrNet.UI
{
    [ExecuteAlways]
#if UNITY_6000_0_OR_NEWER
    [AddComponentMenu("UI (Canvas)/PurrUI/Glow Modifier")]
#else
    [AddComponentMenu("UI/PurrUI/Glow Modifier")]
#endif
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

        [SerializeField] bool _noFill;
        [SerializeField, Min(0f)] float _frameWidth = 5f;
        [SerializeField] FramePlacement _framePlacement;

        [SerializeField] bool _useMaxRoundness;
        [SerializeField] bool _uniformRoundness;
        [SerializeField] Vector4 _roundnessInPixels;

        bool _dirty = true;
#if UNITY_EDITOR
        bool _editorSyncQueued;
#endif

        public bool hasSource => GetComponent<RectangleGraphic>();

        public Vector2 offset
        {
            get => _offset;
            set { _offset = value; SetDirty(); }
        }

        public float extraSize
        {
            get => _extraSize;
            set { _extraSize = value; SetDirty(); }
        }

        public Color glowColor
        {
            get => _glowColor;
            set { _glowColor = value; SetDirty(); }
        }

        public GradientType glowGradientType
        {
            get => _glowGradientType;
            set { _glowGradientType = value; SetDirty(); }
        }

        public Color glowGradientColor
        {
            get => _glowGradientColor;
            set { _glowGradientColor = value; SetDirty(); }
        }

        public int glowGradientQuality
        {
            get => _glowGradientQuality;
            set { _glowGradientQuality = Mathf.Clamp(value, 2, 20); SetDirty(); }
        }

        public RadialMode glowRadialMode
        {
            get => _glowRadialMode;
            set { _glowRadialMode = value; SetDirty(); }
        }

        public float glowGradientSize
        {
            get => _glowGradientSize;
            set { _glowGradientSize = Mathf.Max(0.01f, value); SetDirty(); }
        }

        public Gradient glowGradient
        {
            get => _glowGradient;
            set { _glowGradient = value; SetDirty(); }
        }

        public float glowGradientAngle
        {
            get => _glowGradientAngle;
            set { _glowGradientAngle = value; SetDirty(); }
        }

        public float spread
        {
            get => _spread;
            set { _spread = Mathf.Max(0f, value); SetDirty(); }
        }

        public float blur
        {
            get => _blur;
            set { _blur = Mathf.Max(0f, value); SetDirty(); }
        }

        public float power
        {
            get => _power;
            set { _power = Mathf.Max(0.01f, value); SetDirty(); }
        }

        public bool noFill
        {
            get => _noFill;
            set { _noFill = value; SetDirty(); }
        }

        public float frameWidth
        {
            get => _frameWidth;
            set { _frameWidth = Mathf.Max(0f, value); SetDirty(); }
        }

        public FramePlacement framePlacement
        {
            get => _framePlacement;
            set { _framePlacement = value; SetDirty(); }
        }

        public bool useMaxRoundness
        {
            get => _useMaxRoundness;
            set { _useMaxRoundness = value; SetDirty(); }
        }

        public bool uniformRoundness
        {
            get => _uniformRoundness;
            set { _uniformRoundness = value; SetDirty(); }
        }

        public Vector4 roundnessInPixels
        {
            get => _roundnessInPixels;
            set { _roundnessInPixels = value; SetDirty(); }
        }

        void OnEnable()
        {
            Canvas.willRenderCanvases += OnWillRenderCanvases;
            EnsureGlowObject();
            SetDirty();
            SyncNow();
        }

        void OnDisable()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;

            if (_glowGraphic && OwnsGlowGraphic(_glowGraphic))
                _glowGraphic.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (!_glowGraphic) return;

            if (!OwnsGlowGraphic(_glowGraphic))
            {
                _glowGraphic = null;
                return;
            }

            var go = _glowGraphic.gameObject;
            _glowGraphic = null;

            if (!go) return;

            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
#if UNITY_EDITOR
                // Defer to avoid "Destroying object multiple times" during teardown
                UnityEditor.EditorApplication.delayCall += () => { if (go) DestroyImmediate(go); };
#endif
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            SetDirty();
        }
#endif

        void LateUpdate()
        {
            SyncNow();
        }

        void OnWillRenderCanvases()
        {
            SyncNow();
        }

        void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        void OnTransformParentChanged()
        {
            SetDirty();
            SyncNow();
        }

        void OnTransformChildrenChanged()
        {
            SetDirty();
        }

        void OnDidApplyAnimationProperties()
        {
            SetDirty();
        }

        public void Refresh()
        {
            SetDirty();
            SyncNow();
        }

        void SetDirty()
        {
            _dirty = true;
#if UNITY_EDITOR
            QueueEditorSync();
#endif
        }

#if UNITY_EDITOR
        void QueueEditorSync()
        {
            if (Application.isPlaying || _editorSyncQueued)
                return;

            _editorSyncQueued = true;
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _editorSyncQueued = false;
                if (!this) return;

                if (!isActiveAndEnabled)
                {
                    if (_glowGraphic && OwnsGlowGraphic(_glowGraphic))
                        _glowGraphic.gameObject.SetActive(false);
                    return;
                }

                SyncNow();
                UnityEditor.SceneView.RepaintAll();
            };
        }
#endif

        void SyncNow()
        {
            if (!isActiveAndEnabled)
                return;

            EnsureGlowObject();
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
                // Verify this glow actually belongs to us (not stolen via duplication)
                if (OwnsGlowGraphic(_glowGraphic))
                {
                    _glowGraphic.owner = this;
                    _glowGraphic.gameObject.SetActive(true);
                    return;
                }

                // Shared with another modifier (e.g. after Ctrl+D) — drop reference
                _glowGraphic = null;
            }

            if (_glowGraphic)
                return;

            var go = new GameObject("~Glow", typeof(RectTransform))
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave
            };
            go.transform.SetParent(transform.parent, false);
            _glowGraphic = go.AddComponent<GlowGraphic>();
            _glowGraphic.raycastTarget = false;
            _glowGraphic.owner = this;

            SyncTransform();
        }

        bool OwnsGlowGraphic(GlowGraphic glow)
        {
            if (!glow)
                return false;

            if (glow.owner == this)
                return true;

            if (glow.owner)
                return false;

            var rect = GetComponent<RectangleGraphic>();
            if (rect && glow.source == rect)
                return true;

            if (rect || glow.source || glow.transform.parent != transform.parent)
                return false;

            return glow.transform.GetSiblingIndex() == Mathf.Max(0, transform.GetSiblingIndex() - 1);
        }

        void SyncTransform()
        {
            if (!_glowGraphic) return;

            bool changed = false;
            var glowT = _glowGraphic.transform;

            // Ensure same parent
            if (glowT.parent != transform.parent)
            {
                glowT.SetParent(transform.parent, false);
                changed = true;
            }

            // Ensure glow renders before us (behind)
            int myIdx = transform.GetSiblingIndex();
            int glowIdx = glowT.GetSiblingIndex();
            int targetIdx = glowIdx < myIdx ? myIdx - 1 : myIdx;
            if (glowIdx != targetIdx)
            {
                glowT.SetSiblingIndex(targetIdx);
                changed = true;
            }

            // Sync RectTransform
            var glowRT = _glowGraphic.rectTransform;
            var myRT = transform as RectTransform;
            if (!glowRT || !myRT)
                return;

            var anchoredPosition = myRT.anchoredPosition + _offset;

            if (glowRT.anchorMin != myRT.anchorMin) { glowRT.anchorMin = myRT.anchorMin; changed = true; }
            if (glowRT.anchorMax != myRT.anchorMax) { glowRT.anchorMax = myRT.anchorMax; changed = true; }
            if (glowRT.pivot != myRT.pivot) { glowRT.pivot = myRT.pivot; changed = true; }
            if (glowRT.sizeDelta != myRT.sizeDelta) { glowRT.sizeDelta = myRT.sizeDelta; changed = true; }
            if (glowRT.anchoredPosition != anchoredPosition) { glowRT.anchoredPosition = anchoredPosition; changed = true; }
            if (glowRT.localRotation != myRT.localRotation) { glowRT.localRotation = myRT.localRotation; changed = true; }
            if (glowRT.localScale != myRT.localScale) { glowRT.localScale = myRT.localScale; changed = true; }

            if (changed)
                _glowGraphic.SetAllDirty();
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
                _glowGraphic.noFill = _noFill;
                _glowGraphic.frameWidth = _frameWidth;
                _glowGraphic.framePlacement = _framePlacement;
                _glowGraphic.useMaxRoundness = _useMaxRoundness;
                _glowGraphic.uniformRoundness = _uniformRoundness;
                _glowGraphic.roundnessInPixels = _roundnessInPixels;
            }
        }
    }
}
