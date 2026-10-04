using System;

namespace FakeMG.SceneLoading
{
    public sealed class SceneDataApplicationConfiguration
    {
        public float TimeoutSeconds { get; }

        public SceneDataApplicationConfiguration(float timeoutSeconds)
        {
            TimeoutSeconds = timeoutSeconds > 0f
                ? timeoutSeconds
                : throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
        }
    }
}
