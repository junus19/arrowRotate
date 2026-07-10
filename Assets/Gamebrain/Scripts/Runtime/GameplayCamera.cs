using DG.Tweening;
using UnityEngine;

namespace GameBrain
{
    public class GameplayCamera : MonoBehaviour
    {
        [SerializeField] Vector3 defaultAngle;
        [SerializeField] Vector3 defaultPosition;

        [SerializeField] Vector3 boosterAngle;
        [SerializeField] Vector3 boosterPosition;

        [Header("Animation")]
        [SerializeField, Range(0f, 10f)] private float _animationDuration = 0.4f;
        [SerializeField, Range(0f, 10f)] private Ease _animationEase = Ease.InOutSine;

        private Tween _positionTween;
        private Tween _rotationTween;

        public void SetDefaultView(bool animate)
        {
            _positionTween?.Kill();
            _rotationTween?.Kill();
            if (animate)
            {
                _positionTween = transform.DOMove(defaultPosition, _animationDuration).SetEase(_animationEase);
                _rotationTween = transform.DOLocalRotate(defaultAngle, _animationDuration).SetEase(_animationEase);
            }
            else
            {
                transform.eulerAngles = defaultAngle;
                transform.position = defaultPosition;
            }
        }

        public void SetBoosterView(bool animate)
        {
            _positionTween?.Kill();
            _rotationTween?.Kill();
            if (animate)
            {
                _positionTween = transform.DOMove(boosterPosition, _animationDuration).SetEase(_animationEase);
                _rotationTween = transform.DOLocalRotate(boosterAngle, _animationDuration).SetEase(_animationEase);
            }
            else
            {
                transform.eulerAngles = boosterAngle;
                transform.position = boosterPosition;
            }
        }
    }
}
