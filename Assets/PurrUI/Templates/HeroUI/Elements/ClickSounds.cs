using UnityEngine;
using UnityEngine.EventSystems;

namespace PurrNet.UI.HeroUI
{
    public class ClickSounds : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private AudioSessionPreset _clickSound;

        public void OnPointerDown(PointerEventData eventData) => _clickSound.Play();
    }
}
