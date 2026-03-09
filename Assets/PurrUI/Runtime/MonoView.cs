using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

namespace PurrNet.UI
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public class MonoView : MonoBehaviour
    {
        [SerializeField] private bool _cullWindowsBehind = false;

        public CanvasGroup canvasGroup { get; private set; }

        public ViewStack parentStack { get; private set; }

        public bool cullWindowsBehind => _cullWindowsBehind;

        public bool isTopMost => parentStack && parentStack.top == this;

        public Canvas canvas { get; private set; }

        public void Initialize(ViewStack parentStack)
        {
            this.parentStack = parentStack;
            canvas = GetComponent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void UpdateOrder(int order)
        {
            canvas.sortingOrder = order;
        }

        public void MoveToBackground()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void MoveToForeground()
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            OnBecomeForeground();
        }

        protected virtual void OnBecomeForeground() { }

        public IEnumerator EnterTransition() => OnEnterTransition();

        public IEnumerator ExitTransition() => OnExitTransition();

        protected virtual IEnumerator OnEnterTransition() => null;
        protected virtual IEnumerator OnExitTransition() => null;

        public IEnumerator CulledTransition() => OnCulledTransition();

        public IEnumerator UnculledTransition() => OnUnculledTransition();

        protected virtual IEnumerator OnCulledTransition() => null;
        protected virtual IEnumerator OnUnculledTransition() => null;

        internal void DestroyMe()
        {
            Destroy(gameObject);
        }

        [UsedImplicitly]
        public void CloseMe()
        {
            parentStack.Pop(this);
        }
    }
}
