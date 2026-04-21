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
            float middleVolume = (volume.x + volume.y) / 2f;
            float middlePitch = (pitch.x + pitch.y) / 2f;

            float volumeRange = volume.y - volume.x;
            float pitchRange = pitch.y - pitch.x;

            var session = new AudioSession(clips)
                .WithVolume(middleVolume, volumeRange)
                .WithPitch(middlePitch, pitchRange);

            Sounds2D.Play(session);
        }
    }
}
