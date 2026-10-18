using UnityEngine;
using Wickfall.Meta;

namespace Wickfall.Enemies
{
    /// <summary>
    /// PDF §4 EnemyPerception: "Calcule le rayon de détection depuis la lumière du joueur et garde la
    /// dernière position connue." The detection radius comes from the LightChanged channel
    /// (2 + radius x 1.5, x0.4 trimmed, halved in dark zones); walls and the canopy block sight; flares
    /// and kills within range make the creature come and look (PDF §2.1, §2.3).
    /// </summary>
    public sealed class EnemyPerception : MonoBehaviour
    {
        public float checkInterval = 0.1f;
        public float perceptionScale = 1f;
        public Vector2 eyeOffset = new Vector2(0f, 0.5f);

        static readonly RaycastHit2D[] hits = new RaycastHit2D[4];
        ContactFilter2D sightFilter;
        float nextCheck;
        bool hasInvestigate;
        Vector2 investigatePoint;

        public bool CanSeePlayer { get; private set; }
        public bool InDetectionRange { get; private set; }
        public bool HasLastKnown { get; private set; }
        public Vector2 LastKnown { get; private set; }
        public float TimeSinceSeen { get; private set; } = 999f;
        public Vector2 PlayerPosition { get; private set; }
        public float DistanceToPlayer { get; private set; } = 999f;
        public float DetectionRadius { get; private set; }
        public bool PlayerShining { get; private set; }
        public bool Blind { get; set; }

        void Awake()
        {
            sightFilter.useLayerMask = true;
            sightFilter.layerMask = Layers.SightMask;
            sightFilter.useTriggers = false;
            nextCheck = Time.time + Random.value * checkInterval;
        }

        void OnEnable() => Events.Flare.Register(OnFlare);
        void OnDisable() => Events.Flare.Unregister(OnFlare);

        void OnFlare(FlareInfo flare)
        {
            if (Blind)
                return;
            if (((Vector2)transform.position - flare.Position).sqrMagnitude <= flare.Radius * flare.Radius)
            {
                hasInvestigate = true;
                investigatePoint = flare.Position;
            }
        }

        /// <summary>Returns true once per flare/noise heard, with where it came from.</summary>
        public bool ConsumeInvestigate(out Vector2 point)
        {
            point = investigatePoint;
            if (!hasInvestigate)
                return false;
            hasInvestigate = false;
            return true;
        }

        /// <summary>Something told this creature where the player is (it was hit, a bell rang).</summary>
        public void Reveal(Vector2 point)
        {
            LastKnown = point;
            HasLastKnown = true;
            TimeSinceSeen = 0f;
        }

        public void Forget()
        {
            HasLastKnown = false;
            hasInvestigate = false;
            TimeSinceSeen = 999f;
        }

        void Update()
        {
            TimeSinceSeen += Time.deltaTime;
            if (Time.time < nextCheck)
                return;
            nextCheck = Time.time + checkInterval;
            Check();
        }

        public void Check()
        {
            var session = GameSession.Instance;
            var player = session != null ? session.player : null;
            if (Blind || player == null || !player.isActiveAndEnabled || player.IsExtinguished || !Events.LightChanged.HasLast)
            {
                CanSeePlayer = false;
                InDetectionRange = false;
                return;
            }

            var light = Events.LightChanged.Last;
            PlayerPosition = light.Position;
            PlayerShining = !light.Trimmed;
            Vector2 eye = (Vector2)transform.position + eyeOffset;
            DistanceToPlayer = Vector2.Distance(eye, PlayerPosition);
            DetectionRadius = light.DetectionRadius * perceptionScale;
            InDetectionRange = DistanceToPlayer <= DetectionRadius;
            CanSeePlayer = InDetectionRange && LineOfSight(eye, PlayerPosition + new Vector2(0f, 0.6f));
            if (CanSeePlayer)
            {
                LastKnown = PlayerPosition;
                HasLastKnown = true;
                TimeSinceSeen = 0f;
            }
        }

        public bool LineOfSight(Vector2 from, Vector2 to)
        {
            int count = Physics2D.Linecast(from, to, sightFilter, hits);
            for (int i = 0; i < count; i++)
                hits[i] = default;
            return count == 0;
        }
    }
}
