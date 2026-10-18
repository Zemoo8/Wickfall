using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wickfall.Meta;
using Wickfall.Player;
using Wickfall.Presentation;
using Wickfall.World;

namespace Wickfall.UI
{
    /// <summary>
    /// PDF §4 Brazier: "Point de contrôle : sauvegarde, remplit la cire, boutique d'éclats." The menu
    /// opened by resting: the rest result (G08: one refill per rest), the shard shop (G19) and the
    /// flame loadout (G03). The world waits while it is open.
    /// </summary>
    public sealed class BrazierMenuView : MonoBehaviour
    {
        RectTransform root;
        TextMeshProUGUI shardsLabel, restLabel, info;
        readonly List<Button> shopButtons = new List<Button>();
        readonly List<(Button button, int slot)> slotButtons = new List<(Button, int)>();
        PlayerController player;
        Button leave;

        public static bool IsOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => IsOpen = false;

        void Start() => BrazierMenu.Open = Open;

        void OnDestroy()
        {
            if (BrazierMenu.Open == (System.Action<Brazier, PlayerController, bool>)Open)
                BrazierMenu.Open = null;
        }

        void Open(Brazier brazier, PlayerController who, bool refilled)
        {
            if (IsOpen || UiKit.Instance == null || who.IsBotControlled)
                return;
            player = who;
            IsOpen = true;
            player.ControlsLocked = true;
            Time.timeScale = 0f;
            Build(refilled);
            Events.PlayUi(SoundId.UiOpen);
        }

        void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            Time.timeScale = 1f;
            if (player != null)
                player.ControlsLocked = false;
            if (root != null)
                Destroy(root.gameObject);
            Events.PlayUi(SoundId.UiBack);
        }

        void Build(bool refilled)
        {
            var kit = UiKit.Instance;
            root = UiKit.Fill(transform, "BrazierMenu");
            kit.Veil(root, 0.55f);
            var panel = kit.Panel(root, "Panel", new Vector2(320f, 210f));
            kit.Label(panel, "Title", Loc.Get("brazier.title"), new Vector2(0f, 90f), new Vector2(300f, 14f), TextAlignmentOptions.Center, UiKit.Amber);
            restLabel = kit.Label(panel, "Rest", Loc.Get(refilled ? "brazier.rested" : "brazier.spent"), new Vector2(0f, 76f),
                new Vector2(300f, 14f), TextAlignmentOptions.Center, UiKit.BoneDim);
            shardsLabel = kit.Label(panel, "Shards", "", new Vector2(0f, 60f), new Vector2(300f, 14f), TextAlignmentOptions.Center);

            // Shop (left column)
            kit.Label(panel, "ShopTitle", Loc.Get("brazier.shop"), new Vector2(-78f, 42f), new Vector2(150f, 14f), TextAlignmentOptions.Center, UiKit.BoneDim);
            shopButtons.Clear();
            float y = 26f;
            if (PresentationMode.IsEnabled(Feature.Shop))
            {
                foreach (var item in AvailableItems())
                {
                    var captured = item;
                    var b = kit.Button(panel, item.id, ItemLabel(item), new Vector2(-78f, y), new Vector2(150f, 14f), () => Buy(captured));
                    b.GetComponentInChildren<TMP_Text>().fontSize = 12f;
                    shopButtons.Add(b);
                    y -= 15f;
                    if (shopButtons.Count >= 7)
                        break;
                }
            }

            // Loadout (right column)
            kit.Label(panel, "LoadoutTitle", Loc.Get("brazier.loadout"), new Vector2(78f, 42f), new Vector2(150f, 14f), TextAlignmentOptions.Center, UiKit.BoneDim);
            slotButtons.Clear();
            y = 26f;
            for (int i = 0; i < WickfallCounts.Flames; i++)
            {
                int slot = i;
                var b = kit.Button(panel, "Slot" + i, "", new Vector2(78f, y), new Vector2(150f, 14f), () => CycleSlot(slot));
                slotButtons.Add((b, slot));
                y -= 15f;
            }

            info = kit.Label(panel, "Info", "", new Vector2(0f, -72f), new Vector2(300f, 14f), TextAlignmentOptions.Center, UiKit.BoneDim);
            leave = kit.Button(panel, "Leave", Loc.Get("brazier.leave"), new Vector2(0f, -90f), new Vector2(120f, 14f), Close);
            Refresh();
            UiKit.Select(shopButtons.Count > 0 ? shopButtons[0] : leave);
        }

        IEnumerable<ShopItemSO> AvailableItems()
        {
            var save = GameSession.Instance.Save;
            foreach (var item in Db.Data.shop)
            {
                if (item == null)
                    continue;
                if ((item.kind == ShopItemKind.FlameDamage || item.kind == ShopItemKind.FlameArtCost) && !save.HasFlame(item.flame))
                    continue;
                yield return item;
            }
        }

        static int Owned(SaveData save, ShopItemSO item)
        {
            switch (item.kind)
            {
                case ShopItemKind.WaxCandle: return save.candles;
                case ShopItemKind.SootSalve: return save.salves;
                case ShopItemKind.FlameDamage: return save.flameDamageTier[(int)item.flame];
                default: return save.flameArtTier[(int)item.flame];
            }
        }

        static int Price(SaveData save, ShopItemSO item)
        {
            bool upgrade = item.kind == ShopItemKind.FlameDamage || item.kind == ShopItemKind.FlameArtCost;
            return upgrade ? item.price * (Owned(save, item) + 1) : item.price;
        }

        string ItemLabel(ShopItemSO item)
        {
            var save = GameSession.Instance.Save;
            int owned = Owned(save, item);
            string name = Loc.Get(item.nameKey);
            if (item.kind == ShopItemKind.FlameDamage || item.kind == ShopItemKind.FlameArtCost)
                name = Loc.Get(Db.Flame(item.flame).nameKey) + " · " + name + " " + (owned >= item.maxOwned ? "★" : new string('I', owned + 1));
            else
                name += " (" + owned + "/" + item.maxOwned + ")";
            return owned >= item.maxOwned ? name : name + "  " + Price(save, item);
        }

        void Buy(ShopItemSO item)
        {
            var session = GameSession.Instance;
            var save = session.Save;
            int owned = Owned(save, item);
            if (owned >= item.maxOwned || !session.SpendShards(Price(save, item)))
            {
                Events.PlayUi(SoundId.UiError);
                info.text = Loc.Get(owned >= item.maxOwned ? "brazier.full" : "brazier.poor");
                return;
            }
            switch (item.kind)
            {
                case ShopItemKind.WaxCandle: save.candles++; break;
                case ShopItemKind.SootSalve: save.salves++; break;
                case ShopItemKind.FlameDamage: save.flameDamageTier[(int)item.flame]++; break;
                default: save.flameArtTier[(int)item.flame]++; break;
            }
            session.ApplyProgress();
            session.SaveGame();
            Events.PlayUi(SoundId.Purchase);
            info.text = Loc.Get(item.descriptionKey);
            Refresh();
        }

        void CycleSlot(int slot)
        {
            var flames = player.Flames;
            if (slot >= flames.Slots)
            {
                Events.PlayUi(SoundId.UiError);
                info.text = Loc.Get("brazier.slotlocked");
                return;
            }
            // Options in order: empty, amber, blue, green, white, crimson. Pick the next valid one:
            // an unlocked flame not equipped elsewhere, or empty if another slot still holds a flame.
            int current = flames.Loadout[slot];
            for (int step = 1; step <= WickfallCounts.Flames + 1; step++)
            {
                int option = (current + 1 + step) % (WickfallCounts.Flames + 1) - 1; // -1 .. 4
                bool valid = option < 0
                    ? CountEquipped(flames) > 1
                    : flames.IsUnlocked((FlameId)option) && !EquippedElsewhere(flames, option, slot);
                if (valid)
                {
                    flames.Loadout[slot] = option;
                    break;
                }
            }
            var save = GameSession.Instance.Save;
            for (int i = 0; i < save.loadout.Length; i++)
                save.loadout[i] = flames.Loadout[i];
            if (!flames.IsEquipped(flames.Current))
                flames.ForceFlame((FlameId)FirstEquipped(flames));
            Events.ProgressChanged.Raise();
            Refresh();
        }

        static int CountEquipped(FlameSwitcher f)
        {
            int n = 0;
            for (int i = 0; i < f.Slots; i++) if (f.Loadout[i] >= 0) n++;
            return n;
        }

        static int FirstEquipped(FlameSwitcher f)
        {
            for (int i = 0; i < f.Slots; i++) if (f.Loadout[i] >= 0) return f.Loadout[i];
            return 0;
        }

        static bool EquippedElsewhere(FlameSwitcher f, int flame, int slot)
        {
            for (int i = 0; i < f.Slots; i++)
                if (i != slot && f.Loadout[i] == flame)
                    return true;
            return false;
        }

        void Refresh()
        {
            var save = GameSession.Instance.Save;
            shardsLabel.text = Loc.Format("brazier.shards", save.shards);
            int index = 0;
            foreach (var item in AvailableItems())
            {
                if (index >= shopButtons.Count) break;
                shopButtons[index].GetComponentInChildren<TMP_Text>().text = ItemLabel(item);
                index++;
            }
            var flames = player.Flames;
            foreach (var (button, slot) in slotButtons)
            {
                var label = button.GetComponentInChildren<TMP_Text>();
                if (slot >= flames.Slots)
                    label.text = (slot + 1) + ". " + Loc.Get("brazier.locked");
                else
                    label.text = (slot + 1) + ". " + (flames.Loadout[slot] >= 0 ? Loc.Get(Db.Flame((FlameId)flames.Loadout[slot]).nameKey) : "—");
                label.color = slot >= flames.Slots ? UiKit.BoneDim : UiKit.Bone;
            }
        }

        void Update()
        {
            if (IsOpen && GameInput.Instance.Pause.WasPressedThisFrame())
            {
                UiKit.PauseConsumedFrame = Time.frameCount;
                Close();
            }
        }
    }
}
