using ArrowRotate.Game;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Raycast'siz tap girişi: ekran → dünya → axial dönüşüm (SKILL.md §11).
    /// TAP = kısa, EŞİK'ten (CameraPanZoom.DragThresholdPx) az kayan, tek-parmak dokunuş; SERBEST BIRAKIŞTA sayılır.
    /// Böylece pan (sürükleme) ve pinch (2 parmak) yanlışlıkla dönüş tetiklemez. CameraPanZoom ile AYNI eşiği
    /// kullanır → koordinasyona gerek yok: kayış > eşik ise burada tap yok, orada pan var.
    /// </summary>
    public class TapController : MonoBehaviour
    {
        public Camera Cam;
        public HexaGameplayManager Manager;

        private const float MaxTapTime = 0.4f; // bundan uzun basış tap değil (uzun basış/gezinme)
        private float Thr2 => CameraPanZoom.DragThresholdPx * CameraPanZoom.DragThresholdPx;

        // dokunuş tap adayı
        private Vector2 _touchStart;
        private float _touchStartTime;
        private bool _touchValid;
        private bool _multiSeen; // gesture boyunca 2+ parmak görüldüyse tap iptal
        // fare (editör)
        private Vector2 _mouseStart;
        private float _mouseStartTime;
        private bool _mouseDown;

        private void Awake()
        {
            if (Manager == null) Manager = GetComponent<HexaGameplayManager>();
            if (Cam == null) Cam = Camera.main;
        }

        private void Update()
        {
            if (Manager == null || Cam == null) return;

            int tc = Input.touchCount;
            if (tc > 0)
            {
                if (tc >= 2) _multiSeen = true;
                var t = Input.GetTouch(0);
                switch (t.phase)
                {
                    case TouchPhase.Began:
                        if (tc == 1) { _touchValid = true; _multiSeen = false; _touchStart = t.position; _touchStartTime = Time.unscaledTime; }
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (_touchValid && (t.position - _touchStart).sqrMagnitude > Thr2) _touchValid = false;
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (_touchValid && !_multiSeen && tc == 1
                            && (t.position - _touchStart).sqrMagnitude <= Thr2
                            && (Time.unscaledTime - _touchStartTime) < MaxTapTime)
                            Tap(t.position);
                        _touchValid = false;
                        break;
                }
            }
            else
            {
                _multiSeen = false;
                // fare (editörde touch yok): basıp-bırak, eşikten az kaydıysa tap
                if (Input.GetMouseButtonDown(0)) { _mouseDown = true; _mouseStart = Input.mousePosition; _mouseStartTime = Time.unscaledTime; }
                else if (Input.GetMouseButtonUp(0) && _mouseDown)
                {
                    _mouseDown = false;
                    if (((Vector2)Input.mousePosition - _mouseStart).sqrMagnitude <= Thr2
                        && (Time.unscaledTime - _mouseStartTime) < MaxTapTime)
                        Tap(Input.mousePosition);
                }
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
