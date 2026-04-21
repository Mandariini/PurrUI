using UnityEngine;

namespace PurrNet.UI
{
    public class MinMaxAttribute : PropertyAttribute
    {
        public readonly float minLimit;
        public readonly float maxLimit;

        public MinMaxAttribute(float min, float max)
        {
            minLimit = min;
            maxLimit = max;
        }
    }
}
