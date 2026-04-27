using UnityEngine;
using UnityEngine.EventSystems;

namespace PurrNet.UI.HeroUI
{
    public class ClickSounds : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private AudioSessionPreset _clickSound;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_clickSound)
                _clickSound.Play();
        }
    }
}
