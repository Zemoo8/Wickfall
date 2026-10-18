using UnityEngine;
using Wickfall.Presentation;

namespace Wickfall.Player
{
    /// <summary>
    /// PDF §4 FlameSwitcher: "Choix de la flamme, coût en cire, éclat qui attire."
    /// Switching costs 8 wax and makes a flare that draws enemies (PDF §2.1). The loadout holds
    /// up to 4 flames (5 with the flame-slot relic), chosen at braziers (G03).
    /// </summary>
    public sealed class FlameSwitcher : MonoBehaviour
    {
        public FlameId Current { get; private set; } = FlameId.Amber;
        public FlameTypeSO CurrentType => Db.Flame(Current);
        public int UnlockedMask { get; private set; } = 1;
        public int Slots { get; private set; } = 4;

        /// <summary>Equipped flame per slot, or -1 for an empty slot.</summary>
        public readonly int[] Loadout = { 0, -1, -1, -1, -1 };

        WaxSystem wax;

        void Awake() => wax = GetComponent<WaxSystem>();

        void Start() => Announce(Current, Current);

        public bool IsUnlocked(FlameId id) => (UnlockedMask & (1 << (int)id)) != 0;

        public bool IsEquipped(FlameId id)
        {
            for (int i = 0; i < Slots; i++)
                if (Loadout[i] == (int)id)
                    return true;
            return false;
        }

        public void Configure(int unlockedMask, int[] loadout, int slots, FlameId active)
        {
            UnlockedMask = unlockedMask | 1;
            Slots = Mathf.Clamp(slots, 1, WickfallCounts.Flames);
            for (int i = 0; i < Loadout.Length; i++)
            {
                int value = loadout != null && i < loadout.Length ? loadout[i] : -1;
                Loadout[i] = i < Slots && value >= 0 && IsUnlocked((FlameId)value) ? value : -1;
            }
            if (!IsEquipped(FlameId.Amber) && !HasAnyEquipped())
                Loadout[0] = 0;
            FlameId previous = Current;
            Current = IsUnlocked(active) ? active : FlameId.Amber;
            Announce(previous, Current);
        }

        bool HasAnyEquipped()
        {
            for (int i = 0; i < Slots; i++)
                if (Loadout[i] >= 0) return true;
            return false;
        }

        /// <summary>Unlocks a flame and puts it in the first free slot (G03: flames are found in regions).</summary>
        public void Unlock(FlameId id)
        {
            UnlockedMask |= 1 << (int)id;
            if (IsEquipped(id))
                return;
            for (int i = 0; i < Slots; i++)
            {
                if (Loadout[i] < 0)
                {
                    Loadout[i] = (int)id;
                    break;
                }
            }
            Events.ProgressChanged.Raise();
        }

        public void UnlockAll()
        {
            for (int i = 0; i < WickfallCounts.Flames; i++)
                Unlock((FlameId)i);
        }

        public bool TrySelectSlot(int slot)
        {
            if (slot < 0 || slot >= Slots || Loadout[slot] < 0)
                return false;
            return TrySwitch((FlameId)Loadout[slot]);
        }

        public bool TryCycle(int direction)
        {
            int slot = SlotOf(Current);
            for (int step = 1; step <= Slots; step++)
            {
                int next = ((slot + direction * step) % Slots + Slots) % Slots;
                if (Loadout[next] >= 0 && Loadout[next] != (int)Current)
                    return TrySwitch((FlameId)Loadout[next]);
            }
            return false;
        }

        public int SlotOf(FlameId id)
        {
            for (int i = 0; i < Slots; i++)
                if (Loadout[i] == (int)id)
                    return i;
            return 0;
        }

        /// <summary>PDF: "Changer de flamme coûte 8 de cire et provoque un éclat qui attire les ennemis."</summary>
        public bool TrySwitch(FlameId to)
        {
            if (to == Current || !IsUnlocked(to) || !PresentationMode.IsEnabled(Feature.FlameSwitching))
                return false;
            if (wax == null || !wax.TrySpend(Db.Wax.flameSwitchCost))
            {
                Events.PlayUi(SoundId.UiError);
                return false;
            }

            FlameId from = Current;
            Current = to;
            Announce(from, to);

            Vector2 position = transform.position;
            var flame = Db.Flame(to);
            Events.Flare.Raise(new FlareInfo(position, Db.Balance.flareRadius, FlareKind.FlameSwitch, flame.lightColor));
            Events.PlayerActed.Raise(new PlayerActionInfo
            {
                Kind = PlayerActionKind.SwitchFlame, Flame = to, Position = position, Time = Time.time,
            });
            Events.Play(SoundId.FlameSwitch, position);
            Events.Play(SoundId.Flare, position, 0.8f);
            return true;
        }

        /// <summary>Sets the flame without cost (respawn, loading a save, the boss intro).</summary>
        public void ForceFlame(FlameId id)
        {
            FlameId from = Current;
            Current = id;
            Announce(from, id);
        }

        void Announce(FlameId from, FlameId to)
        {
            if (wax != null)
                wax.SetFlame(to);
            Events.FlameSwitched.Raise(new FlameSwitch { From = from, To = to, Position = transform.position });
        }
    }
}
