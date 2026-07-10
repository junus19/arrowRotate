using System;
using Gameplay;
using UnityEngine;
using DG.Tweening;
using GameBrain.Utils;
using GameBrain.Casual;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public class FireCrackerRocket : MonoBehaviour
    {
        [SerializeField] private bool isLaunched;
        [SerializeField] private GameObject rocketVisual;
        [SerializeField] private Transform RocketTransform;
        [SerializeField] private GameObject FireCrackerIgnition;
        [SerializeField] private GameObject FireCrackerExplode;
        [SerializeField] private FireCrackerRocketAnimationConfig FireCrackerRocketAnimationConfig;
        private List<Vector3> _path = new List<Vector3>();
        private Vector3 _wayPointPosition;
        private Action _callback;
        private int _pathIndex = 0;
        private float _speed = 2;
        private float _rotationSpeed = 2;
        private float _rotationApplyTime = 0.3f;
        private ICell targetCell;
        private Vector3 targetPosition;
        private Action explodeAction;

        public void Launch(ICell _targetCell, Action action)
        {
            targetCell = _targetCell;
            Launch(targetCell.transform.position);
            explodeAction = action;
        }

        public void Launch(Vector3 pos)
        {
            targetPosition = pos;
            Animate();
        }

        private void Update()
        {
            if (isLaunched)
            {
                MoveRocket(Time.deltaTime);
            }
        }

        private void Animate()
        {
            GetProjectilePath(targetPosition);
            FireCrackerIgnition.SetActive(true);
            DOVirtual.DelayedCall(0.75f, () =>
            {
                isLaunched = true;
                if (explodeAction != null) explodeAction.Invoke();
            });
        }

        private void MoveRocket(float deltaTime)
        {
            var distance = Vector3.Distance(_wayPointPosition, RocketTransform.position);
            if (distance > 0.5f)
            {
                var fireCracker = RocketTransform;
                _rotationApplyTime -= Time.deltaTime;
                if (_rotationApplyTime <= 0)
                {
                    _rotationSpeed += (Time.deltaTime * FireCrackerRocketAnimationConfig.RotationIncreasingFactor);
                }

                _speed += Time.deltaTime * FireCrackerRocketAnimationConfig.SpeedIncreasingFactor;
                _speed = Mathf.Clamp(_speed, 0, FireCrackerRocketAnimationConfig.MaxSpeed);
                Vector3 directionToTarget = _wayPointPosition - fireCracker.position;
                Vector3 currentDirection = fireCracker.forward;
                Vector3 resultingDirection = Vector3.RotateTowards(currentDirection, directionToTarget, _rotationSpeed * Mathf.Deg2Rad * deltaTime, 1f);
                fireCracker.rotation = Quaternion.LookRotation(resultingDirection);
                fireCracker.Translate(Vector3.forward * (_speed * deltaTime), Space.Self);
            }
            else
            {
                _pathIndex++;
                if (_pathIndex >= _path.Count)
                {
                    isLaunched = false;
                    _callback?.Invoke();
                    if (targetCell != null)
                    {
                        if (targetCell.HasDamageableBoardObject())
                            ((DamageableBoardObject)targetCell.BoardObject).TakeDamage(1);
                        else
                            targetCell.ClearHexes();
                    }
                    Explode();
                }
                else
                {
                    _wayPointPosition = _path[_pathIndex];
                }
            }
        }

        private List<Vector3> GetProjectilePath(Vector3 destination)
        {
            _path = new List<Vector3>();
            _path.Add(destination);
            _wayPointPosition = _path[0];
            _rotationSpeed = FireCrackerRocketAnimationConfig.RotationInitialSpeed;
            _speed = FireCrackerRocketAnimationConfig.InitialSpeed;
            _rotationApplyTime = FireCrackerRocketAnimationConfig.RotationApplyTime;
            return _path;
        }

        private void Explode()
        {
            rocketVisual.SetActive(false);
            FireCrackerIgnition.SetActive(false);
            FireCrackerExplode.SetActive(true);
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.RocketHit));

            //if (explodeAction != null)
            //    explodeAction.Invoke();
        }
    }
}
