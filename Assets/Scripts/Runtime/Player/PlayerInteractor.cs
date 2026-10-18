using UnityEngine;
using Wickfall.World;

namespace Wickfall.Player
{
    /// <summary>Finds the nearest usable <see cref="Interactable"/> and shows its prompt (PDF action "interaction").</summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        static readonly Collider2D[] buffer = new Collider2D[16];
        ContactFilter2D filter;
        Interactable current;
        PlayerController player;
        float nextScan;

        public Interactable Current => current;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            filter.useLayerMask = true;
            filter.layerMask = LayerMask.GetMask(Layers.Interactable);
            filter.useTriggers = true;
        }

        void Update()
        {
            if (Time.time < nextScan)
                return;
            nextScan = Time.time + 0.08f;
            Scan();
        }

        void Scan()
        {
            Interactable best = null;
            float bestDistance = float.MaxValue;
            Vector2 position = transform.position;
            int count = Physics2D.OverlapCircle(position, Db.Balance.interactRadius, filter, buffer);
            for (int i = 0; i < count; i++)
            {
                var collider = buffer[i];
                buffer[i] = null;
                if (collider == null)
                    continue;
                var interactable = collider.GetComponentInParent<Interactable>();
                if (interactable == null || !interactable.CanInteract(player))
                    continue;
                float d = ((Vector2)interactable.transform.position - position).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = interactable;
                }
            }

            if (best != current)
            {
                current = best;
                Events.InteractPrompt.Raise(new InteractPromptInfo
                {
                    Visible = best != null,
                    TextKey = best != null ? best.promptKey : null,
                    WorldPosition = best != null ? (Vector2)best.transform.position + best.promptOffset : Vector2.zero,
                });
            }
        }

        public void Hide()
        {
            current = null;
            Events.InteractPrompt.Raise(new InteractPromptInfo { Visible = false });
        }

        public void Refresh()
        {
            current = null;
            nextScan = 0f;
        }
    }
}
