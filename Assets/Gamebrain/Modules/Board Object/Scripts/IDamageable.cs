using UnityEngine;

namespace GameBrain.Casual
{
    public interface IDamageable
    {
        Transform Transform { get; }
        Health Health { get; }
        void TakeDamage(int damage);
    }
}
