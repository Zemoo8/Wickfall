using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wickfall.Player;
using Wickfall.Presentation;
using Wickfall.UI;

namespace Wickfall.Meta
{
    /// <summary>
    /// The running game: the save in memory, the player's progress applied to the player's systems,
    /// death and respawn at the last brazier (PDF §2.1), brazier rests (PDF §2.4, G08), relics,
    /// flames, shards and world flags. Lives in the Core scene with the player.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        public PlayerController player;
        public GameObject hud;

        public SaveData Save { get; private set; } = SaveData.NewGame();
        public PlayerModifiers Modifiers { get; private set; }
        public SceneProfile Profile { get; private set; }
        public RegionId Region { get; private set; }
        public bool InGameplay => Profile != null && Profile.kind == SceneKind.Gameplay;
        public bool Paused { get; private set; }
        public RegionTracker Tracker { get; } = new RegionTracker();

        static readonly int[] AllFlames = { 0, 1, 2, 3, 4 };
        readonly HashSet<string> flags = new HashSet<string>();
        bool spareWickUsed;
        bool respawning;
        int bossPhase;

        void Awake()
        {
            Instance = this;
            Save.Sanitize();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnEnable()
        {
            Events.PlayerExtinguished.Register(OnExtinguished);
            Events.GamePaused.Register(OnPauseRequested);
            Events.BossPhaseChanged.Register(OnBossPhase);
        }

        void OnDisable()
        {
            Events.PlayerExtinguished.Unregister(OnExtinguished);
            Events.GamePaused.Unregister(OnPauseRequested);
            Events.BossPhaseChanged.Unregister(OnBossPhase);
        }

        void Start()
        {
            ApplyProgress();
            if (Profile == null && SceneProfile.Active != null)
                OnSceneProfile(SceneProfile.Active);
            else if (Profile == null)
                SetGameplayVisible(false);
        }

        void Update()
        {
            bool menuUsedPause = UI.UiKit.PauseConsumedFrame == Time.frameCount || UI.BrazierMenuView.IsOpen
                                 || (UI.OptionsView.Instance != null && UI.OptionsView.Instance.IsOpen);
            if (GameInput.Instance.Pause.WasPressedThisFrame() && InGameplay && !respawning && !menuUsedPause)
                SetPaused(!Paused);
            if (InGameplay && !Paused)
                Tracker.Tick(Time.deltaTime);
        }

        // ---- scenes ----------------------------------------------------------------------------

        public void OnSceneProfile(SceneProfile profile)
        {
            Profile = profile;
            bool gameplay = profile.kind == SceneKind.Gameplay;
            SetGameplayVisible(gameplay);
            SetPaused(false);
            if (!gameplay)
            {
                if (profile.music != MusicId.None)
                    Events.Music.Raise(profile.music);
                return;
            }

            if (profile.sandbox)
                ApplyProgress();

            if (profile.hasRegion)
            {
                Region = profile.region;
                Save.region = (int)profile.region;
                Events.RegionEntered.Raise(profile.region);
                Tracker.Begin(profile.region);
            }

            Vector2 start = profile.playerStart != null ? (Vector2)profile.playerStart.position : Vector2.zero;
            if (!profile.sandbox && Save.region == (int)profile.region && Save.position != Vector2.zero && !string.IsNullOrEmpty(Save.brazierId))
                start = Save.position;
            if (player != null)
            {
                player.Respawn(start);
                player.Wax.SetCurrent(Mathf.Clamp(Save.wax, 1f, player.Wax.Max));
            }
            var music = profile.music != MusicId.None ? profile.music : Db.Region(profile.region).music;
            Events.Music.Raise(music);
        }

        void SetGameplayVisible(bool visible)
        {
            if (player != null)
            {
                player.gameObject.SetActive(visible);
                if (visible)
                    player.Wax.Ticking = true;
            }
            if (hud != null)
                hud.SetActive(visible && PresentationMode.IsEnabled(Feature.Hud));
        }

        // ---- pause -----------------------------------------------------------------------------

        void OnPauseRequested(bool pause)
        {
            if (pause != Paused && InGameplay)
                SetPaused(pause);
        }

        public void SetPaused(bool pause)
        {
            if (Paused == pause)
                return;
            Paused = pause;
            Time.timeScale = pause ? 0f : 1f;
            AudioListener.pause = pause;
            if (Events.GamePaused.Last != pause)
                Events.GamePaused.Raise(pause);
        }

        // ---- progress --------------------------------------------------------------------------

        /// <summary>Pushes relics, pacts, flames and upgrades from the save into the player's systems.</summary>
        public void ApplyProgress()
        {
            Modifiers = PlayerModifiers.Compute(Save, Db.Data);
            if (player == null)
                return;
            var m = Modifiers;
            player.Wax.ApplyModifiers(m);
            player.Light.ApplyModifiers(m);
            player.Combat.ApplyModifiers(m, Save);
            player.Body.ApplyModifiers(m);
            player.Dodger.SetMaxCharges(m.DodgeCharges);
            if (Profile != null && Profile.sandbox)
            {
                // The Light Lab equips all five flames so each can be tried. It never touches the save.
                player.Flames.Configure((1 << WickfallCounts.Flames) - 1, AllFlames, WickfallCounts.Flames, player.Flames.Current);
            }
            else
            {
                player.Flames.Configure(Save.flamesUnlockedMask, Save.loadout, m.FlameSlots, (FlameId)Save.activeFlame);
            }
            Events.ShardsChanged.Raise(Save.shards);
            Events.ProgressChanged.Raise();
        }

        public void NewGame()
        {
            Save = SaveData.NewGame();
            Save.Sanitize();
            flags.Clear();
            spareWickUsed = false;
            ApplyProgress();
        }

        public bool LoadGame()
        {
            var services = GameServices.Instance;
            if (services == null)
                return false;
            var result = services.Store.TryLoad(out var data);
            if (data == null)
                return false;
            Save = data;
            flags.Clear();
            foreach (string f in Save.worldFlags)
                flags.Add(f);
            ApplyProgress();
            Debug.Log($"[WICKFALL Save] Loaded ({result}).");
            return true;
        }

        public void SaveGame()
        {
            if (Profile != null && Profile.sandbox)
                return;
            if (!PresentationMode.IsEnabled(Feature.SaveSystem))
                return;
            if (player != null)
            {
                Save.wax = player.Wax.Current;
                Save.activeFlame = (int)player.Flames.Current;
                for (int i = 0; i < Save.loadout.Length; i++)
                    Save.loadout[i] = player.Flames.Loadout[i];
                Save.flamesUnlockedMask = player.Flames.UnlockedMask;
            }
            Save.worldFlags.Clear();
            Save.worldFlags.AddRange(flags);
            GameServices.Instance?.Store.Write(Save);
        }

        public bool HasFlag(string flag) => flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag))
                flags.Add(flag);
        }

        public void AddShards(int amount)
        {
            int gained = Mathf.RoundToInt(amount * Modifiers.ShardMultiplier);
            Save.shards = Mathf.Max(0, Save.shards + gained);
            Events.ShardsChanged.Raise(Save.shards);
            if (gained > 0)
                Events.Play(SoundId.ShardPickup, player != null ? (Vector2)player.transform.position : Vector2.zero, 0.6f);
        }

        public bool SpendShards(int amount)
        {
            if (Save.shards < amount)
                return false;
            Save.shards -= amount;
            Events.ShardsChanged.Raise(Save.shards);
            return true;
        }

        /// <summary>G19 consumable: a wax candle gives wax back (C / right stick click).</summary>
        public bool UseCandle()
        {
            if (Save.candles <= 0 || player == null || player.Wax.Current >= player.Wax.Max - 0.5f)
            {
                Events.PlayUi(SoundId.UiError);
                return false;
            }
            Save.candles--;
            var item = FindShopItem(ShopItemKind.WaxCandle);
            player.Wax.Gain(item != null ? item.value : 25f);
            Events.Play(SoundId.WaxPoolDrink, player.transform.position);
            Combat.Vfx.Play(Combat.VfxId.RelightBurst, (Vector2)player.transform.position + new Vector2(0f, 1.2f));
            Events.ProgressChanged.Raise();
            return true;
        }

        /// <summary>G19 consumable: a soot salve clears soot (X / left stick click).</summary>
        public bool UseSalve()
        {
            if (Save.salves <= 0 || player == null || !player.Status.Sooted)
            {
                Events.PlayUi(SoundId.UiError);
                return false;
            }
            Save.salves--;
            player.Status.ClearSoot();
            Events.Play(SoundId.Interact, player.transform.position);
            Events.ProgressChanged.Raise();
            return true;
        }

        static ShopItemSO FindShopItem(ShopItemKind kind)
        {
            foreach (var item in Db.Data.shop)
                if (item != null && item.kind == kind)
                    return item;
            return null;
        }

        public void CollectRelic(int index)
        {
            if (Save.HasRelic(index))
                return;
            Save.relicsMask |= 1 << index;
            Tracker.RelicTaken();
            ApplyProgress();
            Events.RelicCollected.Raise(index);
            Events.PlayUi(SoundId.RelicGet);
        }

        public void UnlockFlame(FlameId flame)
        {
            Save.flamesUnlockedMask |= 1 << (int)flame;
            if (player != null)
                player.Flames.Unlock(flame);
            Events.ProgressChanged.Raise();
        }

        public void TakePact(int index)
        {
            Save.pactsMask |= 1 << index;
            ApplyProgress();
        }

        // ---- braziers (PDF §2.4: checkpoint, refills all wax, one use per rest) ----------------

        public bool CanRefillAt(string brazierId) => Save.spentBrazierId != brazierId;

        /// <summary>Rest: saves, refills wax once (G08) and makes this the respawn point.</summary>
        public void Rest(string brazierId, Vector2 respawnPoint)
        {
            bool refill = CanRefillAt(brazierId);
            Save.brazierId = brazierId;
            Save.position = respawnPoint;
            Save.region = (int)Region;
            spareWickUsed = false;
            if (player != null && refill)
                player.Wax.Refill();
            Save.spentBrazierId = brazierId;
            if (player != null)
            {
                player.Dodger.RefillAll();
                player.Status.ClearAll();
            }
            SaveGame();
            Events.BrazierRested.Raise(brazierId.GetHashCode());
        }

        // ---- death (PDF §2.1: "la flamme s'éteint (cire à 0), retour au dernier brasero") -------

        void OnExtinguished()
        {
            if (respawning || player == null)
                return;
            StartCoroutine(Modifiers.SpareWickWax > 0f && !spareWickUsed ? Relight() : Respawn());
        }

        IEnumerator Relight()
        {
            respawning = true;
            spareWickUsed = true;
            yield return new WaitForSeconds(0.9f);
            player.Wax.Revive(Modifiers.SpareWickWax);
            player.Respawn(player.transform.position);
            Events.Play(SoundId.Relight, player.transform.position);
            Combat.Vfx.Play(Combat.VfxId.RelightBurst, (Vector2)player.transform.position + new Vector2(0f, 2f));
            Events.PlayerRespawned.Raise();
            respawning = false;
        }

        IEnumerator Respawn()
        {
            respawning = true;
            Tracker.Died();
            yield return new WaitForSeconds(Db.Balance.extinguishSeconds);
            var fader = ScreenFader.Instance;
            if (fader != null)
                yield return fader.FadeOut(0.6f);

            Vector2 at = !string.IsNullOrEmpty(Save.brazierId) ? Save.position
                : Profile != null && Profile.playerStart != null ? (Vector2)Profile.playerStart.position : Vector2.zero;
            // G08: dying resets the spent brazier; the flame is relit full at the brazier.
            Save.spentBrazierId = "";
            if (bossPhase > 0)
                Events.BossPhaseChanged.Raise(0);
            player.Wax.Revive(player.Wax.Max);
            player.Respawn(at);
            Events.PlayerRespawned.Raise();
            Events.Play(SoundId.Relight, at);
            yield return new WaitForSeconds(0.2f);
            if (fader != null)
                yield return fader.FadeIn(0.6f);
            respawning = false;
        }

        void OnBossPhase(int phase) => bossPhase = phase;
    }
}
