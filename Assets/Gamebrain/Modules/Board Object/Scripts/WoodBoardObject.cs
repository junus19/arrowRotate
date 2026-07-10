using UnityEngine;
using DG.Tweening;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class WoodBoardObject : DamageableBoardObject
    {
        [Header("Wood")]
        [SerializeField] GameObject woodParent;

        [SerializeField] GameObject wood_1X;
        [SerializeField] GameObject wood_1X_Particle;

        [SerializeField] GameObject wood_2X;
        [SerializeField] GameObject wood_2X_Particle;

        [SerializeField] GameObject wood_3X;
        [SerializeField] GameObject wood_3X_Particle;

        public override void TakeDamage(int damage)
        {
            if (!CanTakeDamage) return;
            
            health.Decrease(damage);

            if (health.Current == 2)
            {
                wood_3X.gameObject.SetActive(false);
                wood_3X_Particle.gameObject.SetActive(true);

                ScaleAnim(wood_2X.transform);
                ScaleAnim(wood_1X.transform);
                EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.WoodHit_1));

            }
            else if (health.Current == 1)
            {
                wood_2X.gameObject.SetActive(false);
                wood_2X_Particle.gameObject.SetActive(true);
                ScaleAnim(wood_1X.transform);
                EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.WoodHit_2));
            }
            else if (health.Current == 0)
            {
                wood_1X.gameObject.SetActive(false);
                wood_1X_Particle.gameObject.SetActive(true);

                GetComponent<Collider>().enabled = false;
                EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.WoodHit_3));

                UnloadAndDestroyBoardObject();
            }
        }

        protected override void SetVisual()
        {
            woodParent.SetActive(true);

            if (health.Current == 3)
            {
                wood_3X.gameObject.SetActive(true);
                wood_2X.gameObject.SetActive(true);
                wood_1X.gameObject.SetActive(true);

                ScaleAnim(wood_3X.transform);
                ScaleAnim(wood_2X.transform);
                ScaleAnim(wood_1X.transform);
            }
            else if (health.Current == 2)
            {
                wood_2X.gameObject.SetActive(true);
                wood_1X.gameObject.SetActive(true);

                ScaleAnim(wood_2X.transform);
                ScaleAnim(wood_1X.transform);

            }
            else if (health.Current <= 1)
            {
                wood_1X.gameObject.SetActive(true);
                ScaleAnim(wood_1X.transform);
            }

            woodParent.SetActive(true);
        }

        private void ScaleAnim(Transform target, float scaleAmount = 1.2f)
        {
            target.localScale = Vector3.one;
            target.DOScale(Vector3.one * scaleAmount, 0.25f).SetLoops(2, LoopType.Yoyo);
        }

        public override void HardDestroy()
        {
            wood_3X.gameObject.SetActive(false);
            wood_2X.gameObject.SetActive(false);
            wood_1X.gameObject.SetActive(false);
        }

        public override void UnloadAndDestroyBoardObject()
        {
            EventBus<BoardObjectBrokenEvent>.Raise(new BoardObjectBrokenEvent(BoardObjectType.Wood, transform.position));
            currentCell.UnLoadBoardObject();
            HardDestroy();
            // OnBoardObjectUnloaded.Invoke();
        }
    }
}
