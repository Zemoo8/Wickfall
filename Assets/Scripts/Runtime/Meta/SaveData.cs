using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wickfall.Meta
{
    [Serializable]
    public sealed class RegionRecord
    {
        public bool cleared;
        public int bestScore = -1;
        public int bestRank = -1;
    }

    /// <summary>
    /// PDF §4 SaveSystem: "Sauvegarde chiffrée de la position, de la cire, des reliques, des pactes et
    /// des fins débloquées". Plus what the rest of the game needs to resume (G08, G10, G19).
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public long savedAtUtcTicks;
        public float playTimeSeconds;

        [Header("Position (PDF)")]
        public int region;              // RegionId
        public string brazierId = "";   // the last brazier rested at (respawn point)
        public Vector2 position;

        [Header("Wax (PDF)")]
        public float wax = 100f;
        [Tooltip("G08: the brazier whose refill is spent until you die or rest elsewhere.")]
        public string spentBrazierId = "";

        [Header("Relics, pacts, endings (PDF)")]
        public int relicsMask;
        public int pactsMask;
        public int endingsMask;
        public bool kingSpared;
        public bool kingDecided;

        [Header("Flames (G03)")]
        public int flamesUnlockedMask = 1; // amber
        public int activeFlame;
        public int[] loadout = { 0, -1, -1, -1, -1 };

        [Header("Economy (G19)")]
        public int shards;
        public int candles;
        public int salves;
        public int[] flameDamageTier = new int[WickfallCounts.Flames];
        public int[] flameArtTier = new int[WickfallCounts.Flames];

        [Header("World state")]
        public List<string> worldFlags = new List<string>();
        public RegionRecord[] regions = NewRecords();

        static RegionRecord[] NewRecords()
        {
            var records = new RegionRecord[WickfallCounts.Regions];
            for (int i = 0; i < records.Length; i++)
                records[i] = new RegionRecord();
            return records;
        }

        public static SaveData NewGame()
        {
            var data = new SaveData();
            data.region = (int)RegionId.TallowFields;
            return data;
        }

        /// <summary>Fixes arrays from older or damaged saves so the game never reads past an end.</summary>
        public void Sanitize()
        {
            if (loadout == null || loadout.Length != WickfallCounts.Flames)
                loadout = new[] { 0, -1, -1, -1, -1 };
            if (flameDamageTier == null || flameDamageTier.Length != WickfallCounts.Flames)
                flameDamageTier = new int[WickfallCounts.Flames];
            if (flameArtTier == null || flameArtTier.Length != WickfallCounts.Flames)
                flameArtTier = new int[WickfallCounts.Flames];
            if (worldFlags == null)
                worldFlags = new List<string>();
            if (regions == null || regions.Length != WickfallCounts.Regions)
                regions = NewRecords();
            for (int i = 0; i < regions.Length; i++)
                if (regions[i] == null)
                    regions[i] = new RegionRecord();
            if ((flamesUnlockedMask & 1) == 0)
                flamesUnlockedMask |= 1;
            region = Mathf.Clamp(region, 0, WickfallCounts.Regions - 1);
            activeFlame = Mathf.Clamp(activeFlame, 0, WickfallCounts.Flames - 1);
            if (brazierId == null) brazierId = "";
            if (spentBrazierId == null) spentBrazierId = "";
        }

        public bool HasRelic(int index) => (relicsMask & (1 << index)) != 0;
        public int RelicCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < WickfallCounts.Relics; i++)
                    if (HasRelic(i)) n++;
                return n;
            }
        }

        public bool HasFlame(FlameId id) => (flamesUnlockedMask & (1 << (int)id)) != 0;
        public bool HasPact(int index) => (pactsMask & (1 << index)) != 0;
        public bool HasEnding(EndingId id) => id != EndingId.None && (endingsMask & (1 << (int)id)) != 0;
    }

    /// <summary>Player settings, saved unencrypted and separately from the game (prompt §8).</summary>
    [Serializable]
    public sealed class SettingsData
    {
        public string language = "";   // "" = not chosen yet: follow the system language
        public float masterVolume = 0.9f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.9f;
        public float ambienceVolume = 0.8f;
        public float uiVolume = 0.8f;
        public int displayMode;        // 0 fullscreen, 1 borderless, 2 windowed
        public int resolutionWidth;
        public int resolutionHeight;
        public bool vsync = true;
        public int frameCap = 60;
        public bool pixelPerfect = true; // false = fill screen
        public bool trimToggle;
        public int movementMode;       // MovementMode
        public float screenShake = 1f;
        public float flashIntensity = 1f;
        public int textScale = 1;      // 1 normal, 2 large
        public string bindingOverrides = "";
    }
}
