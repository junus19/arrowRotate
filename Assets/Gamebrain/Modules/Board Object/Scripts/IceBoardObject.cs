using UnityEngine;
using DG.Tweening;
using GameBrain.Utils;
using GameBrain.Casual;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public class IceBoardObject : DamageableBoardObject
    {
        [Header("Ice")]
        [SerializeField] GameObject iceParent;
        [SerializeField] GameObject Ice_Lvl01;
        [SerializeField] GameObject Ice_Broken_Lvl01;
        [SerializeField] GameObject Ice_Lvl02;
        [SerializeField] GameObject Ice_Broken_Lvl02;
        [SerializeField] GameObject Ice_Lvl03;
        [SerializeField] GameObject Ice_Broken_Lvl03;

        [SerializeField] IceTokenVisual iceTokenPrefab;
        [SerializeField] Transform iceTokenParent;
        [SerializeField] List<IceTokenVisual> iceTokens;

        public override void TakeDamage(int damage)
        {
            if (!CanTakeDamage) return;
            health.Decrease(damage);
        
            UpdateICeTokens(health.Current);
            
            if (health.Current == 2)
            {
                // Ice_Lvl01.SetActive(false);
                // Ice_Broken_Lvl01.SetActive(true);
                // Ice_Lvl02.SetActive(true);
                EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Ice_1));
            }
            else if (health.Current == 1)
            {
                // Ice_Lvl02.SetActive(false);
                // Ice_Broken_Lvl02.SetActive(true);
                // Ice_Lvl03.SetActive(true);
                EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Ice_2));
            }
            else if (health.Current == 0)
            {
                // Ice_Lvl03.SetActive(false);
                // Ice_Broken_Lvl03.SetActive(true);
                GetComponent<Collider>().enabled = false;
                EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Ice_3));
                UnloadAndDestroyBoardObject();
            }
        }

        private void UpdateICeTokens(int health)
        {
            for(int i = 0; i < iceTokens.Count; i++)
            {
                iceTokens[i].UpdateVisual(health, i == iceTokens.Count - 1);
            }
        }

        protected override void SetVisual()
        {
            int tokenCount = currentCell.GetTotalTokenCount();
            // transform.localEulerAngles = new Vector3(0, 60 * Random.Range(0, 6), 0);
            // Ice_Lvl01.SetActive(true);
            // Ice_Lvl02.SetActive(false);
            // Ice_Lvl03.SetActive(false);

            for(int i = 0; i < tokenCount; i++)
            {
                var iceToken = Instantiate(iceTokenPrefab, iceTokenParent);
                iceToken.transform.localRotation = Quaternion.identity;

                iceToken.transform.localPosition = Vector3.up * .4f * i;
                iceTokens.Add(iceToken);
            }
        }

        private void ScaleAnim(Transform target, float scaleAmount = 1.2f)
        {
            target.localScale = Vector3.one;
            target.DOScale(Vector3.one * scaleAmount, 0.25f).SetLoops(2, LoopType.Yoyo);
        }

        public override void UnloadAndDestroyBoardObject()
        {
            EventBus<BoardObjectBrokenEvent>.Raise(new BoardObjectBrokenEvent(BoardObjectType.Ice, transform.position));
            currentCell.UnLoadBoardObject();
            iceParent.transform.SetParent(null);
            //iceParent.SetActive(false);
            // OnBoardObjectUnloaded.Invoke();
        }
    }
}
