using UnityEngine;
using UnityEngine.Rendering.Universal;
using Wickfall.Combat;
using Wickfall.Meta;

namespace Wickfall.World
{
    /// <summary>
    /// PDF §2.3: "chaque panneau de la Rose demande une flamme différente" (G23: R4's climax). The rose
    /// window wakes only while a beam reaches its eye (the pedestals); then each of its 5 panels lights
    /// when struck with its own flame, and only that one. All five lit opens the way for good.
    /// </summary>
    public sealed class RoseWindow : MonoBehaviour
    {
        public BeamSocket eye;
        public RosePanel[] panels = new RosePanel[0];
        public string gateId = "";
        public string id = "rose";
        public Light2D glow;

        public bool Powered => Complete || (eye != null && (eye.Lit || eye.Solved));
        public bool Complete { get; private set; }
        public int LitCount
        {
            get
            {
                int n = 0;
                foreach (var p in panels)
                    if (p != null && p.IsLit)
                        n++;
                return n;
            }
        }

        void Start()
        {
            foreach (var p in panels)
                if (p != null)
                    p.rose = this;
            if (GameSession.Instance != null && GameSession.Instance.HasFlag("rose/" + id))
            {
                Complete = true;
                foreach (var p in panels)
                    p?.SetLit(true, false);
            }
        }

        void Update()
        {
            if (glow != null)
                glow.intensity = Complete ? 1.4f : Powered ? 0.4f + 0.18f * LitCount : 0.1f;
        }

        public void PanelLit()
        {
            if (Complete || LitCount < panels.Length)
                return;
            Complete = true;
            GameSession.Instance?.SetFlag("rose/" + id);
            Events.Play(SoundId.RoseComplete, transform.position);
            Events.Shake(0.15f, 0.5f);
            Vfx.Play(VfxId.RadiantPulse, transform.position);
            if (!string.IsNullOrEmpty(gateId))
                Gate.Find(gateId)?.Open();
        }
    }
}
