using UnityEngine;

public static class BackgroundMusicPlayback
{
    private const string RuntimeObjectName = "BackgroundMusicRuntime";

    private static AudioSource runtimeSource;

    public static void PlayLoop()
    {
        BackgroundMusicConfig config = BackgroundMusicConfig.Load();
        if (config == null || config.Clip == null)
        {
            Stop();
            return;
        }

        EnsureRuntimeSource();
        if (runtimeSource == null)
        {
            return;
        }

        runtimeSource.clip = config.Clip;
        runtimeSource.volume = config.Volume;
        runtimeSource.loop = true;

        if (!runtimeSource.isPlaying)
        {
            runtimeSource.Play();
        }
    }

    public static void Stop()
    {
        if (runtimeSource == null)
        {
            return;
        }

        runtimeSource.Stop();
        runtimeSource.clip = null;
    }

    private static void EnsureRuntimeSource()
    {
        if (runtimeSource != null)
        {
            return;
        }

        GameObject runtimeObject = GameObject.Find(RuntimeObjectName);
        if (runtimeObject == null)
        {
            runtimeObject = new GameObject(RuntimeObjectName);
            Object.DontDestroyOnLoad(runtimeObject);
        }

        runtimeSource = runtimeObject.GetComponent<AudioSource>();
        if (runtimeSource == null)
        {
            runtimeSource = runtimeObject.AddComponent<AudioSource>();
        }

        runtimeSource.playOnAwake = false;
        runtimeSource.spatialBlend = 0f;
        runtimeSource.loop = true;
    }
}
