using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PurrNet.UI
{
    public class CloseParentView : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Sound to play when closing the parent view.")]
        [SerializeField] private AudioSessionPreset _closeSound;
        [Tooltip("Whether this view can be closed by clicking it. If false, clicks will pass through to the parent view instead.")]
        [SerializeField] private bool _canClose = true;
        [Tooltip("Whether clicks that close the parent view should also pass through to any views behind it.")]
        [SerializeField] private bool _passThrough = false;

        /// <summary>
        /// Whether this view can be closed by clicking it.
        /// If false, clicks will pass through to the parent view instead.
        /// </summary>
        public bool canClose
        {
            get => _canClose;
            set => _canClose = value;
        }

        /// <summary>
        /// Closes the parent view of this component, if it has one. If canClose is false, this method does nothing.
        /// If passThrough is true, clicks that close the parent view will also pass through to any views behind it.
        /// </summary>
        public void Close()
        {
            if (!_canClose) return;

            var view = GetComponentInParent<MonoView>();
            if (view && view.parentStack)
            {
                _closeSound.Play();
                view.CloseMe();
            }
        }

        private readonly List<RaycastResult> _results = new ();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_canClose)
                return;

            Close();

            if (_passThrough)
            {
                EventSystem.current.RaycastAll(eventData, _results);

                for (var i = 0; i < _results.Count;)
                {
                    ExecuteEvents.ExecuteHierarchy(_results[i].gameObject, eventData,
                        ExecuteEvents.pointerClickHandler);
                    break;
                }
            }
        }
    }
}
