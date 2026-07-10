using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameBrain.Casual
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance;

        public Camera GameplayCamera;
        public Camera GameplayUICamera;
        public Camera Flyind3dObjectCamera;
        
        void Awake()
        {
            Instance = this;
            // var camData = GameplayCamera.GetUniversalAdditionalCameraData();
            // camData.cameraStack.Add(GameplayUICamera);
            // camData.cameraStack.Add(Flyind3dObjectCamera);
        }
    }
}
