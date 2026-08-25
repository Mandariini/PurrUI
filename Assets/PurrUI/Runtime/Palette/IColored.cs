using UnityEngine;

namespace PurrNet.UI
{
    public interface IColored
    {
        public string[] keys { get; }

        public Color GetColor(int keyIndex);

        public void SetColor(int keyIndex, Color color);
    }
}
