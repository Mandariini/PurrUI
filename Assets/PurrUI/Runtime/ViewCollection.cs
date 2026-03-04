using UnityEngine;

namespace PurrNet.UI
{
    [CreateAssetMenu(menuName = "PurrNet/View Collection", order = 1)]
    public class ViewCollection : ScriptableObject
    {
        public MonoView[] views;
    }
}
