using UnityEngine;

namespace Wickfall.Combat
{
    /// <summary>Put on a collider so hit queries can find its owner without allocating.</summary>
    public sealed class Hurtbox : MonoBehaviour
    {
        public MonoBehaviour owner;
        IDamageable cached;

        public IDamageable Owner
        {
            get
            {
                if (cached == null && owner != null)
                    cached = owner as IDamageable;
                return cached;
            }
        }

        public void Bind(IDamageable damageable)
        {
            cached = damageable;
            owner = damageable as MonoBehaviour;
        }
    }
}
