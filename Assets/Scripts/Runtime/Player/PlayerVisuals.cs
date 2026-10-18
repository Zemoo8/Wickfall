using UnityEngine;
using Wickfall.Combat;
using Wickfall.Presentation;

namespace Wickfall.Player
{
    /// <summary>
    /// Draws the Wickbearer: body frames picked from the state machine, and a separate flame layer
    /// recoloured to the active flame (Wickfall/FlameSprite). The flame visibly shrinks as wax
    /// drops and becomes a tiny bead while trimmed, so the light is readable on the sprite too.
    /// </summary>
    public sealed class PlayerVisuals : MonoBehaviour
    {
        public PlayerSpriteSet sprites;
        public SpriteRenderer body;
        public SpriteAnimator bodyAnimator;
        public SpriteRenderer flame;
        public SpriteAnimator flameAnimator;
        public SpriteRenderer shadow;
        public Transform squashRoot;

        PlayerController player;
        MaterialPropertyBlock bodyBlock;
        MaterialPropertyBlock flameBlock;
        float flash;
        float wax = 100f;
        float waxMax = 100f;
        FlameId currentFlame = FlameId.Amber;
        bool deathPlayed;
        static readonly int FlashId = Shader.PropertyToID("_Flash");
        static readonly int CoreId = Shader.PropertyToID("_Core");
        static readonly int BodyId = Shader.PropertyToID("_Body");
        static readonly int EdgeId = Shader.PropertyToID("_Edge");
        static readonly int EmissionId = Shader.PropertyToID("_Emission");

        void Awake()
        {
            player = GetComponentInParent<PlayerController>();
            bodyBlock = new MaterialPropertyBlock();
            flameBlock = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            Events.WaxChanged.Register(OnWax);
            Events.FlameSwitched.Register(OnFlame);
            Events.PlayerHit.Register(OnHit);
            Events.PlayerRespawned.Register(OnRespawn);
        }

        void OnDisable()
        {
            Events.WaxChanged.Unregister(OnWax);
            Events.FlameSwitched.Unregister(OnFlame);
            Events.PlayerHit.Unregister(OnHit);
            Events.PlayerRespawned.Unregister(OnRespawn);
        }

        void OnWax(WaxChange change)
        {
            wax = change.Current;
            waxMax = change.Max;
        }

        void OnFlame(FlameSwitch change)
        {
            currentFlame = change.To;
            ApplyFlameColors();
            if (change.From != change.To)
                Vfx.Play(VfxId.Flare, (Vector2)transform.position + new Vector2(0f, 2f), Vector2.up, Db.Flame(change.To).bodyColor);
        }

        void OnHit(HitReport report) => flash = 1f;

        void OnRespawn() => deathPlayed = false;

        void Start() => ApplyFlameColors();

        void ApplyFlameColors()
        {
            if (flame == null)
                return;
            var f = Db.Flame(currentFlame);
            flame.GetPropertyBlock(flameBlock);
            flameBlock.SetColor(CoreId, f.coreColor);
            flameBlock.SetColor(BodyId, f.bodyColor);
            flameBlock.SetColor(EdgeId, f.edgeColor);
            flameBlock.SetFloat(EmissionId, 1.9f);
            flame.SetPropertyBlock(flameBlock);
        }

        void LateUpdate()
        {
            if (sprites == null || player == null)
                return;

            Facing facing = player.Facing;
            bool trimmed = player.Dim.IsTrimmed;
            bool showFlame = true;
            Vector2 squash = Vector2.one;

            if (player.IsExtinguished)
            {
                if (!deathPlayed)
                {
                    bodyAnimator.Play(sprites.death, true);
                    deathPlayed = true;
                }
                showFlame = false;
            }
            else if (player.IsAttacking)
            {
                bodyAnimator.Play(sprites.Attack(facing));
            }
            else if (player.IsDodging)
            {
                bodyAnimator.Play(sprites.Dash(facing));
                // Squash-and-stretch along the dash (PDF: "dash with squash-and-stretch").
                float p = player.DodgeProgress;
                float stretch = p < 0.3f ? Mathf.Lerp(1f, 0.8f, p / 0.3f) : Mathf.Lerp(1.15f, 1f, (p - 0.3f) / 0.7f);
                squash = facing == Facing.Left || facing == Facing.Right
                    ? new Vector2(2f - stretch, stretch)
                    : new Vector2(stretch, 2f - stretch);
            }
            else if (player.IsHurt)
            {
                bodyAnimator.Play(sprites.hurt);
            }
            else if (trimmed)
            {
                if (player.IsMoving)
                    bodyAnimator.Play(sprites.Walk(facing));
                else
                    bodyAnimator.Play(sprites.dim);
            }
            else if (player.IsMoving)
            {
                bodyAnimator.Play(sprites.Walk(facing));
            }
            else
            {
                bodyAnimator.Play(sprites.Idle(facing));
            }

            if (squashRoot != null)
                squashRoot.localScale = new Vector3(squash.x, squash.y, 1f);

            // Flash on hits (PresentationMode can hide it in Phase 4: no juice).
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 6f);
            if (body != null)
            {
                body.GetPropertyBlock(bodyBlock);
                bodyBlock.SetFloat(FlashId, PresentationMode.IsEnabled(Feature.VFX) ? flash : 0f);
                body.SetPropertyBlock(bodyBlock);
            }

            UpdateFlame(showFlame, trimmed, facing);
        }

        void UpdateFlame(bool show, bool trimmed, Facing facing)
        {
            if (flame == null)
                return;
            flame.enabled = show;
            if (!show)
                return;

            if (flameAnimator != null)
                flameAnimator.Play(trimmed && sprites.flameDim.IsValid ? sprites.flameDim : sprites.flame);

            float ppu = Mathf.Max(1, sprites.pixelsPerUnit);
            Vector2 anchor;
            if (trimmed && !player.IsMoving && !player.IsAttacking)
            {
                anchor = sprites.dimFlameAnchor;
            }
            else
            {
                anchor = sprites.flameAnchor[(int)facing];
                if (player.IsMoving && bodyAnimator.Frame >= 0 && sprites.walkBob.Length > 0)
                    anchor.y += sprites.walkBob[bodyAnimator.Frame % sprites.walkBob.Length];
            }
            flame.transform.localPosition = new Vector3(anchor.x / ppu, anchor.y / ppu, 0f);

            // The flame grows with wax and shrinks to a bead while trimmed.
            float t = Mathf.Clamp01(wax / Mathf.Max(1f, Db.Wax.maxWax));
            float size = Mathf.Lerp(0.55f, 1f, t) * Mathf.Lerp(1f, 0.45f, player.Dim.Blend);
            flame.transform.localScale = new Vector3(size, size, 1f);
        }
    }
}
