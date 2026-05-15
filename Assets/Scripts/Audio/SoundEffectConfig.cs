using System;
using System.Collections.Generic;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundEffectConfig", menuName = "JumpGame/Audio/Sound Effect Config")]
public class SoundEffectConfig : ScriptableObject
{
    [Serializable]
    public class SoundEffectEntry
    {
        [LabelText("音效类型")]
        [SerializeField] private SoundEffectId soundEffectId = SoundEffectId.None;
        [LabelText("音效")]
        [SerializeField] private AudioClip clip;
        [LabelText("音量")]
        [SerializeField] private float volume = 1f;

        public SoundEffectId SoundEffectId => soundEffectId;
        public AudioClip Clip => clip;
        public float Volume => Mathf.Clamp01(volume);
    }

    [Header("Config")]
    [LabelText("音效条目")]
    [SerializeField] private List<SoundEffectEntry> entries = new List<SoundEffectEntry>();

    public static SoundEffectConfig Load()
    {
        return Resources.Load<SoundEffectConfig>("Audio/SoundEffectConfig");
    }

    public bool TryGetClip(SoundEffectId soundEffectId, out AudioClip clip, out float volume)
    {
        clip = null;
        volume = 1f;

        for (int i = 0; i < entries.Count; i++)
        {
            SoundEffectEntry entry = entries[i];
            if (entry == null || entry.SoundEffectId != soundEffectId)
            {
                continue;
            }

            if (entry.Clip == null)
            {
                return false;
            }

            clip = entry.Clip;
            volume = entry.Volume;
            return true;
        }

        return false;
    }
}
