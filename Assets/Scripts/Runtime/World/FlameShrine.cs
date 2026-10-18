using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Meta;
using Wickfall.Player;

namespace Wickfall.World
{
    /// <summary>
    /// Where a region's flame is found (G03, PDF §2.5 "trouver une flamme de cire"). A wax altar
    /// holding a flame of a new colour; using it adds the flame to the wheel.
    /// </summary>
    public sealed class FlameShrine : Interactable
    {
        public FlameId flame = FlameId.Blue;
        public SpriteRenderer flameSprite;
        public Light2D glow;

        bool taken;

        void Start()
        {
            promptKey = "prompt.flame";
            var session = GameSession.Instance;
            taken = session != null && session.Save.HasFlame(flame);
            Refresh();
        }

        public override bool CanInteract(PlayerController player) => base.CanInteract(player) && !taken;

        public override void Interact(PlayerController player)
        {
            taken = true;
            GameSession.Instance?.UnlockFlame(flame);
            var type = Db.Flame(flame);
            Combat.Vfx.Play(Combat.VfxId.Flare, (Vector2)transform.position + new Vector2(0f, 0.5f), Vector2.up, type.bodyColor, 1.4f);
            Events.Play(SoundId.FlameSwitch, transform.position);
            Events.Shake(0.12f, 0.3f);
            UI.ItemCard.Show?.Invoke(type.nameKey, type.roleKey, UI.ItemCard.Kind.Flame);
            Refresh();
        }

        void Refresh()
        {
            var type = Db.Flame(flame);
            if (flameSprite != null)
            {
                flameSprite.enabled = !taken;
                flameSprite.color = type.bodyColor;
            }
            if (glow != null)
            {
                glow.color = type.lightColor;
                glow.intensity = taken ? 0.2f : 1.2f;
            }
        }
    }
}
