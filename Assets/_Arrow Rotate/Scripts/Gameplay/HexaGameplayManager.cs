using System;
using System.Collections;
using System.Collections.Generic;
using ArrowRotate.Core;
using ArrowRotate.Logic;
using ArrowRotate.View;
using UnityEngine;

namespace ArrowRotate.Game
{
    /// <summary>
    /// Oyun akışının tek koordinatörü — ok durum makinesinin sahibi (SKILL.md §4.3):
    ///   idle → connected → (flying → done) | (waiting → connected → ...)
    /// Zamanlamalar prototip birebir: dönüş sonrası kontrol ~170ms, bağlanınca fırlatma 240ms,
    /// bounce 560ms, zincirleme fırlatma ilki 180ms sonra +260ms kademe, win 500ms.
    /// Hamle: yalnızca GERÇEKLEŞEN döndürmeler. Süre: ilk gerçek döndürmeyle başlar.
    /// </summary>
    public class HexaGameplayManager : MonoBehaviour
    {
        public const float CheckDelay = 0.17f;
        public const float LaunchDelay = 0.24f;
        public const float BounceDuration = 0.56f;
        public const float ChainFirstDelay = 0.18f;
        public const float ChainStep = 0.26f;
        public const float WinDelay = 0.5f;
        public const float VanishStartDelay = 0.12f;

        public BoardView Board;

        public event Action LevelWon;
        public event Action<int> ArrowExited;    // arrowId
        public event Action<int> ArrowBlocked;   // arrowId
        public event Action<int> ArrowConnected; // arrowId
        public event Action<int> RotatePerformed;// arrowId — yalnızca gerçekleşen döndürmeler
        public event Action<int> IceBroken;      // arrowId
        public bool InputLocked { get; set; }

        public int MoveCount { get; private set; }
        public float Elapsed => _timerStarted ? Time.time - _timerStart : 0f;
        public bool Won { get; private set; }
        public HexaLevel Level => _level;

        private HexaLevel _level;
        private readonly Dictionary<int, TraceResult> _pendingExit = new Dictionary<int, TraceResult>();
        private bool _timerStarted;
        private float _timerStart;

        private void Awake()
        {
            if (Board == null) Board = GetComponent<BoardView>();
        }

        /// <summary>Uçuş hızı: prototip 0.55 px/ms @ S=34 ⇒ 16.18·S birim/sn.</summary>
        private float FlightSpeed => 16.18f * Board.CellSize;

        private float FlightExtension => 26f * Board.CellSize; // proto: 900px @ S=34

        public void Begin(HexaLevel level)
        {
            StopAllCoroutines();
            _level = level;
            _pendingExit.Clear();
            MoveCount = 0;
            Won = false;
            _timerStarted = false;
            Board.Build(level);
        }

        // ── input ──────────────────────────────────────────────────────────────
        public void OnTapWorld(Vector3 world)
        {
            if (_level == null || Won) return;
            OnTap(Board.WorldToAxial(world));
        }

        public void OnTap((int q, int r) pos)
        {
            if (InputLocked) return;

            var cell = _level.GetCell(pos);
            if (cell == null) return;

            var arrow = _level.Arrows[cell.ArrowId];
            if (arrow.State != ArrowState.Idle) return;       // bağlı/bekleyen/uçan kilitli

            if (arrow.IsFrozen(_level.ExitedCount))
            {
                Board.ShakeIce(arrow.ArrowId); // hamle SAYILMAZ, timer BAŞLAMAZ
                return;
            }

            if (!_timerStarted) { _timerStarted = true; _timerStart = Time.time; }
            MoveCount++;

            cell.Rot = (cell.Rot + 1) % 6;
            Board.GetSegment(pos)?.RotateOneStep();
            RotatePerformed?.Invoke(cell.ArrowId);
            StartCoroutine(CheckAfterDelay(cell.ArrowId));
        }

        private IEnumerator CheckAfterDelay(int arrowId)
        {
            yield return new WaitForSeconds(CheckDelay);
            CheckArrow(arrowId);
        }

        // ── bağlantı → fırlatma ────────────────────────────────────────────────
        public void CheckArrow(int arrowId)
        {
            var arrow = _level.Arrows[arrowId];
            if (arrow.State != ArrowState.Idle) return;

            var res = ConnectionTracer.Trace(_level, arrowId);
            if (!res.Connected) return;

            arrow.State = ArrowState.Connected;
            _pendingExit[arrowId] = res;
            ArrowConnected?.Invoke(arrowId);
            StartCoroutine(LaunchAfterDelay(arrowId));
        }

        private IEnumerator LaunchAfterDelay(int arrowId)
        {
            yield return new WaitForSeconds(LaunchDelay);
            TryLaunch(arrowId);
        }

        private void TryLaunch(int arrowId)
        {
            var arrow = _level.Arrows[arrowId];
            if (arrow.State != ArrowState.Connected && arrow.State != ArrowState.Waiting) return;
            if (!_pendingExit.TryGetValue(arrowId, out var exit)) return;

            var blockers = RayScanner.Blockers(_level, arrowId, exit.HeadCell, exit.ExitDir);
            var pts = FlightPathBuilder.Build(_level, arrowId, Board.CellSize);

            if (blockers.Count > 0)
            {
                arrow.State = ArrowState.Waiting;
                StartBounce(arrow, pts, blockers[0]);
            }
            else
            {
                StartFlight(arrow, pts);
            }
        }

        // ── uçuş ───────────────────────────────────────────────────────────────
        private void StartFlight(Arrow arrow, List<(float x, float y)> pts)
        {
            float s = Board.CellSize;
            arrow.State = ArrowState.Flying;
            arrow.Exited = true; // uçan ok ANINDA engel olmaktan çıkar (bilinen tuzak)

            var cellKeys = new List<(int q, int r)>(arrow.Cells);

            // veri hücrelerini hemen kaldır; overlay gövdeyi çizer
            foreach (var ck in cellKeys)
            {
                _level.Cells.Remove(ck);
                Board.GetSegment(ck)?.SetVisible(false);
            }

            // taşlar kuyruk geçtikçe SIRAYLA yok olur
            float perCell = HexMetrics.Sqrt3 * s / FlightSpeed;
            for (int i = 0; i < cellKeys.Count; i++)
            {
                Board.GetTile(cellKeys[i])?.Vanish(VanishStartDelay + i * perCell);
            }

            var fr = FlightRenderer.Create(pts, s, FlightExtension, BoardView.OverlayZ);
            fr.Fly(FlightSpeed, () => OnFlightDone(arrow, cellKeys));
        }

        private void OnFlightDone(Arrow arrow, List<(int q, int r)> cellKeys)
        {
            arrow.State = ArrowState.Done;
            foreach (var ck in cellKeys) Board.RemoveCellVisuals(ck);
            _pendingExit.Remove(arrow.ArrowId);
            ArrowExited?.Invoke(arrow.ArrowId);

            // buz eşikleri: HERHANGİ bir okun çıkışıyla dolar (SKILL.md §5)
            int exitedCount = _level.ExitedCount;
            Board.UpdateIceBadges(exitedCount);
            foreach (var a in _level.Arrows)
            {
                if (a.FreezeAt > 0 && !a.Unfrozen && exitedCount >= a.FreezeAt)
                {
                    a.Unfrozen = true;
                    Board.BreakIce(a.ArrowId);
                    IceBroken?.Invoke(a.ArrowId);
                }
            }

            bool allExited = true;
            foreach (var a in _level.Arrows) if (!a.Exited) { allExited = false; break; }
            if (allExited)
            {
                StartCoroutine(WinAfterDelay());
                return;
            }

            // bekleyenleri kademeli tetikle (zincirleme çıkış hissi)
            float delay = ChainFirstDelay;
            foreach (var a in _level.Arrows)
            {
                if (a.State != ArrowState.Waiting) continue;
                a.State = ArrowState.Connected;
                StartCoroutine(RelaunchAfterDelay(a.ArrowId, delay));
                delay += ChainStep;
            }
        }

        private IEnumerator RelaunchAfterDelay(int arrowId, float delay)
        {
            yield return new WaitForSeconds(delay);
            TryLaunch(arrowId);
        }

        private IEnumerator WinAfterDelay()
        {
            yield return new WaitForSeconds(WinDelay);
            Won = true;
            Debug.Log($"[HexaArrows] LEVEL WON — moves={MoveCount}, time={Elapsed:F1}s");
            LevelWon?.Invoke();
        }

        // ── bounce / waiting ───────────────────────────────────────────────────
        private void StartBounce(Arrow arrow, List<(float x, float y)> pts, RayBlocker blocker)
        {
            float s = Board.CellSize;
            // ileri mesafe: engel taşının sınırına kadar (prototip formülü, px→S ölçeği)
            float hitDist = Mathf.Max(0.3f * s,
                blocker.Dist * HexMetrics.Sqrt3 * s - 1.05f * s - 0.62f * HexMetrics.Apothem(s));

            // bounce sırasında hücre segmentleri gizlenir, overlay gövdeyi oynatır
            foreach (var ck in arrow.Cells) Board.GetSegment(ck)?.SetVisible(false);

            var fr = FlightRenderer.Create(pts, s, hitDist + 1.2f * s, BoardView.OverlayZ);
            fr.Bounce(hitDist, BounceDuration, () =>
            {
                foreach (var ck in arrow.Cells) Board.GetSegment(ck)?.SetVisible(true);
                PulseWaiting(arrow);
                ArrowBlocked?.Invoke(arrow.ArrowId);
            });
        }

        /// <summary>Doğru çözüldü ama önü tıkalı: taşlar görünmez olur, segment zeminde kalır.</summary>
        private void PulseWaiting(Arrow arrow)
        {
            foreach (var ck in arrow.Cells) Board.GetTile(ck)?.FadeOut();
        }
    }
}
