using ArrowRotate.Game;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Raycast'siz tap girişi: ekran → dünya → axial dönüşüm (SKILL.md §11).
    /// Debounce yok — arka arkaya hızlı dokunuşlar manager'da birikerek döner.
    /// </summary>
    public class TapController : MonoBehaviour
    {
        public Camera Cam;
        public HexaGameplayManager Manager;

        private void Awake()
        {
            if (Manager == null) Manager = GetComponent<HexaGameplayManager>();
            if (Cam == null) Cam = Camera.main;
        }

        private void Update()
        {
            if (Manager == null || Cam == null) return;

            if (Input.GetMouseButtonDown(0))
                Tap(Input.mousePosition);

            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began) Tap(t.position);
            }
        }

        private void Tap(Vector3 screenPos)
        {
            // z=0 board düzlemine ışın kesişimi — dik ve eğik (3D mod) kamerada aynı çalışır
            var ray = Cam.ScreenPointToRay(screenPos);
            if (Mathf.Abs(ray.direction.z) < 1e-5f) return;
            float t = -ray.origin.z / ray.direction.z;
            Manager.OnTapWorld(ray.GetPoint(t));
        }
    }
}
