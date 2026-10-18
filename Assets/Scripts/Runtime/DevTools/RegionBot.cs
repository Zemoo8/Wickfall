using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Wickfall.Enemies;
using Wickfall.Meta;
using Wickfall.Player;
using Wickfall.World;

namespace Wickfall.DevTools
{
    /// <summary>
    /// Plays a region from its level file's @bot steps (prompt §15: "one bot per region that clears
    /// it"), through the same input path as a person. Steps (text coordinates, y down):
    ///   goto x y | walk x y (no fighting) | fight x y radius | clear x y radius (a fight even in showcase) |
    ///   rest x y | drink x y | read x y |
    ///   take x y | ring x y | exit x y | trim on|off | flame n
    /// After a death the bot resumes from the step after its last rest.
    /// Showcase mode (gameplay videos) plays like a careful person instead: no optional fights (it
    /// sneaks past with the wick trimmed and fights only what attacks it), it rolls away from blows and
    /// shots, and <see cref="Caption"/> says in plain English what it is doing.
    /// </summary>
    public sealed class RegionBot : MonoBehaviour
    {
        public float stepTimeout = 90f;
        public float engageRadius = 3.2f;
        public bool showcase;
        [Tooltip("Showcase: a hunting creature this close is fought; farther ones are outrun.")]
        public float threatRadius = 2.4f;
        [Tooltip("Showcase: the wick is trimmed while an unaware creature is this close.")]
        public float sneakRadius = 7f;

        PlayerController player;
        readonly ScriptedInput input = new ScriptedInput();
        LevelData level;
        string[] steps;
        int step;
        int resumeStep;
        float stepTime;
        float actedAt;
        bool interacted;
        Interactable interactedWith;
        readonly Vector2[] path = new Vector2[256];
        int pathCount, pathIndex;
        Vector2 pathTarget;
        float repath;
        float nextFlameSwitch;
        float nextArt;
        readonly HashSet<int> doneOnce = new HashSet<int>(); // take/ring steps already done (skipped after a death)

        public int Step => step;
        public int StepCount => steps != null ? steps.Length : 0;
        public bool Done { get; private set; }
        public bool Failed { get; private set; }
        public string FailReason { get; private set; }
        public string CurrentStep => steps != null && step < steps.Length ? steps[step] : "-";
        public int Deaths { get; private set; }
        public int Kills { get; private set; }
        /// <summary>What the current step is doing (for logs).</summary>
        public string Note { get; private set; } = "";

        public void Begin(PlayerController target, LevelData data, IList<string> route = null)
        {
            player = target;
            level = data;
            steps = route != null ? new List<string>(route).ToArray() : data.BotScript.ToArray();
            step = 0;
            resumeStep = 0;
            stepTime = 0f;
            Done = Failed = false;
            player.SetInputSource(input);
            Events.PlayerRespawned.Register(OnRespawned);
            Events.EnemyKilled.Register(OnKill);
            Events.WaxChanged.Register(OnWax);
            Events.PlayerExtinguished.Register(OnExtinguished);
            Events.BossPhaseChanged.Register(OnBossPhase);
        }

        void OnDestroy()
        {
            Events.PlayerRespawned.Unregister(OnRespawned);
            Events.EnemyKilled.Unregister(OnKill);
            Events.WaxChanged.Unregister(OnWax);
            Events.PlayerExtinguished.Unregister(OnExtinguished);
            Events.BossPhaseChanged.Unregister(OnBossPhase);
            if (player != null)
                player.SetInputSource(null);
        }

        // ---- showcase narration (gameplay videos) ----
        // One caption per situation, explaining what is on screen and why. Each topic is told once per
        // run and stays up long enough to read aloud. Times are unscaled: in a recording that is video time.
        const string En = UI.Loc.English;
        struct Pending
        {
            public string Key, Text;
            public int Priority;
            public float At;
        }
        readonly List<Pending> pending = new List<Pending>();
        readonly HashSet<string> told = new HashSet<string>();
        string caption = "";
        float captionAt = -100f, captionHold, quietUntil;
        int captionPriority;
        bool routeTrim;
        int toldStep = -1;
        string objective = "", objectiveShown = "";

        /// <summary>Showcase: the narration on screen, in plain English.</summary>
        public string Caption => caption;

        /// <summary>Narration clock: video time while recording (a frame is 1/captureFramerate s), else real time.</summary>
        static float Now => Time.captureFramerate > 0 ? Time.frameCount / (float)Time.captureFramerate : Time.unscaledTime;

        /// <summary>No new caption for this many seconds (the video's own intro is on screen).</summary>
        public void HoldNarration(float seconds) => quietUntil = Now + seconds;

        /// <summary>Queues a topic for the narration; each topic is told once.</summary>
        public void Tell(string key, string text, int priority = 1)
        {
            if (!showcase || told.Contains(key))
                return;
            for (int i = 0; i < pending.Count; i++)
                if (pending[i].Key == key)
                    return;
            pending.Add(new Pending { Key = key, Text = text, Priority = priority, At = Now });
        }

        static float HoldFor(string text) => Mathf.Clamp(2f + text.Length / 18f, 5f, 9f);

        void Show(string text, int priority = 0)
        {
            caption = text;
            captionPriority = priority;
            captionAt = Now;
            captionHold = HoldFor(text);
        }

        void Narrate()
        {
            float now = Now;
            if (now < quietUntil)
                return;
            if (now - captionAt < captionHold)
            {
                // what the bot is doing right now (priority 3+) may cut a lesser caption once it has been read
                bool urgent = false;
                for (int i = 0; i < pending.Count; i++)
                    urgent |= pending[i].Priority >= 3 && pending[i].Priority > captionPriority;
                if (!urgent || now - captionAt < 4f)
                    return;
            }
            // forget what is no longer happening (an important topic waits longer)
            for (int i = pending.Count - 1; i >= 0; i--)
                if (now - pending[i].At > (pending[i].Priority >= 3 ? 30f : 15f))
                    pending.RemoveAt(i);
            int best = -1;
            for (int i = 0; i < pending.Count; i++)
                if (best < 0 || pending[i].Priority > pending[best].Priority)
                    best = i;
            if (best >= 0)
            {
                told.Add(pending[best].Key);
                Show(pending[best].Text, pending[best].Priority);
                pending.RemoveAt(best);
                return;
            }
            // nothing new to explain: say where the bot is heading
            if (objective.Length > 0 && caption != objective && (objective != objectiveShown || now - captionAt >= captionHold + 8f))
            {
                objectiveShown = objective;
                Show(objective);
            }
        }

        void OnKill(EnemyKilledInfo info)
        {
            Kills++;
            if (info.Kind != EnemyKind.HollowKing)
                Tell("kill", "Killing a creature gives wax back and shards: fighting is risky, but it can also refill the candle", 1);
        }

        void OnWax(WaxChange change)
        {
            if (change.Reason == WaxChangeReason.Damage && change.Delta <= -1f)
                Tell("hit", "Every hit burns wax. Wax is also the light, so a hurt candle sees less and less", 2);
            else if (change.Reason == WaxChangeReason.Gain && change.Delta >= 1f)
                Tell("gain", "Wax drops refill the candle: wax is the only resource, life and light at once", 1);
            else if (change.Reason == WaxChangeReason.Refill)
                Tell("brazier", BrazierText, 2);
        }

        const string BrazierText = "Braziers are checkpoints: resting refills the wax once and saves. If the flame goes out, the bot comes back here";

        void OnExtinguished() =>
            Tell("death", "Out of wax: the flame goes out. The bot comes back at the last brazier, and each death costs score", 4);

        void OnBossPhase(int phase)
        {
            if (phase == 1)
                Tell("phase1", "Phase 2: each phase takes something away. The arena's wax pools go dark and ash waves begin", 3);
            else if (phase >= 2)
                Tell("phase2", "Phase 3: no more trimming the wick, and the King pulses negative light", 3);
        }

        void OnRespawned()
        {
            Deaths++;
            step = resumeStep;
            stepTime = 0f;
            pathCount = 0;
            input.TrimHeld = false;
            routeTrim = false;
        }

        static string EnemyName(EnemyKind kind) => UI.Loc.Get("enemy." + kind.ToString().ToLowerInvariant(), En) ?? kind.ToString();
        static string FlameName(FlameId flame) => UI.Loc.Get("flame." + flame.ToString().ToLowerInvariant(), En) ?? flame.ToString();
        static string FlameRole(FlameId flame) => UI.Loc.Get("flame." + flame.ToString().ToLowerInvariant() + ".role", En) ?? "";

        static string LowerFirst(string text) => text.Length > 0 ? char.ToLowerInvariant(text[0]) + text.Substring(1) : text;
        static string Capital(string text) => text.Length > 0 ? char.ToUpperInvariant(text[0]) + text.Substring(1) : text;

        /// <summary>"a Dripling", "the Unlit".</summary>
        static string AName(EnemyKind kind)
        {
            string name = EnemyName(kind);
            return name.StartsWith("The ") ? "the " + name.Substring(4) : "a " + name;
        }

        /// <summary>The next objective of the route, said between explanations.</summary>
        string Objective()
        {
            for (int j = step; j < steps.Length; j++)
            {
                var p = steps[j].Split(' ');
                switch (p[0])
                {
                    case "rest": return "Next: the brazier ahead, the next checkpoint";
                    case "read": return "Next: an inscription stone";
                    case "drink": return "Next: a wax pool, to refill the candle";
                    case "take":
                    {
                        Vector2 at = World(p[1], p[2]);
                        var shrine = Near<FlameShrine>(at, 2f);
                        if (shrine != null)
                            return $"Next: the shrine of the {FlameName(shrine.flame)} flame";
                        return Near<RelicPickup>(at, 2f) != null ? "Next: this region's relic" : "Next: something to collect";
                    }
                    case "ring": return "Next: the Great Bell, which opens the exit gate";
                    case "climb": return "Next: the wick-vine up the wall";
                    case "aim": return "Next: the crystals that steer the light beam";
                    case "smash": return "Next: a glass barrier blocking the way";
                    case "rose": return "Next: the rose window puzzle";
                    case "boss": return "Next: the Hollow King's arena";
                    case "exit": return "The gate is open: heading for the exit";
                    case "judge":
                    case "ending": return "";
                }
            }
            return "";
        }

        /// <summary>What a step is about, told once when the bot starts it.</summary>
        void TellStep(string command, string[] parts, Vector2 target)
        {
            switch (command)
            {
                case "rest": Tell("brazier", BrazierText, 2); break;
                case "clear": Tell("clear", "Creatures guard this spot, so here the bot has to fight: it clears them first, then goes on", 3); break;
                case "read": Tell("read", "Inscription stones tell the story of Wickfall, one piece at a time", 1); break;
                case "drink": Tell("pool", "Wax pools refill the candle: wax is life, so drinking heals", 2); break;
                case "take":
                {
                    var shrine = Near<FlameShrine>(target, 2f);
                    var relic = shrine == null ? Near<RelicPickup>(target, 2f) : null;
                    if (shrine != null)
                        Tell("flame" + shrine.flame, $"A flame shrine gives a new flame: {FlameName(shrine.flame)} ({LowerFirst(FlameRole(shrine.flame))}). " +
                                                     "Each flame suits other enemies; switching costs a little wax", 3);
                    else if (relic != null)
                        Tell("relic" + relic.relicIndex, $"A relic is a permanent power. This one, {UI.Loc.Get("relic." + relic.relicIndex, En)}: " +
                                                         UI.Loc.Get("relic." + relic.relicIndex + ".desc", En), 3);
                    break;
                }
                case "ring": Tell("bell", "The Great Bell opens the exit gate, but its toll wakes every creature around: ring it, then run", 3); break;
                case "climb": Tell("vine", "With the Ember Hook relic the bot can climb wick-vines to reach higher ground", 2); break;
                case "aim": Tell("aim", "Crystal pedestals turn step by step: the bot points them so the light beam reaches the next mirror", 2); break;
                case "smash": Tell("glass", "Glass barriers only break under the Crimson flame, so the bot switches to it and strikes", 2); break;
                case "rose": Tell("rose", "The rose window has five panels; each lights only with the flame of its own colour, so the bot switches flame for each", 3); break;
                case "boss": Tell("boss", "The Hollow King has three phases, each taking something away. The bot uses the White flame (anti-Hollow) and dodges his big attacks", 3); break;
                case "judge":
                    Tell("judge", "The King is beaten and the game asks: spare him or extinguish him? The choice changes the ending. The bot " +
                                  (parts.Length > 3 && parts[3] == "spare" ? "spares him" : "extinguishes him"), 4);
                    break;
                case "ending": Tell("ending", "At the Hearth, the last choice picks one of three endings. The bot picks ending " + (parts.Length > 3 ? parts[3] : ""), 4); break;
                case "exit": Tell("exit", "At the exit, the score counts wax left, time, relics and enemies avoided, minus deaths, and gives a rank", 2); break;
            }
        }

        Vector2 World(string sx, string sy)
        {
            int x = int.Parse(sx, CultureInfo.InvariantCulture);
            int y = int.Parse(sy, CultureInfo.InvariantCulture);
            return new Vector2(x + 0.5f, level.Height - 1 - y + 0.5f);
        }

        void Fail(string reason)
        {
            Failed = true;
            var cell = LevelData.CellOf(player.transform.position);
            FailReason = $"step {step} '{CurrentStep}': {reason} (at text {cell.x} {level.Height - 1 - cell.y}, state {player.StateName})";
            input.Move = Vector2.zero;
            Debug.LogWarning("[WICKFALL Bot] " + FailReason);
        }

        void Next()
        {
            if (steps != null && step < steps.Length && (steps[step].StartsWith("take") || steps[step].StartsWith("ring") || (showcase && steps[step].StartsWith("read"))))
                doneOnce.Add(step);
            step++;
            stepTime = 0f;
            interacted = false;
            pathCount = 0;
            if (step >= steps.Length)
            {
                Done = true;
                input.Move = Vector2.zero;
            }
        }

        void Update()
        {
            if (showcase)
                Narrate();
            if (player == null || steps == null || Done || Failed)
                return;
            input.Move = Vector2.zero;
            if (player.IsExtinguished || player.ControlsLocked || Time.timeScale <= 0f)
                return;

            stepTime += Time.deltaTime;
            if (stepTime > stepTimeout)
            {
                Fail("timed out");
                return;
            }

            if (doneOnce.Contains(step))
            {
                Next(); // a relic, a flame or the bell cannot be taken twice
                return;
            }
            var parts = steps[step].Split(' ');
            string command = parts[0];
            switch (command)
            {
                case "trim":
                    input.TrimHeld = routeTrim = parts.Length > 1 && parts[1] == "on";
                    Next();
                    return;
                case "flame":
                    input.SelectSlot(int.Parse(parts[1]));
                    Next();
                    return;
            }

            Vector2 target = World(parts[1], parts[2]);
            if (showcase)
            {
                if (command == "fight")
                {
                    Next(); // an optional fight: only what attacks gets fought (Defend)
                    return;
                }
                if (toldStep != step)
                {
                    toldStep = step;
                    objective = Objective();
                    TellStep(command, parts, target);
                }
                if (Evade())
                    return;
                bool bossFight = command == "boss" || command == "judge" || command == "ending";
                if (!bossFight)
                {
                    if (Defend())
                        return;
                    if (command != "clear") // no trimmed wick in a fight: it halves the damage
                        Sneak();
                    else
                        input.TrimHeld = false;
                }
            }
            else
            {
                bool fightOnWay = command != "walk" && !input.TrimHeld;
                if (fightOnWay && Engage(engageRadius))
                    return;
            }

            switch (command)
            {
                case "goto":
                case "walk":
                    if (MoveTo(target, 0.6f))
                        Next();
                    break;
                case "fight":
                case "clear":
                {
                    float radius = parts.Length > 3 ? float.Parse(parts[3], CultureInfo.InvariantCulture) : 6f;
                    if (Engage(radius, target))
                        break;
                    if (MoveTo(target, 1f) && stepTime > 1.5f && !AnyAlive(target, radius))
                        Next();
                    break;
                }
                case "rest":
                case "read":
                case "take":
                case "ring":
                case "drink":
                case "climb":
                    if (Interact(target, command == "drink" ? 2.2f : command == "climb" ? 2.6f : 0.6f, command == "take" || command == "ring"))
                    {
                        if (command == "rest")
                            resumeStep = step + 1;
                        Next();
                    }
                    break;
                case "aim":
                {
                    // turn a crystal pedestal until it faces step k (0 = east, counter-clockwise, 45° steps)
                    int k = int.Parse(parts[3], CultureInfo.InvariantCulture);
                    var pedestal = Near<CrystalPedestal>(target);
                    if (pedestal != null && pedestal.facing == k)
                    {
                        Next();
                        break;
                    }
                    if (Interact(target, 0.25f))
                        interacted = false; // press again
                    break;
                }
                case "smash":
                {
                    var glass = Near<GlassBarrier>(target);
                    if (glass == null || glass.Broken)
                    {
                        Next();
                        break;
                    }
                    if (EquipFlameThatHurts(DamageTags.Glass))
                        break;
                    Strike(glass.transform.position + new Vector3(0f, 0.3f, 0f), 1.6f);
                    break;
                }
                case "rose":
                {
                    var rose = Near<RoseWindow>(target, 3f);
                    if (rose == null || rose.Complete)
                    {
                        Next();
                        break;
                    }
                    RosePanel panel = null;
                    foreach (var p in rose.panels)
                        if (p != null && !p.IsLit) { panel = p; break; }
                    if (panel == null)
                        break;
                    Note = $"rose: {rose.LitCount}/5 lit, panel {panel.flame}, flame {player.Flames.Current}, powered {rose.Powered}";
                    if (player.Flames.Current != panel.flame)
                    {
                        if (Time.time >= nextFlameSwitch && player.Flames.IsEquipped(panel.flame))
                        {
                            input.SelectSlot(player.Flames.SlotOf(panel.flame));
                            nextFlameSwitch = Time.time + 1.2f;
                        }
                        break;
                    }
                    Vector2 out_ = ((Vector2)panel.transform.position - (Vector2)rose.transform.position).normalized;
                    Vector2 stand = (Vector2)panel.transform.position + out_ * 0.85f;
                    if (MoveTo(stand, 0.25f))
                        Strike(panel.transform.position, 1.5f);
                    break;
                }
                case "boss":
                {
                    var king = Near<BossHollowKing>(target, 25f);
                    if (king == null || (king.Current != BossHollowKing.Mode.Dormant && king.Current != BossHollowKing.Mode.Fighting
                                         && king.Current != BossHollowKing.Mode.Shifting))
                    {
                        Next();
                        break;
                    }
                    Note = $"boss: {king.DebugState} hp {king.Body.Health:0}";
                    // the white flame is anti-Creux (PDF): prefer it against the King
                    if (player.Flames.IsEquipped(FlameId.White) && player.Flames.Current != FlameId.White)
                    {
                        if (Time.time >= nextFlameSwitch)
                        {
                            input.SelectSlot(player.Flames.SlotOf(FlameId.White));
                            nextFlameSwitch = Time.time + 1.5f;
                            Tell("bosswhite", "The White flame is anti-Hollow: it hurts the Hollow King the most", 2);
                        }
                        break;
                    }
                    Vector2 me = player.transform.position;
                    Vector2 kingAt = king.transform.position;
                    if (king.IsTelegraphingSlam && Vector2.Distance(me, king.SlamTarget) < Db.Boss.slamRadius * 0.6f + 0.8f)
                    {
                        Vector2 away = me - king.SlamTarget;
                        input.Move = away.sqrMagnitude > 0.01f ? away.normalized : Vector2.right;
                        if (player.Dodger.Charges > 0 && !player.IsDodging)
                        {
                            input.PressDodge();
                            Tell("bossdodge", "A dodge roll cannot be hit: the bot rolls out of the King's slam, through his void volleys and over his ash waves", 3);
                        }
                        break;
                    }
                    // dodge through a void volley or the ash wave's ring (a dodge is invulnerable)
                    float fromKing = Vector2.Distance(me, kingAt);
                    if ((king.VoidsComing || (king.AshWaveActive && Mathf.Abs(fromKing - king.WaveRadius) < 1.3f))
                        && player.Dodger.Charges > 0 && !player.IsDodging)
                    {
                        Vector2 side = me - kingAt;
                        input.Move = side.sqrMagnitude > 0.01f ? new Vector2(-side.y, side.x).normalized : Vector2.right;
                        input.PressDodge();
                        Tell("bossdodge", "A dodge roll cannot be hit: the bot rolls out of the King's slam, through his void volleys and over his ash waves", 3);
                        break;
                    }
                    Vector2 aim = kingAt + new Vector2(0f, 1.5f);
                    // a flame art when there is wax to spare (the white flame's art is anti-Creux too)
                    if (fromKing < 2.6f && player.Wax.Current > 45f && player.Combat.CanArt && !player.IsAttacking && Time.time >= nextArt)
                    {
                        input.PressArt(aim);
                        nextArt = Time.time + 3f;
                        Tell("art", "A flame art: a strong special attack that costs wax", 1);
                        break;
                    }
                    Strike(kingAt, 1.9f, aim);
                    break;
                }
                case "judge":
                case "ending":
                {
                    // answer the question like a person picking an option
                    int option = command == "judge" ? (parts[3] == "spare" ? 1 : 0) : parts[3][0] - 'A';
                    if (Choice.IsOpen)
                    {
                        if (Choice.Resolve(option))
                            Next();
                        else
                            Fail("option refused: " + parts[3]);
                        break;
                    }
                    if (Interact(target, 0.3f, false, command == "judge" ? 3f : 4.5f))
                        interacted = false;
                    break;
                }
                case "exit":
                    MoveTo(target, 0.2f);
                    if (RegionDirector.Instance == null || RegionDirector.Instance.Completed)
                        Next();
                    break;
                default:
                    Fail("unknown command " + command);
                    break;
            }
        }

        /// <param name="oneShot">The target can only be used once: if it still can be after the wait, the press was lost; press again.</param>
        static T Near<T>(Vector2 point, float radius = 1.5f) where T : Component
        {
            T best = null;
            float bestDistance = radius;
            foreach (var c in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                float d = Vector2.Distance(c.transform.position, point);
                if (d <= bestDistance)
                {
                    bestDistance = d;
                    best = c;
                }
            }
            return best;
        }

        /// <summary>Selects a flame that can hurt `tags` (true while switching).</summary>
        bool EquipFlameThatHurts(DamageTags tags)
        {
            if (CombatMath.CanHurt(tags, true, player.Flames.Current))
                return false;
            if (Time.time >= nextFlameSwitch)
            {
                for (int s = 0; s < player.Flames.Slots; s++)
                {
                    int f = player.Flames.Loadout[s];
                    if (f >= 0 && CombatMath.CanHurt(tags, true, (FlameId)f))
                    {
                        input.SelectSlot(s);
                        nextFlameSwitch = Time.time + 1.2f;
                        break;
                    }
                }
            }
            return true;
        }

        /// <summary>Walks into reach of a point and swings at it (aimed at `aim`, or the point).</summary>
        void Strike(Vector2 point, float reach, Vector2? aim = null)
        {
            Vector2 position = player.transform.position;
            Vector2 to = point - position;
            if (to.magnitude > reach)
            {
                MoveTo(point, reach * 0.8f);
                return;
            }
            input.Move = to.normalized * 0.2f; // face it
            if (player.Combat.CanAttack && !player.IsAttacking)
                input.PressAttack(aim ?? point); // aimed, like a mouse click on it
        }

        bool Interact(Vector2 target, float wait, bool oneShot = false, float near = 1.5f)
        {
            if (!interacted)
            {
                // Like a person: walk toward it and use it as soon as its prompt shows.
                float distance = Vector2.Distance(player.transform.position, target);
                var current = player.Interactor.Current;
                if (current == null || distance > near + 1.1f || Vector2.Distance(current.transform.position, target) > near)
                {
                    MoveTo(target, 0.2f);
                    if (stepTime > 30f)
                        Fail("nothing to interact with");
                    return false;
                }
                input.PressInteract();
                interacted = true;
                interactedWith = current;
                actedAt = Time.time;
                return false;
            }
            if (Time.time - actedAt < wait)
                return false;
            if (oneShot && interactedWith != null && interactedWith.CanInteract(player))
            {
                interacted = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Like a person at a press: never step under one that is about to slam or still lifting; if
        /// already beneath it, keep going.
        /// </summary>
        bool PressBlocks(Vector2 position, Vector2 waypoint)
        {
            var presses = Press.All;
            for (int i = 0; i < presses.Count; i++)
            {
                var p = presses[i];
                Vector2 at = p.transform.position;
                float now = Vector2.Distance(position, at);
                if (now > 3f || now < 1.35f)
                    continue; // far away, or already under it: move on
                if (Vector2.Distance(waypoint, at) >= now)
                    continue; // not heading toward it
                if (p.IsDownOrRising || p.SecondsUntilSlam < 1.05f)
                    return true;
            }
            return false;
        }

        /// <summary>Waits before stepping into a laser that burns or is about to; never stops on one.</summary>
        bool LaserBlocks(Vector2 position, Vector2 waypoint)
        {
            var lasers = LaserEmitter.All;
            if (lasers.Count == 0)
                return false;
            Vector2 step = waypoint - position;
            Vector2 ahead = position + (step.sqrMagnitude > 0.0001f ? step.normalized * 0.95f : Vector2.zero);
            for (int i = 0; i < lasers.Count; i++)
            {
                var l = lasers[i];
                if (l.DistanceToPath(position) < 0.8f)
                    continue; // already on it: get off
                if (l.DistanceToPath(ahead) < 0.8f && (l.IsFiring || l.SecondsUntilFire < 0.9f))
                    return true;
            }
            return false;
        }

        bool MoveTo(Vector2 target, float tolerance)
        {
            Vector2 position = player.transform.position;
            Vector2 to = target - position;
            if (to.magnitude <= tolerance)
                return true;
            var nav = NavGrid.Active;
            if (nav == null)
            {
                input.Move = to.normalized;
                return false;
            }
            if (pathCount == 0 || (pathTarget - target).sqrMagnitude > 0.25f || Time.time > repath)
            {
                pathCount = nav.FindPath(position, target, path, 20000);
                pathIndex = 0;
                pathTarget = target;
                repath = Time.time + 1.5f;
                if (pathCount == 0)
                {
                    input.Move = to.normalized; // let physics sort it out
                    return false;
                }
            }
            while (pathIndex < pathCount - 1 && (path[pathIndex] - position).sqrMagnitude < 0.2f)
                pathIndex++;
            Vector2 waypoint = pathIndex >= pathCount - 1 ? target : path[pathIndex];
            bool press = PressBlocks(position, waypoint);
            if (press || LaserBlocks(position, waypoint))
            {
                input.Move = Vector2.zero;
                if (press)
                    Tell("press", "The presses slam down on a rhythm: the bot waits for one to lift, then runs under it", 2);
                else
                    Tell("laser", "The lasers fire on a rhythm: the bot waits for a pause before crossing", 2);
                return false;
            }
            Vector2 d = waypoint - position;
            input.Move = d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.zero;
            return false;
        }

        static bool AnyAlive(Vector2 center, float radius)
        {
            var all = EnemyBrain.All;
            for (int i = 0; i < all.Count; i++)
                if (!all[i].IsDead && ((Vector2)all[i].transform.position - center).sqrMagnitude <= radius * radius)
                    return true;
            return false;
        }

        /// <summary>Showcase: a creature that has noticed the player (or is striking) is hunting it.</summary>
        bool Hunting(EnemyBrain e)
        {
            if (e.Anim == EnemyAnim.Windup || e.Anim == EnemyAnim.Attack)
                return true;
            switch (e.DebugState)
            {
                case "Alert":
                case "Chase":
                case "Attack":
                case "Mimic":
                case "Dropping":
                    return true;
            }
            // the Unlit drift toward the strongest light: hunting when that light is the player's
            return e is UnlitBrain unlit && !unlit.IsLured && (unlit.Target - (Vector2)player.transform.position).sqrMagnitude < 2.25f;
        }

        /// <summary>Showcase: fights back only against a creature that is on the player. True while fighting.</summary>
        bool Defend()
        {
            Vector2 me = player.transform.position;
            EnemyBrain threat = null;
            float best = threatRadius * threatRadius;
            var all = EnemyBrain.All;
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e.IsDead || !e.isActiveAndEnabled || e.type == null || !Hunting(e))
                    continue;
                float d = ((Vector2)e.transform.position - me).sqrMagnitude;
                if (e is UnlitBrain)
                {
                    // the Unlit drain wax on touch: meet them early, with the anti-Hollow flame
                    Tell("unlit", "The Unlit are holes in the dark: they drift toward the strongest light and drain wax on touch. The White flame burns them", 3);
                    if (d < 36f)
                        ReadyFlame(FlameId.White);
                    d *= 0.5f; // counts as closer: fought from about 3.4 units
                }
                if (d < best)
                {
                    best = d;
                    threat = e;
                }
            }
            if (threat == null)
                return false;
            input.TrimHeld = false; // a trimmed wick halves the damage
            Tell("defend", $"{Capital(AName(threat.type.kind))} caught up with the bot, so it fights back. It only fights when it has to", 2);
            return Fight(threat);
        }

        /// <summary>Showcase: switches to `flame` ahead of a fight, if it is equipped.</summary>
        void ReadyFlame(FlameId flame)
        {
            if (player.Flames.Current == flame || !player.Flames.IsEquipped(flame) || Time.time < nextFlameSwitch)
                return;
            input.SelectSlot(player.Flames.SlotOf(flame));
            nextFlameSwitch = Time.time + 1.5f;
        }

        /// <summary>Showcase: holds the wick trimmed while an unaware creature is near (smaller light, harder to see).</summary>
        void Sneak()
        {
            Vector2 me = player.transform.position;
            EnemyBrain near = null;
            var all = EnemyBrain.All;
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e.IsDead || !e.isActiveAndEnabled || e.type == null || e.type.kind == EnemyKind.HollowKing || Hunting(e))
                    continue;
                if (((Vector2)e.transform.position - me).sqrMagnitude < sneakRadius * sneakRadius)
                {
                    near = e;
                    break;
                }
            }
            bool sneaking = near != null && player.Dim.CanTrim;
            input.TrimHeld = routeTrim || sneaking;
            if (sneaking || routeTrim)
            {
                Tell("sneak", "Golden rule: you cannot be strong and hidden at once. Near enemies the bot trims its wick: " +
                              "a small light is hard to see, but it hits half as hard", 3);
                if (told.Contains("sneak"))
                    Tell("avoid", "The bot walks past creatures instead of fighting them: each enemy avoided adds 20 points to the score", 1);
            }
        }

        /// <summary>Showcase: rolls away from a blow about to land or a shot about to hit. True when it rolled.</summary>
        bool Evade()
        {
            if (player.IsDodging || player.Dodger.Charges <= 0)
                return false;
            Vector2 me = player.transform.position;
            var all = EnemyBrain.All;
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e.IsDead || !e.isActiveAndEnabled || e.type == null || e.Anim != EnemyAnim.Windup)
                    continue;
                Vector2 at = e.transform.position;
                float reach = e.type.attackRange + 1f;
                if ((at - me).sqrMagnitude > reach * reach)
                    continue;
                if (Time.time - e.AnimSince < e.type.attackWindup - 0.3f)
                    continue; // roll just before the blow lands
                Roll(me - at, $"Dodge roll: the {EnemyName(e.type.kind)} attack misses (a roll cannot be hit)");
                return true;
            }
            var shots = Combat.Projectile.Active;
            Vector2 body = me + new Vector2(0f, 0.4f);
            for (int i = 0; i < shots.Count; i++)
            {
                var p = shots[i];
                if (p == null || !p.FromEnemy)
                    continue;
                Vector2 v = p.Velocity;
                float speed2 = v.sqrMagnitude;
                if (speed2 < 0.01f)
                    continue;
                Vector2 rel = body - (Vector2)p.transform.position;
                float t = Vector2.Dot(rel, v) / speed2; // seconds until the closest approach
                if (t <= 0f || t > 0.4f || (rel - v * t).sqrMagnitude > 0.64f)
                    continue;
                Roll(new Vector2(-v.y, v.x), "Dodge roll: out of the way of an incoming shot");
                return true;
            }
            return false;
        }

        static readonly float[] RollAngles = { 0f, 45f, -45f, 90f, -90f, 180f };

        /// <summary>Rolls toward `preferred`, or the closest direction with room (a roll into a wall goes nowhere).</summary>
        void Roll(Vector2 preferred, string why)
        {
            Vector2 me = player.transform.position;
            Vector2 dir = preferred.sqrMagnitude > 0.0001f ? preferred.normalized : Vector2.right;
            Vector2 bestDir = dir;
            float bestRoom = -1f;
            float want = Db.Balance.dodgeDistance;
            for (int k = 0; k < RollAngles.Length; k++)
            {
                Vector2 d = Quaternion.Euler(0f, 0f, RollAngles[k]) * dir;
                var hit = Physics2D.Raycast(me, d, want + 0.4f, Layers.WallMask);
                float room = hit.collider != null ? hit.distance : want + 0.4f;
                if (room > bestRoom + 0.3f)
                {
                    bestRoom = room;
                    bestDir = d;
                }
                if (room >= want)
                    break;
            }
            input.Move = bestDir;
            input.PressDodge();
            Tell("dodge", "Dodge roll: a quick dash that cannot be hit. It has one charge, back after about a second", 2);
        }

        /// <summary>Fights the nearest living creature in range. Returns true while busy fighting.</summary>
        bool Engage(float radius, Vector2? around = null)
        {
            Vector2 position = player.transform.position;
            Vector2 center = around ?? position;
            EnemyBrain best = null;
            float bestDistance = float.MaxValue;
            var all = EnemyBrain.All;
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                if (e.IsDead || !e.isActiveAndEnabled)
                    continue;
                Vector2 p = e.transform.position;
                if ((p - center).sqrMagnitude > radius * radius)
                    continue;
                float d = (p - position).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = e;
                }
            }
            return best != null && Fight(best);
        }

        /// <summary>Fights one creature: a flame that can hurt it, then into reach and swing. True while busy.</summary>
        bool Fight(EnemyBrain best)
        {
            Vector2 position = player.transform.position;

            // Like a person: pick a flame that can hurt it (Slagborn need white or green, PDF §2.3).
            var tags = best.type != null ? best.type.tags : DamageTags.None;
            if (!CombatMath.CanHurt(tags, true, player.Flames.Current) && Time.time >= nextFlameSwitch)
            {
                for (int s = 0; s < player.Flames.Slots; s++)
                {
                    int f = player.Flames.Loadout[s];
                    if (f >= 0 && CombatMath.CanHurt(tags, true, (FlameId)f))
                    {
                        input.SelectSlot(s);
                        nextFlameSwitch = Time.time + 1.5f;
                        if (showcase && best.type != null)
                            Tell("armour", $"The {EnemyName(best.type.kind)} is armoured: only some flames hurt it, so the bot switches to the {FlameName((FlameId)f)} flame", 2);
                        return true;
                    }
                }
            }

            if (showcase && (tags & DamageTags.Hollow) != 0 && player.Flames.IsEquipped(FlameId.White) && player.Flames.Current != FlameId.White
                && Time.time >= nextFlameSwitch)
            {
                ReadyFlame(FlameId.White);
                return true;
            }

            Vector2 enemy = best.transform.position;
            float distance = Vector2.Distance(enemy, position);
            Vector2 dir = (enemy - position).normalized;

            // dodge a wind-up at close range, sideways
            if (best.Anim == EnemyAnim.Windup && distance < 1.8f && player.Dodger.Charges > 0)
            {
                input.Move = new Vector2(-dir.y, dir.x);
                input.PressDodge();
                return true;
            }

            float reach = player.Flames.CurrentType.attackStyle == FlameAttackStyle.Bolt ? 5f : 1.15f;
            if (distance > reach)
            {
                MoveTo(enemy, reach * 0.8f);
                return true;
            }
            input.Move = dir * 0.2f; // face it
            if (player.Combat.CanAttack && !player.IsAttacking)
                input.PressAttack(enemy + new Vector2(0f, 0.4f)); // aimed at its body
            return true;
        }
    }
}
