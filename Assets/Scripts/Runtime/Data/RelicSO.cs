using UnityEngine;

namespace Wickfall
{
    public enum RelicEffect
    {
        ExtraDodge,   // PDF: "esquive en plus"
        WallGrip,     // PDF: "accroche au mur"
        MaxWax,       // PDF: "cire maximale"
        FlameSlot,    // PDF: "emplacement de flamme en plus"
        SpareWick,    // proposal (G04): relight once per rest instead of going out
        HearthEmber,  // proposal (G04): flame arts cost less wax
    }

    /// <summary>PDF §2.4 "6 Reliques de Braise (une par région)". Four effects are the PDF's, two are proposals (G04).</summary>
    [CreateAssetMenu(menuName = "WICKFALL/Relic", fileName = "Relic")]
    public sealed class RelicSO : ScriptableObject
    {
        [Range(0, 5)] public int index;
        public RegionId region;
        public RelicEffect effect;
        public float value = 1f;
        public string nameKey = "relic.0";
        public string descriptionKey = "relic.0.desc";
        [Tooltip("True when the effect comes from the PDF's own examples.")]
        public bool fromPdf = true;
        public Color glow = new Color(1f, 0.757f, 0.302f);
    }
}
