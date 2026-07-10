using System;
using System.Collections.Generic;
using UnityEngine;
//using DG.Tweening;
//using MoreMountains.Tools;
//using UM.Events;

namespace Gameplay
{
    public class FireCrackerHex : MonoBehaviour
    {
        /*
        [Header("Rocket Positions")]
        [SerializeField] private Transform[] RocketTransformsArray;
        
        [Header("Rocket Shadows")]
        [SerializeField] private Transform[] RocketShadowArray;
        
        [Header("Prefab Ref")]
        [SerializeField] private FireCrackerHexRocket FireCrackerHexRocket;
        [SerializeField] private GameEvent ReValidateSorting;
        
        [Header("Launch Position")] 
        [SerializeField] private Transform BasketTransform;
        [SerializeField] private Transform LaunchPositionTransform;

        [Header("Objects")] 
        [SerializeField] private GameplaySounds GameplaySounds;

        private List<FireCrackerHexRocket> _rockets;
        private int _cost;

        [SerializeField] private GameObject FireCrackerExplosionPrefab;
        [SerializeField] private GameObject FireCrackerSmokePrefab;
        private static bool _onGameplay = true;
        private Feedbacks _feedbacksInstance;
        private Sequence _animationSequence;
        
        public static bool OnGameplay
        {
            set
            {
                _onGameplay = value;
            }
        }

        private Feedbacks Feedbacks => _feedbacksInstance ??= Feedbacks.Instance;
        
        #region Mono Methods

        private void OnDisable()
        {
            _animationSequence?.Kill();
            Feedbacks.FireCrackerExplosion.StopSounds();
            Feedbacks.FireCrackerLaunch.StopSounds();
            Feedbacks.FireCrackerBoxRemove.StopSounds();
        }

        #endregion
        
        public void Init(int cost)
        {
            _cost = cost;
            SpawnRockets();
        }

        public void Animate(Transform cellTransform,Vector3 targetPosition,float initialDelay,Action onComplete)
        {
            if(_cost == 0) return;
            _cost -= 1;


            DOVirtual.DelayedCall(initialDelay, () =>
            {
                if (RocketShadowArray != null && _cost < RocketShadowArray.Length && _cost >= 0 &&
                    RocketShadowArray[_cost] != null)
                {
                    RocketShadowArray[_cost].gameObject.SetActive(false);
                }

                if (_rockets != null && _cost < _rockets.Count && _cost >= 0 && _rockets[_cost] != null)
                {
                    _rockets[_cost].Animate(cellTransform, targetPosition, LaunchPositionTransform, () =>
                    {
                        onComplete?.Invoke();
                        ReValidateSorting.Invoke();
                        OnRocketExplode(targetPosition);
                    });
                    SpawnSmoke();
                    transform.DOShakePosition(0.6f, 0.2f, 30);
                }

                if (_cost == 0)
                    Completed(0.7f);
            });
        }

        public void Completed(float initialDelay)
        {
            DisappearAnimation(transform, initialDelay); 
        }

        public void OnRotationUpdate(Vector3 rotation)
        {
            rotation.x = 0;
            transform.rotation = Quaternion.Euler(rotation);
        }

        private void SpawnRockets()
        {
            _rockets = new List<FireCrackerHexRocket>();
            for (int i = 0; i < _cost; i++)
            {
                var rocket = Instantiate(FireCrackerHexRocket, RocketTransformsArray[i]);
                rocket.Init();
                _rockets.Add(rocket);
                RocketShadowArray[i].gameObject.SetActive(true);
            }
        }
        
        private void DisappearAnimation(Transform transform, float initialDelay)
        {
            _animationSequence?.Kill();
            _animationSequence = DOTween.Sequence();
            _animationSequence.AppendInterval(initialDelay)
                .Append(BasketTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                    .OnComplete(()=> GameplaySounds.PlayFireCrackerBoxRemoveSound()))
                .AppendInterval(4.1f)
                .OnComplete(() =>
                {
                    Destroy(transform.gameObject);
                });
        }
        
        private void OnRocketExplode(Vector3 position)
        {
            SpawnExplosion(position);
        }
        
        private void SpawnExplosion(Vector3 position)
        {
            var pos = position;
            pos.y += 1f;
            var particle = Instantiate(FireCrackerExplosionPrefab, pos, Quaternion.identity);

            particle.transform.ChangeLayersRecursively(!_onGameplay ? "GameplayFtue" : "Gameplay");

            Destroy(particle, 0.75f);
        }

        private void SpawnSmoke()
        {
            Quaternion angle = Quaternion.Euler(-90f, 0f, 0f);
            Vector3 pos = LaunchPositionTransform.position;
            pos.y = -0.53f;
            var particle = Instantiate(FireCrackerSmokePrefab, pos, angle);
            
            particle.transform.ChangeLayersRecursively(!_onGameplay ? "GameplayFtue" : "Gameplay");
            
            Destroy(particle, 0.75f);
        }*/
    }
}