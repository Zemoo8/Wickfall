using UnityEngine;

namespace Wickfall
{
    // Payloads carried by the event channels. All structs, so raising an event never allocates.

    public struct WaxChange
    {
        public float Current;
        public float Max;
        public float Delta;
        public WaxChangeReason Reason;

        public float Normalized => Max > 0f ? Current / Max : 0f;

        public WaxChange(float current, float max, float delta, WaxChangeReason reason)
        {
            Current = current;
            Max = max;
            Delta = delta;
            Reason = reason;
        }
    }

    public struct LightState
    {
        public float Radius;            // what the player sees (Light2D outer radius)
        public float UntrimmedRadius;   // the radius before the trim multiplier
        public float DetectionRadius;   // PDF §2.3: 2 + radius x 1.5 (x 0.4 while trimmed)
        public float Intensity;
        public Color Color;
        public bool Trimmed;
        public Vector2 Position;
    }

    public struct FlameSwitch
    {
        public FlameId From;
        public FlameId To;
        public Vector2 Position;
    }

    public struct FlareInfo
    {
        public Vector2 Position;
        public float Radius;
        public FlareKind Kind;
        public Color Color;

        public FlareInfo(Vector2 position, float radius, FlareKind kind, Color color)
        {
            Position = position;
            Radius = radius;
            Kind = kind;
            Color = color;
        }
    }

    public struct EnemyKilledInfo
    {
        public EnemyKind Kind;
        public Vector2 Position;
        public int WaxReward;
        public int Shards;
        public int EntityId;
    }

    public struct EnemyAlertInfo
    {
        public EnemyKind Kind;
        public Vector2 Position;
        public int EntityId;
        public bool Alerted; // false when the enemy gives up
    }

    public struct HitReport
    {
        public Vector2 Position;
        public float Amount;
        public bool Blocked; // e.g. a Scorifié hit by the wrong flame
        public FlameId Flame;
        public Team Victim;
        public bool Killed;
    }

    public struct PlayerActionInfo
    {
        public PlayerActionKind Kind;
        public Vector2 Direction;
        public FlameId Flame;
        public Vector2 Position;
        public float Time;
    }

    public struct DodgeState
    {
        public int Charges;
        public int MaxCharges;
        public float RechargeProgress; // 0..1 for the next charge
    }

    public struct StatusInfo
    {
        public HitEffects Effect;
        public bool Active;
        public float Duration;
    }

    public struct RegionResult
    {
        public RegionId Region;
        public int Score;
        public Rank Rank;
        public float WaxPercent;
        public float TimeSeconds;
        public int Relics;
        public int EnemiesAvoided;
        public int Deaths;
    }

    public struct InteractPromptInfo
    {
        public bool Visible;
        public string TextKey; // localization key, never a raw string
        public Vector2 WorldPosition;
    }

    public struct SoundRequest
    {
        public SoundId Id;
        public Vector2 Position;
        public bool Positional;
        public float Volume;
        public float Pitch;

        public SoundRequest(SoundId id, Vector2 position, float volume = 1f, float pitch = 1f, bool positional = true)
        {
            Id = id;
            Position = position;
            Volume = volume;
            Pitch = pitch;
            Positional = positional;
        }
    }

    public enum FeedbackKind { Shake, HitStop, Flash }

    public struct FeedbackRequest
    {
        public FeedbackKind Kind;
        public float Strength;
        public float Duration;
        public Color Color;

        public FeedbackRequest(FeedbackKind kind, float strength, float duration, Color color = default)
        {
            Kind = kind;
            Strength = strength;
            Duration = duration;
            Color = color;
        }
    }
}
