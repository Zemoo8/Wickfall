using TMPro;
using UnityEngine;

namespace Wickfall.UI
{
    /// <summary>Shows the region's name and environment line when the player enters it.</summary>
    public sealed class RegionTitleCard : MonoBehaviour
    {
        CanvasGroup group;
        TextMeshProUGUI title, subtitle;
        float shownAt = -100f;
        const float Duration = 4.5f;

        void Start()
        {
            var kit = UiKit.Instance;
            if (kit == null)
                return;
            var root = UiKit.Rect(transform, "RegionTitle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(400f, 50f));
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            title = kit.Label(root, "Name", "", new Vector2(0f, 8f), new Vector2(400f, 24f), TextAlignmentOptions.Center, UiKit.Bone, 24f);
            subtitle = kit.Label(root, "Env", "", new Vector2(0f, -14f), new Vector2(400f, 14f), TextAlignmentOptions.Center, UiKit.BoneDim);
        }

        void OnEnable() => Events.RegionEntered.Register(OnRegion);
        void OnDisable() => Events.RegionEntered.Unregister(OnRegion);

        void OnRegion(RegionId region)
        {
            if (title == null)
                return;
            var data = Db.Region(region);
            title.text = Loc.Get(data.nameKey);
            subtitle.text = Loc.Get(data.environmentKey);
            shownAt = Time.unscaledTime + 0.8f;
        }

        void Update()
        {
            if (group == null)
                return;
            float t = Time.unscaledTime - shownAt;
            float a = t < 0f ? 0f : t < 0.8f ? t / 0.8f : t < Duration - 1f ? 1f : Mathf.Clamp01((Duration - t) / 1f);
            group.alpha = a;
        }
    }
}
