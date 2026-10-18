using System.Collections.Generic;
using UnityEngine;
using Wickfall.Combat;
using Wickfall.Meta;
using Wickfall.Player;
using Wickfall.Presentation;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.2 Cathédrale de Verre: "sols de verre qui cassent sous la flamme cramoisie". A glass floor
    /// cell cracks while the player stands on it carrying the crimson flame (2 crack stages, A24), then
    /// gives way: the player drops into the dark below (the hazard's 12) and climbs back to the last
    /// solid ground. The hole mends when you rest at a brazier or relight.
    /// </summary>
    public sealed class GlassFloor : MonoBehaviour
    {
        public HazardSO hazard;
        public SpriteRenderer sprite;
        public Sprite intact;
        public Sprite crack1;
        public Sprite crack2;
        public Sprite hole;
        public Collider2D pit;
        public float stageSeconds = 0.4f;

        float stress;
        public bool Broken { get; private set; }
        public int Stage => Broken ? 3 : stress >= stageSeconds * 2f ? 2 : stress >= stageSeconds ? 1 : 0;

        static Vector2 lastSafe;
        static int lastSafeFrame = -1;
        static readonly HashSet<Vector2Int> glassCells = new HashSet<Vector2Int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            glassCells.Clear();
            lastSafe = Vector2.zero;
            lastSafeFrame = -1;
        }

        void OnEnable()
        {
            glassCells.Add(LevelData.CellOf(transform.position));
            Events.BrazierRested.Register(OnRested);
            Events.PlayerRespawned.Register(Mend);
        }

        void OnDisable()
        {
            glassCells.Remove(LevelData.CellOf(transform.position));
            Events.BrazierRested.Unregister(OnRested);
            Events.PlayerRespawned.Unregister(Mend);
        }

        void Start()
        {
            if (hazard == null)
                hazard = Db.Hazard(HazardKind.GlassFloor);
            Show();
        }

        void Mend()
        {
            if (!Broken && stress <= 0f)
                return;
            Broken = false;
            stress = 0f;
            if (pit != null)
                pit.enabled = false;
            NavGrid.Active?.SetBlocked(LevelData.CellOf(transform.position), false);
            Show();
        }


        void OnRested(int brazier) => Mend();

        void Update()
        {
            if (Broken || !PresentationMode.IsEnabled(Feature.Hazards))
                return;
            var session = GameSession.Instance;
            var player = session != null ? session.player : null;
            if (player == null || !player.isActiveAndEnabled || player.IsExtinguished)
                return;
            var cell = LevelData.CellOf(transform.position);
            bool onMe = LevelData.CellOf(player.transform.position) == cell && !player.IsDodging;
            if (!onMe)
            {
                stress = Mathf.Max(0f, stress - Time.deltaTime * 0.5f);
                Show();
                return;
            }
            if (player.Flames.Current != FlameId.Crimson)
                return;
            int before = Stage;
            stress += Time.deltaTime;
            if (Stage != before && Stage < 3)
                Events.Play(SoundId.GlassCrack, transform.position);
            if (stress >= stageSeconds * 3f)
                Give(player);
            Show();
        }

        /// <summary>Where the player last stood on solid ground (refreshed by every glass cell each frame).</summary>
        public static void NoteSafe(PlayerController player)
        {
            if (lastSafeFrame == Time.frameCount)
                return;
            lastSafeFrame = Time.frameCount;
            var nav = NavGrid.Active;
            var cell = LevelData.CellOf(player.transform.position);
            if (glassCells.Contains(cell))
                return;
            if (nav == null || nav.IsWalkable(cell))
                lastSafe = player.transform.position;
        }

        void LateUpdate()
        {
            var session = GameSession.Instance;
            if (session != null && session.player != null && session.player.isActiveAndEnabled)
                NoteSafe(session.player);
        }

        void Give(PlayerController player)
        {
            Broken = true;
            Events.Play(SoundId.GlassBreak, transform.position);
            Vfx.Play(VfxId.GlassShards, transform.position);
            if (pit != null)
                pit.enabled = true;
            NavGrid.Active?.SetBlocked(LevelData.CellOf(transform.position), true);
            var hit = HitData.FromHazard(hazard.damage, transform.position, 0f);
            player.Body.TakeHit(hit);
            if (lastSafe != Vector2.zero)
                player.Teleport(lastSafe);
        }

        void Show()
        {
            if (sprite == null)
                return;
            switch (Stage)
            {
                case 0: sprite.sprite = intact; break;
                case 1: sprite.sprite = crack1 != null ? crack1 : intact; break;
                case 2: sprite.sprite = crack2 != null ? crack2 : intact; break;
                default: sprite.sprite = hole != null ? hole : crack2; break;
            }
        }
    }
}
