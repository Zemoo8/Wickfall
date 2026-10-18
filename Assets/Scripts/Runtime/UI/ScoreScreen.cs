using System.Collections;
using TMPro;
using UnityEngine;

namespace Wickfall.UI
{
    /// <summary>
    /// PDF §2.4 region score, shown line by line as the formula itself ("cire restante en % x 10,
    /// plus bonus de temps, plus 500 par relique, plus 20 par ennemi évité, moins 150 par mort"),
    /// then the rank: Cendre, Braise, Flamme or Brasier.
    /// </summary>
    public sealed class ScoreScreen : MonoBehaviour
    {
        public static ScoreScreen Instance { get; private set; }
        /// <summary>Tests and bots continue on their own after this many seconds (real players press a key).</summary>
        public static float AutoContinueSeconds = 12f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AutoContinueSeconds = 12f;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static IEnumerator ShowRoutine(RegionResult result)
        {
            if (Instance == null || UiKit.Instance == null)
                yield break;
            yield return Instance.Run(result);
        }

        IEnumerator Run(RegionResult r)
        {
            var kit = UiKit.Instance;
            var root = UiKit.Fill(transform, "Score");
            kit.Veil(root, 0.75f);
            var panel = kit.Panel(root, "Panel", new Vector2(300f, 190f));
            var config = Db.Score;
            var region = Db.Region(r.Region);
            kit.Label(panel, "Region", Loc.Get(region.nameKey), new Vector2(0f, 76f), new Vector2(280f, 14f), TextAlignmentOptions.Center, UiKit.Amber);
            kit.Label(panel, "Title", Loc.Get("score.title"), new Vector2(0f, 62f), new Vector2(280f, 14f), TextAlignmentOptions.Center, UiKit.BoneDim);

            int timeBonus = ScoreCalculator.TimeBonus(r.TimeSeconds, region.parTimeSeconds, config.pointsPerSecondUnderPar);
            string[] labels =
            {
                Loc.Format("score.wax", Mathf.RoundToInt(r.WaxPercent)),
                Loc.Format("score.time", FormatTime(r.TimeSeconds), FormatTime(region.parTimeSeconds)),
                Loc.Format("score.relics", r.Relics),
                Loc.Format("score.avoided", r.EnemiesAvoided),
                Loc.Format("score.deaths", r.Deaths),
            };
            int[] values =
            {
                Mathf.RoundToInt(Mathf.Clamp(r.WaxPercent, 0f, 100f) * config.waxPercentFactor),
                timeBonus,
                r.Relics * config.pointsPerRelic,
                r.EnemiesAvoided * config.pointsPerAvoidedEnemy,
                r.Deaths * config.pointsPerDeath,
            };

            float y = 40f;
            var shown = new TextMeshProUGUI[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                kit.Label(panel, "L" + i, labels[i], new Vector2(-30f, y), new Vector2(200f, 14f), TextAlignmentOptions.Left);
                shown[i] = kit.Label(panel, "V" + i, "", new Vector2(110f, y), new Vector2(60f, 14f), TextAlignmentOptions.Right);
                y -= 16f;
            }
            var total = kit.Label(panel, "Total", "", new Vector2(0f, y - 6f), new Vector2(280f, 14f), TextAlignmentOptions.Center, UiKit.Bone);
            var rank = kit.Label(panel, "Rank", "", new Vector2(0f, y - 30f), new Vector2(280f, 24f), TextAlignmentOptions.Center, UiKit.Amber, 24f);
            var hint = kit.Label(panel, "Hint", "", new Vector2(0f, -84f), new Vector2(280f, 14f), TextAlignmentOptions.Center, UiKit.BoneDim);

            float t = 0f;
            int running = 0;
            for (int i = 0; i < values.Length; i++)
            {
                yield return new WaitForSecondsRealtime(0.35f);
                if (root == null)
                    yield break; // the screen went away with its scene (a test ending, a scene change)
                shown[i].text = (values[i] >= 0 ? "+" : "") + values[i];
                running += values[i];
                total.text = Loc.Format("score.total", running);
                Events.PlayUi(SoundId.ScoreTally);
            }
            yield return new WaitForSecondsRealtime(0.5f);
            if (root == null)
                yield break;
            total.text = Loc.Format("score.total", r.Score);
            rank.text = Loc.Get("rank." + r.Rank.ToString().ToLowerInvariant());
            Events.PlayUi(SoundId.RankStamp);
            hint.text = Loc.Get("score.continue");

            while (t < AutoContinueSeconds)
            {
                t += Time.unscaledDeltaTime;
                var input = GameInput.Instance;
                if (input.Interact.WasPressedThisFrame() || input.Attack.WasPressedThisFrame() || input.Dodge.WasPressedThisFrame())
                    break;
                yield return null;
            }
            if (root == null)
                yield break;
            Events.PlayUi(SoundId.UiConfirm);
            Destroy(root.gameObject);
        }

        static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}
