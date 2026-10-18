using System.Collections;
using UnityEngine;
using Wickfall.Meta;
using Wickfall.Presentation;
using Wickfall.UI;

namespace Wickfall.World
{
    /// <summary>
    /// Runs one region: its navigation grid, its completion (PDF §2.4 region score and rank, saved as
    /// the best for replays), and the way on to the next region.
    /// </summary>
    [DefaultExecutionOrder(-600)]
    public sealed class RegionDirector : MonoBehaviour
    {
        public RegionId region;
        public TextAsset levelFile;

        public static RegionDirector Instance { get; private set; }
        public LevelData Level { get; private set; }
        public NavGrid Nav { get; private set; }
        public bool Completed { get; private set; }
        public RegionResult LastResult { get; private set; }

        void Awake()
        {
            Instance = this;
            if (levelFile != null)
            {
                Level = LevelData.Parse(levelFile.text);
                Nav = new NavGrid(Level);
                Nav.MakeActive();
            }
        }

        void Start()
        {
            if (RoomLoader.Instance != null)
                RoomLoader.Instance.GroupLoaded += OnGroupLoaded;
        }

        void OnDestroy()
        {
            if (RoomLoader.Instance != null)
                RoomLoader.Instance.GroupLoaded -= OnGroupLoaded;
            if (Instance == this)
                Instance = null;
            Nav?.Clear();
        }

        void OnGroupLoaded(RectInt cells)
        {
            if (Nav != null && isActiveAndEnabled)
                StartCoroutine(BlockPropsNextFrame(cells));
        }

        /// <summary>
        /// Props with solid bodies (bell posts, braziers, inscriptions, machines...) make the cells they
        /// stand in unwalkable, so bots and creatures path around them instead of into them.
        /// </summary>
        IEnumerator BlockPropsNextFrame(RectInt cells)
        {
            yield return null; // the group's objects have run Awake/OnEnable and are in the physics scene
            Physics2D.SyncTransforms();
            Nav.BlockSolidProps(cells, LayerMask.GetMask(Layers.Wall)); // not Breakable: broken props free their cell
        }

        /// <summary>Called by the exit: score, save, then on to the next region.</summary>
        public void Complete()
        {
            if (Completed)
                return;
            Completed = true;
            StartCoroutine(CompleteRoutine());
        }

        IEnumerator CompleteRoutine()
        {
            var session = GameSession.Instance;
            var player = session != null ? session.player : null;
            float waxPercent = player != null ? 100f * player.Wax.Current / Mathf.Max(1f, player.Wax.Max) : 0f;
            var data = Db.Region(region);
            var result = session != null
                ? session.Tracker.Finish(waxPercent, data.parTimeSeconds, Db.Score)
                : default;
            LastResult = result;
            if (player != null)
            {
                player.ControlsLocked = true;
                player.Wax.Ticking = false;
            }

            if (session != null)
            {
                var record = session.Save.regions[(int)region];
                record.cleared = true;
                if (result.Score > record.bestScore)
                {
                    record.bestScore = result.Score;
                    record.bestRank = (int)result.Rank;
                }
                RegionId next = region + 1;
                if ((int)next < WickfallCounts.Regions)
                {
                    session.Save.region = (int)next;
                    session.Save.position = Vector2.zero;
                    session.Save.brazierId = "";
                    session.Save.spentBrazierId = "";
                }
                session.SaveGame();
            }
            Events.RegionCompleted.Raise(result);

            if (PresentationMode.IsEnabled(Feature.Score))
                yield return ScoreScreen.ShowRoutine(result);

            if (player != null)
            {
                player.ControlsLocked = false;
                player.Wax.Ticking = true;
            }
            GoOn();
        }

        void GoOn()
        {
            RegionId next = region + 1;
            bool hasNext = (int)next < WickfallCounts.Regions && PresentationMode.IsRegionEnabled(next)
                           && Application.CanStreamedLevelBeLoaded(SceneNames.RegionBase(next));
            SceneLoader.Instance?.Load(hasNext ? SceneNames.RegionBase(next) : SceneNames.MainMenu);
        }
    }
}
