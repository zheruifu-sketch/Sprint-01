using System;
using System.Collections.Generic;
using Nenn.InspectorEnhancements.Runtime.Attributes;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerBuffController : MonoBehaviour
{
    [Serializable]
    public struct BuffSnapshot
    {
        public PlayerBuffType BuffType;
        public float RemainingDuration;
        public float TotalDuration;

        public float NormalizedRemaining => TotalDuration > 0f
            ? Mathf.Clamp01(RemainingDuration / TotalDuration)
            : 0f;
    }

    private sealed class ActiveBuffState
    {
        public PlayerBuffType BuffType;
        public float RemainingDuration;
        public float TotalDuration;
    }

    private readonly List<ActiveBuffState> activeBuffs = new List<ActiveBuffState>(5);

    public int ActiveBuffCount => activeBuffs.Count;

    public event Action BuffsChanged;

    private void Update()
    {
        if (activeBuffs.Count == 0)
        {
            return;
        }

        bool changed = false;
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuffState state = activeBuffs[i];
            state.RemainingDuration -= Time.deltaTime;
            if (state.RemainingDuration > 0f)
            {
                continue;
            }

            activeBuffs.RemoveAt(i);
            changed = true;
        }

        if (changed)
        {
            BuffsChanged?.Invoke();
        }
    }

    public void ApplyTimedBuff(PlayerBuffType buffType, float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        ActiveBuffState state = FindState(buffType);
        if (state == null)
        {
            state = new ActiveBuffState
            {
                BuffType = buffType
            };
            activeBuffs.Add(state);
        }

        state.TotalDuration = duration;
        state.RemainingDuration = duration;
        BuffsChanged?.Invoke();
    }

    public bool HasBuff(PlayerBuffType buffType)
    {
        ActiveBuffState state = FindState(buffType);
        return state != null && state.RemainingDuration > 0f;
    }

    public BuffSnapshot GetBuffSnapshot(int index)
    {
        if (index < 0 || index >= activeBuffs.Count)
        {
            return default;
        }

        ActiveBuffState state = activeBuffs[index];
        return new BuffSnapshot
        {
            BuffType = state.BuffType,
            RemainingDuration = Mathf.Max(0f, state.RemainingDuration),
            TotalDuration = Mathf.Max(0f, state.TotalDuration)
        };
    }

    private ActiveBuffState FindState(PlayerBuffType buffType)
    {
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].BuffType == buffType)
            {
                return activeBuffs[i];
            }
        }

        return null;
    }
}

public enum PlayerBuffType
{
    Shield = 0,
    SpeedBoost = 1
}
