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
    ///   goto x y | walk x y (no fighting) | fight x y radius | rest x y | drink x y | read x y |
    ///   take x y | ring x y | exit x y | trim on|off | flame n
    /// After a death the bot resumes from the step after its last rest.
    /// </summary>
    public sealed class RegionBot : MonoBehaviour
    {
        public float stepTimeout = 90f;
        public float engageRadius = 3.2f;

        PlayerController player;
        readonly ScriptedInput input = new ScriptedInput();
        LevelData level;
        string[] steps;
        int step;
        int resumeStep;
        float stepTime;
        float actedAt;
        bool interacted;
        readonly Vector2[] path = new Vector2[256];
        int pathCount, pathIndex;
        Vector2 pathTarget;
        float repath;

        public int Step => step;
        public int StepCount => steps != null ? steps.Length : 0;
        public bool Done { get; private set; }
        public bool Failed { get; private set; }
        public string FailReason { get; private set; }
        public string CurrentStep => steps != null && step < steps.Length ? steps[step] : "-";
        public int Deaths { get; private set; }
        public int Kills { get; private set; }

        public void Begin(PlayerController target, LevelData data)
        {
            player = target;
            level = data;
            steps = data.BotScript.ToArray();
            step = 0;
            resumeStep = 0;
            stepTime = 0f;
            Done = Failed = false;
            player.SetInputSource(input);
            Events.PlayerRespawned.Register(OnRespawned);
            Events.EnemyKilled.Register(OnKill);
        }

        void OnDestroy()
        {
            Events.PlayerRespawned.Unregister(OnRespawned);
            Events.EnemyKilled.Unregister(OnKill);
            if (player != null)
                player.SetInputSource(null);
        }

        void OnKill(EnemyKilledInfo info) => Kills++;

        void OnRespawned()
        {
            Deaths++;
            step = resumeStep;
            stepTime = 0f;
            pathCount = 0;
            input.TrimHeld = false;
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
            FailReason = $"step {step} '{CurrentStep}': {reason}";
            input.Move = Vector2.zero;
            Debug.LogWarning("[WICKFALL Bot] " + FailReason);
        }

        void Next()
        {
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

            var parts = steps[step].Split(' ');
            string command = parts[0];
            switch (command)
            {
                case "trim":
                    input.TrimHeld = parts.Length > 1 && parts[1] == "on";
                    Next();
                    return;
                case "flame":
                    input.SelectSlot(int.Parse(parts[1]));
                    Next();
                    return;
            }

            Vector2 target = World(parts[1], parts[2]);
            bool fightOnWay = command != "walk" && !input.TrimHeld;
            if (fightOnWay && Engage(engageRadius))
                return;

            switch (command)
            {
                case "goto":
                case "walk":
                    if (MoveTo(target, 0.6f))
                        Next();
                    break;
                case "fight":
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
                    if (Interact(target, command == "drink" ? 2.2f : 0.6f))
                    {
                        if (command == "rest")
                            resumeStep = step + 1;
                        Next();
                    }
                    break;
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

        bool Interact(Vector2 target, float wait)
        {
            if (!interacted)
            {
                // Like a person: walk toward it and use it as soon as its prompt shows.
                float distance = Vector2.Distance(player.transform.position, target);
                var current = player.Interactor.Current;
                if (current == null || distance > 2.6f || Vector2.Distance(current.transform.position, target) > 1.5f)
                {
                    MoveTo(target, 0.2f);
                    if (stepTime > 30f)
                        Fail("nothing to interact with");
                    return false;
                }
                input.PressInteract();
                interacted = true;
                actedAt = Time.time;
                return false;
            }
            return Time.time - actedAt >= wait;
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
            if (best == null)
                return false;

            Vector2 enemy = best.transform.position;
            float distance = Mathf.Sqrt(bestDistance);
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
                input.PressAttack();
            return true;
        }
    }
}
