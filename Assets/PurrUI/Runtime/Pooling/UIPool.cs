using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PurrNet.UI
{
    public class UIPool<T> where T : Component
    {
        readonly T _gameobject;

        readonly RectTransform _parent;

        readonly List<T> _instances;

        private int _index = 0;

        public UIPool(T prefab, RectTransform parent)
        {
            _instances = new List<T>();
            _gameobject = prefab;
            _parent = parent;
        }

        public void ResetCounter()
        {
            _index = 0;
        }

        public T GetInstance()
        {
            if (_instances.Count <= _index)
            {
                var gameo = Object.Instantiate(_gameobject, _parent, false);
                _instances.Add(gameo);
            }

            var go = _instances[_index++];
            if (!go.gameObject.activeSelf) go.gameObject.SetActive(true);
            return go;
        }

        public void DiscardRest()
        {
            for (int i = _index; i < _instances.Count; i++)
                _instances[i].gameObject.SetActive(false);
            ResetCounter();
        }

        internal void Dispose()
        {
            ResetCounter();

            for (int i = 0; i < _instances.Count; i++)
                Object.Destroy(_instances[i]);

            _instances.Clear();
            _instances.TrimExcess();
        }
    }
}
