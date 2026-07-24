using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Ortografik kamera için YUMUŞAK pinch-zoom + pan (kaydırma). Büyük level'larda yakınlaşıp gezinmek için.
    /// Girdi doğrudan kameraya değil bir HEDEF (target) pos/size'a uygulanır; gerçek kamera her frame
    /// SmoothDamp ile hedefe doğru yumuşakça süzülür (sert sıçrama/clamp-snap olmaz). Tüm pan/zoom/clamp
    /// matematiği hedef üzerinde çalışır (geçici kamera-swap ile ekran→düzlem eşlemesi hedeften hesaplanır).
    ///
    /// TAP ile ÇAKIŞMAZ: tek parmak yalnız EŞİK'i (DragThresholdPx) geçince pan olur; geçmezse TapController
    /// serbest bırakışta tap sayar (ikisi de aynı eşiği kullanır). İki parmak = pinch-zoom + iki-parmak pan.
    /// Editörde: fare tekeri zoom, sol-tuş sürükleme pan.
    /// </summary>
    public class CameraPanZoom : MonoBehaviour
    {
        public const float DragThresholdPx = 18f; // TapController ile ORTAK eşik

        public Camera Cam;
        public BoardView Board;
        [Tooltip("En fazla yakınlaşma: fit boyutunun bu çarpanı (küçük = daha çok yakınlaşır).")]
        [Range(0.2f, 1f)] public float MinZoomFactor = 0.4f;
        [Tooltip("Fare tekeri zoom hızı (editör testi).")]
        public float WheelZoomSpeed = 0.12f;
        [Tooltip("Board tam ekrana sığsa bile (uzaklaşmışken) izin verilen pan payı — board yarı-boyutunun oranı. Her iki eksende geçerli.")]
        [Range(0f, 0.6f)] public float OverscrollFactor = 0.25f;
        [Tooltip("Pan yumuşatma süresi (sn). Büyük = daha yumuşak/gecikmeli; 0'a yakın = sert takip.")]
        [Range(0f, 0.4f)] public float PanSmoothTime = 0.09f;
        [Tooltip("Zoom yumuşatma süresi (sn).")]
        [Range(0f, 0.4f)] public float ZoomSmoothTime = 0.10f;

        private bool _xz;

        // HEDEF durum (girdi buraya işlenir; kamera bunu yumuşak takip eder)
        private Vector3 _tPos;
        private float _tSize;
        private bool _tInit;
        private Vector3 _posVel;
        private float _sizeVel;

        // tek parmak pan
        private Vector2 _touchStart, _touchPrev;
        private bool _panning;
        // pinch
        private bool _pinching;
        private float _prevDist;
        private Vector2 _prevMid;
        // fare (editör)
        private Vector2 _mouseStart, _mousePrev;
        private bool _mousePanning;

        // fit dinlenme noktası (overscroll bunun etrafında simetrik)
        private Vector3 _home;
        private bool _homeSet;

        public void Init(Camera cam, BoardView board)
        {
            Cam = cam;
            Board = board;
            _xz = board != null && board.Is3DXZ;
            if (Cam != null)
            {
                // FitCamera Init'ten ÖNCE çağrılmış olmalı → hedef = fit konumu, dinlenme noktası yakalanır
                _tPos = Cam.transform.position;
                _tSize = Cam.orthographicSize;
                _posVel = Vector3.zero;
                _sizeVel = 0f;
                _tInit = true;
                _home = ScreenToPlane(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
                _homeSet = true;
            }
        }

        private float BaseSize => (Board != null && Board.CameraFitReady) ? Board.CameraFitSize
                                : (Cam != null ? Cam.orthographicSize : 5f);
        private float MinSize => BaseSize * MinZoomFactor;

        private void Update()
        {
            if (Cam == null || !Cam.orthographic) return;
            if (!_tInit) { _tPos = Cam.transform.position; _tSize = Cam.orthographicSize; _tInit = true; }

            int tc = Input.touchCount;
            if (tc >= 2) { HandlePinch(); _panning = false; return; }
            _pinching = false;

            if (tc == 1) { HandleOneFinger(Input.GetTouch(0)); return; }
            _panning = false;

            HandleMouse();
        }

        private void LateUpdate()
        {
            if (Cam == null || !_tInit) return;
            float dt = Time.unscaledDeltaTime;
            Cam.transform.position = Vector3.SmoothDamp(Cam.transform.position, _tPos, ref _posVel, PanSmoothTime, Mathf.Infinity, dt);
            Cam.orthographicSize = Mathf.SmoothDamp(Cam.orthographicSize, _tSize, ref _sizeVel, ZoomSmoothTime, Mathf.Infinity, dt);
        }

        private void HandleOneFinger(Touch t)
        {
            if (t.phase == TouchPhase.Began)
            {
                _touchStart = t.position;
                _touchPrev = t.position;
                _panning = false;
            }
            else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                if (!_panning && (t.position - _touchStart).sqrMagnitude > DragThresholdPx * DragThresholdPx)
                {
                    _panning = true;
                    _touchPrev = _touchStart; // pan'i başlangıç noktasından hesapla (sıçrama olmasın)
                }
                if (_panning) { PanByScreen(_touchPrev, t.position); _touchPrev = t.position; }
            }
        }

        private void HandlePinch()
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            Vector2 mid = (t0.position + t1.position) * 0.5f;
            float dist = Vector2.Distance(t0.position, t1.position);

            if (!_pinching || t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                _pinching = true;
                _prevDist = dist;
                _prevMid = mid;
                return;
            }

            PanByScreen(_prevMid, mid);                              // iki parmak orta noktasıyla kaydır
            ZoomToward(mid, _tSize * (_prevDist / Mathf.Max(1e-3f, dist))); // parmak açıklığıyla zoom (hedef boyut)
            _prevDist = dist;
            _prevMid = mid;
        }

        private void HandleMouse()
        {
            float sd = Input.mouseScrollDelta.y;
            if (Mathf.Abs(sd) > 0.01f)
                ZoomToward(Input.mousePosition, _tSize * (1f - sd * WheelZoomSpeed));

            if (Input.GetMouseButtonDown(0))
            {
                _mouseStart = Input.mousePosition;
                _mousePrev = Input.mousePosition;
                _mousePanning = false;
            }
            else if (Input.GetMouseButton(0))
            {
                Vector2 mp = Input.mousePosition;
                if (!_mousePanning && (mp - _mouseStart).sqrMagnitude > DragThresholdPx * DragThresholdPx)
                {
                    _mousePanning = true;
                    _mousePrev = _mouseStart;
                }
                if (_mousePanning) { PanByScreen(_mousePrev, mp); _mousePrev = mp; }
            }
            else if (Input.GetMouseButtonUp(0)) _mousePanning = false;
        }

        /// <summary>Ekran altındaki dünya noktasını sabit tutarak HEDEF'i taşır (kamera yumuşak takip eder).</summary>
        private void PanByScreen(Vector2 prevScreen, Vector2 curScreen)
        {
            Vector3 wPrev = ScreenToPlaneTarget(prevScreen);
            Vector3 wCur = ScreenToPlaneTarget(curScreen);
            _tPos += wPrev - wCur;
            ClampTarget();
        }

        /// <summary>Ekran noktasını sabit tutarak HEDEF orthographicSize'ı hedefe (clamp'li) getirir → o noktaya doğru zoom.</summary>
        private void ZoomToward(Vector2 screen, float targetSize)
        {
            Vector3 before = ScreenToPlaneTarget(screen);
            _tSize = Mathf.Clamp(targetSize, MinSize, BaseSize);
            Vector3 after = ScreenToPlaneTarget(screen);
            _tPos += before - after;
            ClampTarget();
        }

        /// <summary>Board dışına kaçmayı engeller — HEDEF pos üzerinde çalışır (kamera yumuşak takip eder).</summary>
        private void ClampTarget()
        {
            if (Board == null || !Board.CameraFitReady) return;
            Vector3 c = Board.CameraFocusCenter;
            Vector2 e = Board.CameraFocusExtents;

            Vector3 sc = ScreenToPlaneTarget(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            float visH = _tSize * Cam.aspect;
            float visV = _tSize;
            if (_xz)
            {
                float cos = Mathf.Cos(Cam.transform.eulerAngles.x * Mathf.Deg2Rad);
                visV = _tSize / Mathf.Max(0.2f, Mathf.Abs(cos)); // eğik bakışta derinlik uzar
            }

            float cenA = c.x;
            float cenB = _xz ? c.z : c.y;
            float homeA = _homeSet ? _home.x : cenA;
            float homeB = _homeSet ? (_xz ? _home.z : _home.y) : cenB;
            float oX = e.x * OverscrollFactor;
            float oY = e.y * OverscrollFactor;

            // Yakınken (board görünürden büyük): board sınırlarına clamp; home DAİMA erişilebilir.
            // Uzakken (fit; base<0): home etrafında SİMETRİK overscroll → her iki eksende de pan çalışır.
            float baseX = e.x - visH;
            float baseY = e.y - visV;
            float loX, hiX, loY, hiY;
            if (baseX >= 0f) { loX = Mathf.Min(homeA, cenA - baseX) - oX; hiX = Mathf.Max(homeA, cenA + baseX) + oX; }
            else { loX = homeA - oX; hiX = homeA + oX; }
            if (baseY >= 0f) { loY = Mathf.Min(homeB, cenB - baseY) - oY; hiY = Mathf.Max(homeB, cenB + baseY) + oY; }
            else { loY = homeB - oY; hiY = homeB + oY; }

            float scA = sc.x;
            float scB = _xz ? sc.z : sc.y;
            float wantA = Mathf.Clamp(scA, loX, hiX);
            float wantB = Mathf.Clamp(scB, loY, hiY);

            Vector3 corr = _xz ? new Vector3(wantA - scA, 0f, wantB - scB)
                               : new Vector3(wantA - scA, wantB - scB, 0f);
            _tPos += corr;
        }

        /// <summary>Ekran noktasını GERÇEK kameradan Y=0 / z=0 düzlemine ışınlar.</summary>
        private Vector3 ScreenToPlane(Vector2 screen)
        {
            var ray = Cam.ScreenPointToRay(screen);
            var plane = _xz ? new Plane(Vector3.up, 0f) : new Plane(Vector3.forward, 0f);
            if (plane.Raycast(ray, out float d)) return ray.GetPoint(d);
            return Cam.transform.position;
        }

        /// <summary>Ekran noktasını HEDEF durumdan (pos/size) düzleme ışınlar — kamerayı geçici olarak hedefe
        /// alıp geri koyar (frame içinde render yok → güvenli). Yumuşatma gecikmesinden bağımsız doğru eşleme.</summary>
        private Vector3 ScreenToPlaneTarget(Vector2 screen)
        {
            Vector3 p = Cam.transform.position;
            float s = Cam.orthographicSize;
            Cam.transform.position = _tPos;
            Cam.orthographicSize = _tSize;
            Vector3 w = ScreenToPlane(screen);
            Cam.transform.position = p;
            Cam.orthographicSize = s;
            return w;
        }
    }
}
