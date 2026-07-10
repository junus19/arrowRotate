using UnityEngine;

namespace GameBrain.Casual
{
    // [RequireComponent(typeof(DamageableModule))]
    public abstract class DamageableBoardObject : BoardObject, IDamageable
    {
        protected bool _canTakeDamage = false;
        public bool CanTakeDamage => _canTakeDamage;
        public Transform Transform => transform;

        protected Health health;
        public Health Health => health;

        public bool isMarked = false;

        public void TryMark()
        {
            if (health.Current == 1)
                isMarked = true;
        }

        public override void LoadBoardObject(ICell cell, int _health)
        {
            base.LoadBoardObject(cell, _health);
            health = new Health(_health);
            // OnBoardObjectUnloaded.AddListener(cell.UnLoadBoardObject);
            _canTakeDamage = true;
            SetVisual();
        }

        public abstract void TakeDamage(int damage);

        protected virtual void SetVisual()
        {
        }

        public virtual void HardDestroy()
        {
        }

        #region Editor

        [ContextMenu("Give Damage")]
        private void GiveDamage() => TakeDamage(1);

        #endregion
    }
}
