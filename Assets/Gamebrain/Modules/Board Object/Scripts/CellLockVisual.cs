using UnityEngine;
using DG.Tweening;
using GameBrain.Utils;
using GameBrain.Casual;
using TMPro;

namespace GameBrain.Casual
{
    public class CellLockVisual : MonoBehaviour
    {
        [SerializeField] SpriteRenderer lockVisualSpriteRenderer;
        [SerializeField] TextMeshPro txt;

        public void DestroyAnim()
        {
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.UnLockCell));    
            txt.text="";
            transform.SetParent(null);
            lockVisualSpriteRenderer.gameObject.SetActive(true);
            transform.DOScale(Vector3.one * 1.1f, 0.5f).SetDelay(0.2f);
            lockVisualSpriteRenderer.DOFade(0, 0.3f).SetDelay(0.45f);
            transform.DOShakeRotation(.8f, Vector3.forward * 10.0f, 20, 45, true).OnComplete(OnAnimComplete);
        }

        private void OnAnimComplete()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}
