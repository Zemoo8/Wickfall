using TMPro;
using UnityEngine;
using Wickfall.Presentation;

namespace Wickfall.UI
{
    /// <summary>
    /// Small numbers that rise from hits ("BLOQUÉ" when a flame cannot hurt the target - the PDF's
    /// flame-versus-matter rules made readable). Pooled world-space texts; strings are cached.
    /// </summary>
    public sealed class DamagePopups : MonoBehaviour
    {
        public TextMeshPro prefab;
        public int poolSize = 24;
        public float riseSpeed = 1.2f;
        public float lifetime = 0.7f;

        TextMeshPro[] texts;
        float[] ages;
        int next;
        static readonly string[] numbers = new string[200];

        void Awake()
        {
            texts = new TextMeshPro[poolSize];
            ages = new float[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                if (prefab == null)
                    break;
                texts[i] = Instantiate(prefab, transform);
                texts[i].gameObject.SetActive(false);
                ages[i] = lifetime;
            }
            for (int i = 0; i < numbers.Length; i++)
                if (numbers[i] == null)
                    numbers[i] = i.ToString();
        }

        void OnEnable() => Events.EnemyHit.Register(OnHit);
        void OnDisable() => Events.EnemyHit.Unregister(OnHit);

        void OnHit(HitReport report)
        {
            if (prefab == null || !PresentationMode.IsEnabled(Feature.VFX))
                return;
            var text = texts[next];
            ages[next] = 0f;
            next = (next + 1) % texts.Length;
            text.gameObject.SetActive(true);
            text.transform.position = report.Position + new Vector2(Random.Range(-0.3f, 0.3f), 1.1f);
            if (report.Blocked)
            {
                text.text = Loc.Get("hud.blocked");
                text.color = new Color(0.64f, 0.6f, 0.55f);
            }
            else
            {
                int value = Mathf.Clamp(Mathf.RoundToInt(report.Amount), 0, numbers.Length - 1);
                text.text = numbers[value];
                text.color = report.Killed ? new Color(1f, 0.76f, 0.3f) : new Color(0.93f, 0.9f, 0.83f);
            }
        }

        void Update()
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null || ages[i] >= lifetime)
                    continue;
                ages[i] += Time.deltaTime;
                var t = texts[i];
                t.transform.position += new Vector3(0f, riseSpeed * Time.deltaTime, 0f);
                var c = t.color;
                c.a = 1f - Mathf.Clamp01((ages[i] - lifetime * 0.5f) / (lifetime * 0.5f));
                t.color = c;
                if (ages[i] >= lifetime)
                    t.gameObject.SetActive(false);
            }
        }
    }
}
