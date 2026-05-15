using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[CreateAssetMenu(fileName = "BackgroundMusicConfig", menuName = "JumpGame/Audio/Background Music Config")]
public class BackgroundMusicConfig : ScriptableObject
{
    [Header("Config")]
    [LabelText("BGM")]
    [SerializeField] private AudioClip clip;
    [LabelText("音量")]
    [SerializeField] private float volume = 1f;

    public AudioClip Clip => clip;
    public float Volume => Mathf.Clamp01(volume);

    public static BackgroundMusicConfig Load()
    {
        return Resources.Load<BackgroundMusicConfig>("Audio/BackgroundMusicConfig");
    }
}
