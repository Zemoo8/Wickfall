namespace Wickfall
{
    /// <summary>Every sound the game asks for. The AudioLibrary asset maps each id to clips.</summary>
    public enum SoundId
    {
        None = 0,
        // Player
        Footstep, AttackSwing, ArtCast, Dodge, PlayerHurt, Extinguish, Relight, FlameSwitch, Flare, TrimOn, TrimOff,
        WaxPickup, ShardPickup, Interact,
        // Combat
        HitFlesh, HitWax, HitMetal, HitBlocked, HitGlass, EnemyDeath, BoltFire, PoisonBurst, RadiantPulse, CrimsonStrike,
        // Enemies
        EnemyAlert, DriplingSquelch, WaxflyBuzz, HuskDrop, LurkerSkitter, BloomSnap, SlagbornStep, SlagbornSmash,
        BellowsWindup, BellowsPuff, ReflectionGlitch, UnlitWhisper,
        // Hazards and world (every telegraph has a cue, prompt §11)
        GeyserWarn, GeyserErupt, PressWarn, PressSlam, SteamHiss, ConveyorLoop, LaserCharge, LaserFire, GlassCrack, GlassBreak,
        BrazierLight, BrazierRest, WaxPoolDrink, GateOpen, BellRing, RelicGet, BeamHum, PanelLit, RoseComplete,
        // Boss
        KingRise, KingSlam, KingVoid, KingPhase, KingFall,
        // UI
        UiMove, UiConfirm, UiBack, UiOpen, UiError, Purchase, ScoreTally, RankStamp,
    }

    public enum MusicId
    {
        None = 0,
        Title, TallowFields, Wickwood, SootFoundries, GlassCathedral, DeepHollow, HearthChamber, Boss, Ending, Brazier,
    }
}
