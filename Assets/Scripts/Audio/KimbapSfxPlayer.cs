using UnityEngine;

namespace KimbapGame.Audio
{
    public static class KimbapSfxPlayer
    {
        public const string PlayerObjectName = "KimbapSfxPlayer";

        private static AudioSource sfxSource;

        public static string LastRequestedResourcePath { get; private set; } = string.Empty;

        public static int PlayRequestCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            sfxSource = null;
            ResetDiagnosticsForTests();
        }

        public static void Play(Component owner, string resourcePath, float volume)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return;
            }

            string normalizedPath = resourcePath.Trim();
            LastRequestedResourcePath = normalizedPath;
            PlayRequestCount++;

            AudioSource source = EnsurePersistentAudioSource();
            AudioClip clip = Resources.Load<AudioClip>(normalizedPath);
            if (clip == null)
            {
                LogWarning($"SFX clip not found in Resources: {normalizedPath}", owner);
                return;
            }

            source.PlayOneShot(clip, volume);
        }

        public static void ResetDiagnosticsForTests()
        {
            LastRequestedResourcePath = string.Empty;
            PlayRequestCount = 0;
        }

        private static AudioSource EnsurePersistentAudioSource()
        {
            if (sfxSource != null)
            {
                ConfigureAudioSource(sfxSource);
                return sfxSource;
            }

            GameObject playerObject = GameObject.Find(PlayerObjectName);
            if (playerObject == null)
            {
                playerObject = new GameObject(PlayerObjectName);
            }

            if (playerObject.transform.parent != null)
            {
                playerObject.transform.SetParent(null);
            }

            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(playerObject);
            }

            sfxSource = playerObject.GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = playerObject.AddComponent<AudioSource>();
            }

            ConfigureAudioSource(sfxSource);
            return sfxSource;
        }

        private static void ConfigureAudioSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
        }

        private static void LogWarning(string message, Object context)
        {
            if (context == null)
            {
                Debug.LogWarning(message);
                return;
            }

            Debug.LogWarning(message, context);
        }
    }
}
