using System;

namespace Wickfall
{
    /// <summary>The five flames of PDF §2.1, in wheel order.</summary>
    public enum FlameId
    {
        Amber = 0,   // Ambre: balanced, melee arc
        Blue = 1,    // Bleue: ranged, slows
        Green = 2,   // Verte: poison, hits groups
        White = 3,   // Blanche: anti-Creux, huge light
        Crimson = 4, // Cramoisie: more damage at low wax
    }

    /// <summary>The six regions of PDF §2.2, in the PDF's order.</summary>
    public enum RegionId
    {
        TallowFields = 0,   // Champs de Suif
        Wickwood = 1,       // Bois-Mèche
        SootFoundries = 2,  // Fonderies de Suie
        GlassCathedral = 3, // Cathédrale de Verre
        DeepHollow = 4,     // Creux Profond
        HearthChamber = 5,  // Chambre du Foyer
    }

    /// <summary>Every creature of PDF §2.2 (the Light-Eater Bloom is an obstacle with its own state machine).</summary>
    public enum EnemyKind
    {
        Dripling = 0,
        Waxfly = 1,
        Husk = 2,
        Lurker = 3,
        LightEaterBloom = 4,
        Slagborn = 5,
        Bellows = 6,
        Reflection = 7,
        Unlit = 8,
        HollowKing = 9,
    }

    /// <summary>PDF §1 endings. C is the secret one.</summary>
    public enum EndingId
    {
        None = -1,
        A = 0, // rallumer le Foyer et mourir
        B = 1, // garder la flamme et rester seul
        C = 2, // passer la mèche à un autre (6 reliques + Roi Creux épargné)
    }

    public enum Team { Player, Enemy, Neutral }

    /// <summary>Four facing directions; the sprites are drawn in these four (PDF §3).</summary>
    public enum Facing { Down = 0, Up = 1, Left = 2, Right = 3 }

    /// <summary>The actions a Reflection can copy (PDF §2.2: "copient vos 3 dernières actions").</summary>
    public enum PlayerActionKind { None = 0, Attack = 1, Art = 2, Dodge = 3, SwitchFlame = 4 }

    public enum FlareKind { FlameSwitch, KillNoise, BellRing, Art }

    public enum WaxChangeReason { Passive, Damage, Gain, Refill, MaxChanged, Cost, Death, Revive, Debug, Load }

    /// <summary>Region ranks of PDF §2.4, lowest first.</summary>
    public enum Rank { Cendre = 0, Braise = 1, Flamme = 2, Brasier = 3 }

    /// <summary>Damage interactions of PDF §2.3 ("Flamme contre matière").</summary>
    [Flags]
    public enum DamageTags
    {
        None = 0,
        Armored = 1 << 0, // Scorifiés: hurt only by the white or green flame
        Glass = 1 << 1,   // glass: broken only by the crimson flame
        Hollow = 1 << 2,  // Creux creatures: the white flame is "anti-Creux"
        Mirror = 1 << 3,  // Reflets
    }

    /// <summary>Status effects a hit can carry.</summary>
    [Flags]
    public enum HitEffects
    {
        None = 0,
        Slow = 1 << 0,     // blue flame
        Poison = 1 << 1,   // green flame
        Soot = 1 << 2,     // Bellows: damage x1.25 and light loss for 1.5 s
        LightCut = 1 << 3, // Light-Eater Bloom: light cut for 3 s
        Stagger = 1 << 4,
    }

    public static class WickfallCounts
    {
        public const int Flames = 5;
        public const int Regions = 6;
        public const int Relics = 6;
    }
}
