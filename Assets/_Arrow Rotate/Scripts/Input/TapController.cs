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

            // ⚠ Cihazda dokunuş, mouse tıklamasını da SİMÜLE eder (Input.simulateMouseWithTouches
            // varsayılan açık). İki dal birden okunursa tek dokunuş 2× Tap → 120° dönüş (build'de yaşandı,
            // editörde touch olmadığı için görünmez). Bu yüzden touch VARSA mouse dalı ATLANIR.
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began) Tap(t.position);
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Tap(Input.mousePosition);
            }
        }

        private void Tap(Vector3 screenPos)
        {
            var ray = Cam.ScreenPointToRay(screenPos);
            // Board düzlemine ışın: XZ modda Y=0 düzlemi, 2D modda z=0 düzlemi
            bool xz = Manager.Board != null && Manager.Board.Is3DXZ;
            var plane = xz ? new UnityEngine.Plane(Vector3.up, 0f) : new UnityEngine.Plane(Vector3.forward, 0f);
            if (plane.Raycast(ray, out float dist))
                Manager.OnTapWorld(ray.GetPoint(dist));
        }
    }
}
