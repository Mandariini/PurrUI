using JetBrains.Annotations;
using UnityEngine;

namespace PurrNet.UI
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public class MonoView : MonoBehaviour
    {
        [SerializeField] private bool _hidesWindowsBelow = false;

        public CanvasGroup canvasGroup { get; private set; }

        public ViewStack parentStack { get; private set; }

        public bool hidesWindowsBelow => _hidesWindowsBelow;

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
            canvasGroup.blocksRaycasts = false;
        }

        public void MoveToForeground()
        {
            canvasGroup.blocksRaycasts = true;
            OnBecomeForeground();
        }

        protected virtual void OnBecomeForeground() { }

        public void DestroyMe()
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
