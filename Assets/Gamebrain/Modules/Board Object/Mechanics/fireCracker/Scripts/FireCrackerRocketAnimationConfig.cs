using UnityEngine;

namespace Gameplay
{
    // [CreateAssetMenu(fileName = "FireCrackerRocketAnimationConfig", menuName = "Gameplay/FireCracker Rocket Animation Config")]
    public class FireCrackerRocketAnimationConfig : ScriptableObject
    {
        public float RotationInitialSpeed = 2;
        public float RotationApplyTime = 0.135f;
        public float InitialSpeed = 10;
        public float MaxSpeed = 20;
        public float RotationIncreasingFactor = 1400;
        public float SpeedIncreasingFactor = 12;
    }
}