using NUnit.Framework;
using PurrNet.UI;
using UnityEngine;

namespace PurrNet.UI.Tests
{
    public class AudioSessionTests
    {
        [Test]
        public void Constructor_DefaultsToNoOverrides()
        {
            var session = new AudioSession((AudioClip)null);
            Assert.IsNull(session.volume);
            Assert.IsNull(session.pitch);
        }

        [Test]
        public void WithVolume_SetsVolume()
        {
            var session = new AudioSession((AudioClip)null).WithVolume(0.42f);
            Assert.AreEqual(0.42f, session.volume);
        }

        [Test]
        public void WithPitch_SetsPitch()
        {
            var session = new AudioSession((AudioClip)null).WithPitch(1.5f);
            Assert.AreEqual(1.5f, session.pitch);
        }

        [Test]
        public void WithVolume_Random_StaysWithinRange()
        {
            for (int i = 0; i < 100; i++)
            {
                var session = new AudioSession((AudioClip)null).WithVolume(0.5f, 0.1f);
                Assert.GreaterOrEqual(session.volume.Value, 0.4f);
                Assert.LessOrEqual(session.volume.Value, 0.6f);
            }
        }

        [Test]
        public void WithPitch_Random_StaysWithinRange()
        {
            for (int i = 0; i < 100; i++)
            {
                var session = new AudioSession((AudioClip)null).WithPitch(1f, 0.2f);
                Assert.GreaterOrEqual(session.pitch.Value, 0.8f);
                Assert.LessOrEqual(session.pitch.Value, 1.2f);
            }
        }

        [Test]
        public void Builders_DoNotMutateOriginal()
        {
            var original = new AudioSession((AudioClip)null);
            original.WithVolume(0.1f).WithPitch(2f);
            Assert.IsNull(original.volume);
            Assert.IsNull(original.pitch);
        }
    }
}
