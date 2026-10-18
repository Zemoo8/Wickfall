using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Wickfall.UI
{
    /// <summary>Wax shards (PDF §3: "wax shard currency icon 16x16"), dodge charges ("small dash-charge icons") and status icons.</summary>
    public sealed class HudCounters : MonoBehaviour
    {
        public HudSprites sprites;
        public TMP_Text shardText;
        public Image[] dodgeIcons = new Image[4];
        public Image sootIcon;
        public Image lightCutIcon;

        int shards = -1;
        float shardBump;

        void OnEnable()
        {
            Events.ShardsChanged.Register(OnShards);
            Events.DodgeChanged.Register(OnDodge);
            Events.StatusChanged.Register(OnStatus);
            if (Events.ShardsChanged.HasLast) OnShards(Events.ShardsChanged.Last);
            if (Events.DodgeChanged.HasLast) OnDodge(Events.DodgeChanged.Last);
            if (sootIcon != null) sootIcon.enabled = false;
            if (lightCutIcon != null) lightCutIcon.enabled = false;
        }

        void OnDisable()
        {
            Events.ShardsChanged.Unregister(OnShards);
            Events.DodgeChanged.Unregister(OnDodge);
            Events.StatusChanged.Unregister(OnStatus);
        }

        void OnShards(int value)
        {
            if (value > shards && shards >= 0)
                shardBump = 1f;
            shards = value;
            if (shardText != null)
                shardText.text = value.ToString();
        }

        void OnDodge(DodgeState state)
        {
            for (int i = 0; i < dodgeIcons.Length; i++)
            {
                if (dodgeIcons[i] == null)
                    continue;
                dodgeIcons[i].enabled = i < state.MaxCharges;
                if (sprites != null)
                    dodgeIcons[i].sprite = i < state.Charges ? sprites.dodgeFull : sprites.dodgeEmpty;
            }
        }

        void OnStatus(StatusInfo info)
        {
            if (info.Effect == HitEffects.Soot && sootIcon != null)
                sootIcon.enabled = info.Active;
            if (info.Effect == HitEffects.LightCut && lightCutIcon != null)
                lightCutIcon.enabled = info.Active;
        }

        void Update()
        {
            if (shardText == null)
                return;
            shardBump = Mathf.MoveTowards(shardBump, 0f, Time.unscaledDeltaTime * 4f);
            shardText.rectTransform.localScale = Vector3.one * (1f + Mathf.Round(shardBump * 2f) / 4f);
        }
    }
}
