using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Wickfall
{
    /// <summary>
    /// The list of every event channel in the game (Resources/GameEvents). Each field is its own
    /// channel asset under Assets/Data/Events, created by WICKFALL > Setup.
    /// </summary>
    [CreateAssetMenu(menuName = "WICKFALL/Game Events Hub", fileName = "GameEvents")]
    public sealed class GameEvents : ScriptableObject
    {
        public const string ResourcePath = "GameEvents";

        [Header("Wax and light (WaxSystem, LightController, DimController)")]
        public WaxEventChannel waxChanged;
        public LightEventChannel lightChanged;
        public BoolEventChannel trimChanged;
        public StatusEventChannel statusChanged;
        public VoidEventChannel playerExtinguished;
        public VoidEventChannel playerRespawned;

        [Header("Flames and combat")]
        public FlameEventChannel flameSwitched;
        [Tooltip("Flame wheel: -2 closed, -1 open with nothing selected, 0..4 the highlighted slot.")]
        public IntEventChannel flameWheel;
        public FlareEventChannel flare;
        public PlayerActionEventChannel playerActed;
        public DodgeEventChannel dodgeChanged;
        public HitEventChannel playerHit;
        public HitEventChannel enemyHit;
        public EnemyKilledEventChannel enemyKilled;
        public EnemyAlertEventChannel enemyAlerted;

        [Header("Progress and world")]
        public IntEventChannel shardsChanged;
        public IntEventChannel relicCollected;
        public VoidEventChannel progressChanged;
        public IntEventChannel brazierRested;
        public RegionEventChannel regionEntered;
        public RegionResultEventChannel regionCompleted;
        public PromptEventChannel interactPrompt;

        [Header("Boss and endings")]
        public IntEventChannel bossPhaseChanged;
        public VoidEventChannel bossDefeated;
        public EndingEventChannel endingReached;

        [Header("Presentation (audio, feedback, menus)")]
        public SoundEventChannel sound;
        public MusicEventChannel music;
        public FeedbackEventChannel feedback;
        public BoolEventChannel gamePaused;
        public VoidEventChannel languageChanged;
        public VoidEventChannel settingsChanged;
        [Tooltip("A content scene finished loading (menu, region, showcase).")]
        public VoidEventChannel sceneReady;

        /// <summary>Every channel field, in declaration order (editor setup, dev overlay, tests).</summary>
        public static IEnumerable<FieldInfo> ChannelFields()
        {
            foreach (var field in typeof(GameEvents).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                if (typeof(EventChannelBase).IsAssignableFrom(field.FieldType))
                    yield return field;
            }
        }

        /// <summary>An in-memory hub with a fresh instance of every channel (tests, or a missing asset).</summary>
        public static GameEvents CreateRuntime()
        {
            var hub = CreateInstance<GameEvents>();
            hub.name = "GameEvents (runtime)";
            hub.FillMissing();
            return hub;
        }

        /// <summary>Creates any channel that is still null, in memory.</summary>
        public void FillMissing()
        {
            foreach (var field in ChannelFields())
            {
                if (field.GetValue(this) as EventChannelBase != null)
                    continue;
                var channel = (EventChannelBase)CreateInstance(field.FieldType);
                channel.name = field.Name;
                field.SetValue(this, channel);
            }
        }

        public void ResetAll()
        {
            foreach (var field in ChannelFields())
            {
                var channel = field.GetValue(this) as EventChannelBase;
                if (channel != null)
                    channel.ResetState();
            }
        }
    }

    /// <summary>
    /// Static access to the channels: <c>Events.WaxChanged.Raise(...)</c>. Systems only ever talk
    /// through these; none of them holds a reference to another system.
    /// </summary>
    public static class Events
    {
        static GameEvents hub;

        public static GameEvents Hub
        {
            get
            {
                if (hub == null)
                {
                    hub = Resources.Load<GameEvents>(GameEvents.ResourcePath);
                    if (hub == null)
                        hub = GameEvents.CreateRuntime();
                    else
                        hub.FillMissing();
                }
                return hub;
            }
        }

        /// <summary>Use another hub (tests use a fresh runtime hub so they never share state).</summary>
        public static void Use(GameEvents value) => hub = value;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => hub = null;

        public static WaxEventChannel WaxChanged => Hub.waxChanged;
        public static LightEventChannel LightChanged => Hub.lightChanged;
        public static BoolEventChannel TrimChanged => Hub.trimChanged;
        public static StatusEventChannel StatusChanged => Hub.statusChanged;
        public static VoidEventChannel PlayerExtinguished => Hub.playerExtinguished;
        public static VoidEventChannel PlayerRespawned => Hub.playerRespawned;
        public static FlameEventChannel FlameSwitched => Hub.flameSwitched;
        public static IntEventChannel FlameWheel => Hub.flameWheel;
        public static FlareEventChannel Flare => Hub.flare;
        public static PlayerActionEventChannel PlayerActed => Hub.playerActed;
        public static DodgeEventChannel DodgeChanged => Hub.dodgeChanged;
        public static HitEventChannel PlayerHit => Hub.playerHit;
        public static HitEventChannel EnemyHit => Hub.enemyHit;
        public static EnemyKilledEventChannel EnemyKilled => Hub.enemyKilled;
        public static EnemyAlertEventChannel EnemyAlerted => Hub.enemyAlerted;
        public static IntEventChannel ShardsChanged => Hub.shardsChanged;
        public static IntEventChannel RelicCollected => Hub.relicCollected;
        public static VoidEventChannel ProgressChanged => Hub.progressChanged;
        public static IntEventChannel BrazierRested => Hub.brazierRested;
        public static RegionEventChannel RegionEntered => Hub.regionEntered;
        public static RegionResultEventChannel RegionCompleted => Hub.regionCompleted;
        public static PromptEventChannel InteractPrompt => Hub.interactPrompt;
        public static IntEventChannel BossPhaseChanged => Hub.bossPhaseChanged;
        public static VoidEventChannel BossDefeated => Hub.bossDefeated;
        public static EndingEventChannel EndingReached => Hub.endingReached;
        public static SoundEventChannel Sound => Hub.sound;
        public static MusicEventChannel Music => Hub.music;
        public static FeedbackEventChannel Feedback => Hub.feedback;
        public static BoolEventChannel GamePaused => Hub.gamePaused;
        public static VoidEventChannel LanguageChanged => Hub.languageChanged;
        public static VoidEventChannel SettingsChanged => Hub.settingsChanged;
        public static VoidEventChannel SceneReady => Hub.sceneReady;

        // Shorthands for the most common sends.
        public static void Play(SoundId id, Vector2 position, float volume = 1f, float pitch = 1f) =>
            Sound.Raise(new SoundRequest(id, position, volume, pitch));

        public static void PlayUi(SoundId id) => Sound.Raise(new SoundRequest(id, Vector2.zero, 1f, 1f, false));

        public static void Shake(float strength, float duration) =>
            Feedback.Raise(new FeedbackRequest(FeedbackKind.Shake, strength, duration));

        public static void HitStop(float duration) =>
            Feedback.Raise(new FeedbackRequest(FeedbackKind.HitStop, 1f, duration));
    }
}
