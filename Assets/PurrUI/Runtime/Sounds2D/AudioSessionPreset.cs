using UnityEngine;

namespace PurrNet.UI
{
    public class AudioSessionPreset : ScriptableObject
    {
        public AudioClip[] clips;

        [MinMax(0f, 1f)] public Vector2 volume = new Vector2(.9f, 1f);
        [MinMax(0f, 2f)] public Vector2 pitch = new Vector2(.8f, 1.2f);

        public void Play()
        {
            var session = new AudioSession(clips)
                .WithVolume(Random.Range(volume.x, volume.y))
                .WithPitch(Random.Range(pitch.x, pitch.y));

            Sounds2D.Play(session);
        }
    }
}
