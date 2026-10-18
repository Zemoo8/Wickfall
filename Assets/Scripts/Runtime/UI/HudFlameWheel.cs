using UnityEngine;
using UnityEngine.UI;

namespace Wickfall.UI
{
    /// <summary>
    /// PDF §3 HUD: "radial flame wheel with five slots". Opens while the wheel key is held; the
    /// highlighted socket follows the stick or WASD. Locked slots (the fifth needs the flame-slot
    /// relic) and empty slots are shown as such.
    /// </summary>
    public sealed class HudFlameWheel : MonoBehaviour
    {
        public HudSprites sprites;
        public CanvasGroup group;
        public Image ring;
        public Image[] sockets = new Image[5];
        public Image[] icons = new Image[5];
        public float radius = 34f;

        int highlighted = -2;
        int[] loadout = { 0, -1, -1, -1, -1 };
        int slots = 4;
        FlameId current;
        float openAmount;

        void OnEnable()
        {
            Events.FlameWheel.Register(OnWheel);
            Events.FlameSwitched.Register(OnFlame);
            Events.ProgressChanged.Register(Refresh);
            Refresh();
        }

        void OnDisable()
        {
            Events.FlameWheel.Unregister(OnWheel);
            Events.FlameSwitched.Unregister(OnFlame);
            Events.ProgressChanged.Unregister(Refresh);
        }

        void OnWheel(int value)
        {
            highlighted = value;
            Refresh();
        }

        void OnFlame(FlameSwitch change)
        {
            current = change.To;
            Refresh();
        }

        /// <summary>The wheel reads the loadout from the player's switcher via the session (UI only).</summary>
        public void Refresh()
        {
            var session = Meta.GameSession.Instance;
            if (session != null && session.player != null)
            {
                var switcher = session.player.Flames;
                slots = switcher.Slots;
                for (int i = 0; i < loadout.Length; i++)
                    loadout[i] = switcher.Loadout[i];
            }
            if (sprites == null)
                return;
            for (int i = 0; i < sockets.Length; i++)
            {
                float angle = (90f - i * 72f) * Mathf.Deg2Rad; // slot 0 at the top, clockwise
                var pos = new Vector2(Mathf.Round(Mathf.Cos(angle) * radius), Mathf.Round(Mathf.Sin(angle) * radius));
                if (sockets[i] != null)
                {
                    sockets[i].rectTransform.anchoredPosition = pos;
                    sockets[i].sprite = i == highlighted ? sprites.wheelSocketSelected : sprites.wheelSocket;
                    sockets[i].color = i < slots ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                }
                if (icons[i] != null)
                {
                    icons[i].rectTransform.anchoredPosition = pos;
                    bool has = i < slots && loadout[i] >= 0;
                    icons[i].enabled = true;
                    icons[i].sprite = has ? sprites.flameIcons[loadout[i]] : sprites.flameIconLocked;
                    icons[i].color = has ? (loadout[i] == (int)current ? Color.white : new Color(0.8f, 0.78f, 0.72f, 1f))
                        : new Color(1f, 1f, 1f, i < slots ? 0.25f : 0.12f);
                }
            }
        }

        void Update()
        {
            float target = highlighted >= -1 ? 1f : 0f;
            openAmount = Mathf.MoveTowards(openAmount, target, Time.unscaledDeltaTime * 10f);
            if (group != null)
            {
                group.alpha = openAmount;
                group.blocksRaycasts = false;
            }
            transform.localScale = Vector3.one * (0.8f + 0.2f * openAmount);
        }
    }
}
