using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Gameplay
{
    public class HammerAnimation : MonoBehaviour
    {
        /*[SerializeField] private GameObject HammerGameObject;
        [SerializeField] private GameObject ParticleGameObject;
        
        [Header("Camera Shake")]
        [SerializeField] private MMF_Player Feedback;

        #region Private Variable
        
        private static HammerAnimation _loadedPrefab;
        private Sequence _sequence;

        #endregion
        
        #region Public Functions

        public static HammerAnimation Create(Vector3 position,int count)
        {
            LoadPrefab();
            var obj = Instantiate(_loadedPrefab);
            
            obj.Init(position,count);
            return obj;
        }

        #endregion


        #region Private Functions

        [Button]
        private void Init(Vector3 position,int count)
        {
            HammerGameObject.SetActive(false);
            ParticleGameObject.SetActive(false);
            transform.position = position + new Vector3(0, 0, -2.25f);
            transform.position += (Vector3.up * count * .25f) + (Vector3.up * .75f);
            StartCoroutine(Animation());
        }

        private IEnumerator Animation()
        {
            HammerGameObject.SetActive(true);
            yield return new WaitForSeconds(1.05f);
            ParticleGameObject.SetActive(true);
            Feedback.PlayFeedbacks();
            
            var pos = transform.position;
            _sequence = DOTween.Sequence();
            _sequence.Append(transform.DOMove(new Vector3(pos.x, 4f, pos.z), 0.25f).OnComplete(()=>ParticleGameObject.SetActive(false)));
            _sequence.Append(transform.DOMove(new Vector3(-2.2f, 1.3f, -14f), 0.5f));
            _sequence.Join(transform.DOScale(0, 0.5f));
            _sequence.AppendCallback(()=> Destroy(gameObject));
            _sequence.Play();
        }

        private static void LoadPrefab()
        {
            if (_loadedPrefab == null)
            {
                _loadedPrefab = Resources.Load<HammerAnimation>("HammerAnimation");
            }
        }

        private void OnDisable() {
            _sequence?.Kill();
        }

        #endregion*/
    }
}