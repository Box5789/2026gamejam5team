using UnityEngine;
using UnityEngine.SceneManagement;

namespace KimbapGame.Audio
{
    public static class KimbapBgmBootstrap
    {
        private const string PlayerObjectName = "KimbapBgmPlayer";
        private const string ListenerObjectName = "KimbapAudioListener";
        private const string ClipResourcePath = "Kitchen/Sound/kimbap_kitchen_bouncy_loop";
        private const float DefaultVolume = 0.45f;

        private static AudioSource bgmSource;
        private static bool sceneLoadedSubscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            bgmSource = null;
            sceneLoadedSubscribed = false;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            EnsureSceneLoadedSubscription();
            EnsureAudioListener();
            EnsureBgmPlayer();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureAudioListener();
            EnsureBgmPlayer();
        }

        private static void EnsureSceneLoadedSubscription()
        {
            if (sceneLoadedSubscribed)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneLoadedSubscribed = true;
        }

        private static void EnsureBgmPlayer()
        {
            if (bgmSource == null)
            {
                bgmSource = FindExistingSource();
            }

            if (bgmSource == null)
            {
                AudioClip clip = Resources.Load<AudioClip>(ClipResourcePath);
                if (clip == null)
                {
                    Debug.LogWarning($"BGM clip was not found at Resources/{ClipResourcePath}.");
                    return;
                }

                GameObject playerObject = new GameObject(PlayerObjectName);
                Object.DontDestroyOnLoad(playerObject);
                bgmSource = playerObject.AddComponent<AudioSource>();
                bgmSource.clip = clip;
            }

            ConfigureBgmSource(bgmSource);
            if (!bgmSource.isPlaying)
            {
                bgmSource.Play();
            }
        }

        private static void ConfigureBgmSource(AudioSource source)
        {
            source.loop = true;
            source.playOnAwake = true;
            source.volume = DefaultVolume;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = 128;
        }

        private static AudioSource FindExistingSource()
        {
            GameObject existingObject = GameObject.Find(PlayerObjectName);
            if (existingObject == null)
            {
                return null;
            }

            return existingObject.GetComponent<AudioSource>();
        }

        private static void EnsureAudioListener()
        {
            if (Object.FindFirstObjectByType<AudioListener>() != null)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindFirstObjectByType<Camera>();
            }

            if (camera != null)
            {
                camera.gameObject.AddComponent<AudioListener>();
                return;
            }

            GameObject listenerObject = new GameObject(ListenerObjectName);
            Object.DontDestroyOnLoad(listenerObject);
            listenerObject.AddComponent<AudioListener>();
        }
    }
}
