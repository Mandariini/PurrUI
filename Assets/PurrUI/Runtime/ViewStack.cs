using System.Collections.Generic;
using UnityEngine;

namespace PurrNet.UI
{
    public class ViewStack : MonoBehaviour
    {
        [SerializeField] private Transform _parent;
        [SerializeField] private ViewCollection _prefabs;
        [SerializeField] private MonoView _pushOnStart;
        [SerializeField] private int _orderOffset;

        private readonly List<MonoView> _stack = new();

        private void Start()
        {
            if (_pushOnStart)
                Push(_pushOnStart);
        }

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
            for (var i = 0; i < _prefabs.views.Length; i++)
            {
                var window = _prefabs.views[i];
                if (window is T typedWindow)
                {
                    prefab = typedWindow;
                    return true;
                }
            }

            prefab = null;
            return false;
        }

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
            instance.UpdateOrder(idx);
            UpdateVisibility();

            return instance;
        }

        private void UpdateVisibility()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                _stack[i].canvas.enabled = true;
                if (_stack[i].hidesWindowsBelow)
                {
                    for (int j = i - 1; j >= 0; j--)
                        _stack[j].canvas.enabled = false;
                    break;
                }
            }
        }

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
            UpdateVisibility();

            return instance;
        }

        public void Pop()
        {
            if (_stack.Count == 0)
            {
                Debug.LogError("[WindowStack] No windows to pop.", this);
                return;
            }

            var top = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            top.DestroyMe();

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

        public void Clear()
        {
            while (_stack.Count > 0)
                Pop();
        }

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
            instance.DestroyMe();
            UpdateOrder(idx);
            UpdateVisibility();
        }

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
    }
}
