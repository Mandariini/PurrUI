using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PurrNet.UI
{
    public class ViewStack : MonoBehaviour
    {
        [SerializeField] private Transform _parent;
        [SerializeField, HideInInspector, Obsolete] private ViewCollection _prefabs;
        [SerializeField] private List<ViewCollection> _prefabCollections;
        [SerializeField] private MonoView _pushOnStart;
        [SerializeField] private int _orderOffset;

        private readonly List<MonoView> _stack = new();
        private readonly Dictionary<MonoView, Coroutine> _cullCoroutines = new();
        private readonly Dictionary<MonoView, Coroutine> _uncullCoroutines = new();

        public MonoView top => _stack.Count > 0 ? _stack[^1] : null;

        private void Start()
        {
            if (_pushOnStart)
                Push(_pushOnStart);
        }

#if UNITY_EDITOR
#pragma warning disable CS0612 // Type or member is obsolete
        private void OnValidate()
        {
            // this is entirely for backwards compatibility
            if (_prefabs)
            {
                _prefabCollections ??= new List<ViewCollection>();
                if (!_prefabCollections.Contains(_prefabs))
                    _prefabCollections.Add(_prefabs);
                _prefabs = null;
            }
        }
#pragma warning restore CS0612 // Type or member is obsolete
#endif

        private void Reset()
        {
            _parent = transform;
        }

        private void UpdateOrder(int fromIdx)
        {
            for (var i = fromIdx; i < _stack.Count; i++)
                _stack[i].UpdateOrder(i + _orderOffset);
        }

        private bool TryGet<T>(out T prefab) where T : MonoView
        {
            if (_prefabCollections == null)
            {
                prefab = null;
                return false;
            }

            for (int i = 0; i < _prefabCollections.Count; i++)
            {
                var prefabs = _prefabCollections[i];

                if (!prefabs)
                    continue;

                for (var j = 0; j < prefabs.views.Length; j++)
                {
                    var window = prefabs.views[j];
                    if (window is T typedWindow)
                    {
                        prefab = typedWindow;
                        return true;
                    }
                }
            }

            prefab = null;
            return false;
        }

        /// <summary>
        /// Searches the stack from top to bottom for the first instance of the specified type and returns it.
        /// If no instance of that type is found, returns null.
        /// </summary>
        public T FindWindow<T>() where T : MonoView
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i] is T instance)
                    return instance;
            }
            return null;
        }

        /// <summary>
        /// Adds a new window to the top of the stack using the provided prefab.
        /// The prefab must be a child of the WindowStack GameObject or its descendants.
        /// </summary>
        public MonoView Push(MonoView prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[WindowStack] Provided prefab is null.", this);
                return null;
            }

            if (_stack.Count > 0)
            {
                var currentTop = _stack[^1];
                currentTop.MoveToBackground();
            }

            var idx = _stack.Count;
            var instance = Instantiate(prefab, _parent);
            _stack.Add(instance);
            instance.Initialize(this);
            instance.UpdateOrder(idx + _orderOffset);
            instance.OnPushed();

            var transition = instance.EnterTransition();
            if (transition != null)
            {
                instance.canvasGroup.interactable = false;
                instance.canvasGroup.blocksRaycasts = false;
                StartCoroutine(RunEnterTransition(instance, transition));
                ApplyCulling(false);
            }
            else
            {
                ApplyCulling(true);
            }

            return instance;
        }

        private void UpdateVisibility()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                var view = _stack[i];

                bool wasCulled = !view.canvas.enabled;

                if (_cullCoroutines.TryGetValue(view, out var cullCoroutine))
                {
                    StopCoroutine(cullCoroutine);
                    _cullCoroutines.Remove(view);
                    wasCulled = true;
                }

                if (wasCulled)
                {
                    view.canvas.enabled = true;

                    var enterTransition = view.UnculledTransition();
                    if (enterTransition != null)
                    {
                        var coroutine = StartCoroutine(RunUncullTransition(view, enterTransition));
                        _uncullCoroutines[view] = coroutine;
                    }
                }

                if (view.cullWindowsBehind)
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        StopUncullTransition(_stack[j]);
                        _stack[j].canvas.enabled = false;
                    }
                    break;
                }
            }
        }

        private void StopUncullTransition(MonoView view)
        {
            if (_uncullCoroutines.TryGetValue(view, out var coroutine))
            {
                StopCoroutine(coroutine);
                _uncullCoroutines.Remove(view);
            }
        }

        private void ApplyCulling(bool instant)
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i].cullWindowsBehind)
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        var view = _stack[j];

                        if (!view.canvas.enabled || _cullCoroutines.ContainsKey(view))
                            continue;

                        StopUncullTransition(view);

                        var transition = view.CulledTransition();
                        if (transition != null)
                        {
                            _cullCoroutines[view] = StartCoroutine(RunCullTransition(view, transition));
                            continue;
                        }

                        if (instant)
                            view.canvas.enabled = false;
                    }
                    break;
                }
            }
        }

        private IEnumerator RunCullTransition(MonoView view, IEnumerator transition)
        {
            yield return transition;
            _cullCoroutines.Remove(view);
            if (view) view.canvas.enabled = false;
        }

        private IEnumerator RunUncullTransition(MonoView view, IEnumerator transition)
        {
            yield return transition;
            _uncullCoroutines.Remove(view);
        }

        /// <summary>
        /// Adds a new window of the specified type to the top of the stack.
        /// The prefab must be included in the WindowPrefabs collection.
        /// If no prefab of that type is found, logs an error and does nothing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Push<T>() where T : MonoView
        {
            if (!TryGet<T>(out var prefab))
            {
                Debug.LogError($"[WindowStack] No window prefab of type `{typeof(T)}` found in WindowPrefabs.", this);
                return null;
            }

            var idx = _stack.Count;

            if (idx > 0)
            {
                var currentTop = _stack[idx - 1];
                currentTop.MoveToBackground();
            }

            var instance = Instantiate(prefab, _parent);
            _stack.Add(instance);
            instance.Initialize(this);
            instance.UpdateOrder(idx + _orderOffset);
            instance.OnPushed();

            var transition = instance.EnterTransition();
            if (transition != null)
            {
                instance.canvasGroup.interactable = false;
                instance.canvasGroup.blocksRaycasts = false;
                StartCoroutine(RunEnterTransition(instance, transition));
                ApplyCulling(false);
            }
            else
            {
                ApplyCulling(true);
            }

            return instance;
        }

        /// <summary>
        /// Removes the top window from the stack. If the stack is empty, does nothing.
        /// </summary>
        public void Pop()
        {
            if (_stack.Count == 0)
            {
                Debug.LogError("[WindowStack] No windows to pop.", this);
                return;
            }

            var topView = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            topView.OnPopped();

            var transition = topView.ExitTransition();
            if (transition != null)
            {
                topView.canvasGroup.interactable = false;
                topView.canvasGroup.blocksRaycasts = false;
                StartCoroutine(RunExitTransition(topView, transition));
            }
            else
            {
                topView.DestroyMe();
            }

            if (_stack.Count > 0)
            {
                var newTopIdx = _stack.Count - 1;
                var newTop = _stack[newTopIdx];
                newTop.transform.SetAsLastSibling();
                newTop.MoveToForeground();
                newTop.UpdateOrder(newTopIdx + _orderOffset);
            }
            UpdateVisibility();
        }

        /// <summary>
        /// Removes all windows from the stack. Windows are removed in order, so exit transitions will play correctly.
        /// </summary>
        public void Clear()
        {
            while (_stack.Count > 0)
                Pop();
        }

        /// <summary>
        /// Removes the specified instance from the stack, regardless of its position. If the instance is not in the stack, does nothing.
        /// </summary>
        /// <param name="instance"></param>
        public void Pop(MonoView instance)
        {
            int idx = _stack.IndexOf(instance);

            if (idx == -1)
            {
                Debug.LogError("[WindowStack] The provided window instance is not in the stack.", this);
                return;
            }

            // If it's the top window, use the regular Pop method
            if (idx == _stack.Count - 1)
            {
                Pop();
                return;
            }

            _stack.RemoveAt(idx);
            instance.OnPopped();

            var transition = instance.ExitTransition();
            if (transition != null)
            {
                instance.canvasGroup.blocksRaycasts = false;
                StartCoroutine(RunExitTransition(instance, transition));
            }
            else
            {
                instance.DestroyMe();
            }

            UpdateOrder(idx);
            UpdateVisibility();
        }

        /// <summary>
        /// Moves the specified instance to the top of the stack. If the instance is not in the stack, does nothing.
        /// </summary>
        /// <param name="instance"></param>
        public void MoveToTop(MonoView instance)
        {
            int idx = _stack.IndexOf(instance);
            if (idx == -1)
            {
                Debug.LogError("[WindowStack] The provided window instance is not in the stack.", this);
                return;
            }

            if (idx == _stack.Count - 1)
                return;

            _stack.RemoveAt(idx);
            _stack.Add(instance);
            instance.transform.SetAsLastSibling();
            UpdateOrder(idx);
            UpdateVisibility();
        }

        /// <summary>
        /// Moves the first instance of the specified type to the top of the stack. If no instance of that type is found, does nothing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void MoveToTop<T>() where T : MonoView
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i] is T instance)
                {
                    MoveToTop(instance);
                    return;
                }
            }
        }

        private IEnumerator RunEnterTransition(MonoView view, IEnumerator transition)
        {
            yield return transition;
            if (view)
            {
                view.canvasGroup.interactable = true;
                view.canvasGroup.blocksRaycasts = true;
            }
            ApplyCulling(true);
        }

        private static IEnumerator RunExitTransition(MonoView view, IEnumerator transition)
        {
            yield return transition;
            if (view) view.DestroyMe();
        }
    }
}
