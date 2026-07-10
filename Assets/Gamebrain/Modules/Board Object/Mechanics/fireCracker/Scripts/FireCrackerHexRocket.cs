using System;
using System.Collections.Generic;

using DG.Tweening;
using UnityEngine;

namespace Gameplay
{
    public class FireCrackerHexRocket : MonoBehaviour
    {
     
        [SerializeField] private Transform RocketTransform;
        [SerializeField] private GameObject FireCrackerIgnition;
        //[SerializeField] private GameplaySounds GameplaySounds;
        [SerializeField] private FireCrackerRocketAnimationConfig FireCrackerRocketAnimationConfig;

        /*
     private Transform _cellToExplode;
     private List<Vector3> _path = new List<Vector3>();
     private Vector3 _wayPointPosition;
     private Action _callback;
     private int _pathIndex = 0;
     private bool _firing;
     private bool _launch;

     private float _speed = 2;
     private float _rotationSpeed = 2;
     private float _rotationApplyTime = 0.3f;

     public void Init()
     {
         transform.localPosition = Vector3.zero;
         transform.localEulerAngles = Vector3.zero;
         this.enabled = false;
     }

     public void Animate(Transform cellTransform,Vector3 targetPosition,Transform parentTransform, Action callback)
     {
         this.enabled = true;

         _callback = callback;
         _cellToExplode = cellTransform;

         var destinationPosition = targetPosition;

         GetProjectilePath(destinationPosition);
         _firing = true;

         DOVirtual.DelayedCall(0.75f, () =>
         {
             GameplaySounds.PlayFireCrackerLaunchSound();
             FireCrackerIgnition.SetActive(true);
             transform.parent = parentTransform;
             transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
             RocketTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
             _launch = true;
         });
     }

     private void Update()
     {
         if (_firing && _launch)
         {
             FireCrackerMovement(Time.deltaTime);
         }
     }

     private void FireCrackerMovement(float deltaTime)
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
             Vector3 resultingDirection = Vector3.RotateTowards(currentDirection, directionToTarget,
                 _rotationSpeed * Mathf.Deg2Rad * deltaTime, 1f);

             fireCracker.rotation = Quaternion.LookRotation(resultingDirection);
             fireCracker.Translate(Vector3.forward * (_speed * deltaTime), Space.Self);
         }
         else
         {
             _pathIndex++;

             if (_pathIndex >= _path.Count)
             {
                 _firing = false;
                 _launch = false;
                 GameplaySounds.PlayFireCrackerExplosionSound();
                 _callback?.Invoke();
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
         gameObject.SetActive(false);
     }*/
    }
}