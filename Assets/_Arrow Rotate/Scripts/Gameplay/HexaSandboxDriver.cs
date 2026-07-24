using ArrowRotate.Core;
using ArrowRotate.Generation;
using ArrowRotate.Logic;
using ArrowRotate.View;
using UnityEngine;

namespace ArrowRotate.Game
{
    /// <summary>
    /// Faz 3-4 test sahnesi sürücüsü — Gamebrain'siz bağımsız oynanış.
    /// Editörden/koddan level üretir, çözer, tap simüle eder (MCP ile test edilebilir).
    /// Faz 6'da yerini HexaGameState_Gameplay alacak; bu sınıf sandbox'ta kalır.
    /// </summary>
    public class HexaSandboxDriver : MonoBehaviour
    {
        public int LevelNumber = 1;
        public int Seed = 12345;

        public BoardView Board { get; private set; }
        public HexaGameplayManager Manager { get; private set; }
        public HexaLevel Level { get; private set; }

        private void Start()
        {
            Application.runInBackground = true; // MCP/arka plan testlerinde frame akışı dursun istemiyoruz
            Board = GetComponent<BoardView>();
            Manager = GetComponent<HexaGameplayManager>();
            Manager.Board = Board;

            var tap = GetComponent<TapController>();
            if (tap != null)
            {
                tap.Cam = Camera.main;
                tap.Manager = Manager;
            }

            Manager.LevelWon += () => Debug.Log("[Sandbox] WIN callback alındı");
            BuildLevel();

            // Pinch-zoom + pan (test için de) — kadrajı BuildLevel'daki FitCamera'dan okur
            var panZoom = GetComponent<CameraPanZoom>();
            if (panZoom == null) panZoom = gameObject.AddComponent<CameraPanZoom>();
            panZoom.Init(Camera.main, Board);
        }

        [ContextMenu("Regenerate")]
        public void BuildLevel()
        {
            Level = LevelGenerator.Generate(Seed, LevelConfig.ForLevel(LevelNumber));
            Manager.Begin(Level);
            Board.FitCamera(Camera.main);
            Debug.Log($"[Sandbox] Level {LevelNumber} seed={Level.Seed}: {Level.Arrows.Count} ok, {Level.Cells.Count} hücre, edges={Level.EdgeCount}");
        }

        /// <summary>Test: hücreye tek tap (veri + görsel + kontrol zinciri).</summary>
        public void TapCell(int q, int r) => Manager.OnTap((q, r));

        /// <summary>Test: bir okun tüm hücrelerini çözülmüş rot'a (0) çevirip kontrolü tetikler.</summary>
        public void SolveArrow(int arrowId)
        {
            var arrow = Level.Arrows[arrowId];
            if (arrow.State != ArrowState.Idle) return;
            foreach (var pos in arrow.Cells)
            {
                var cell = Level.GetCell(pos);
                if (cell == null) continue;
                cell.Rot = 0;
                Board.GetSegment(pos)?.SetInstantRot(0);
            }
            Manager.CheckArrow(arrowId);
        }

        /// <summary>Test: tüm okları topolojik uygunlukla sırayla çözer (win akışı testi).</summary>
        public void SolveAll()
        {
            StartCoroutine(SolveAllRoutine());
        }

        private System.Collections.IEnumerator SolveAllRoutine()
        {
            int guard = 0;
            while (!Manager.Won && guard++ < Level.Arrows.Count * 3)
            {
                foreach (var arrow in Level.Arrows)
                {
                    if (arrow.State == ArrowState.Idle && !arrow.IsFrozen(Level.ExitedCount))
                    {
                        SolveArrow(arrow.ArrowId);
                        yield return new WaitForSeconds(0.6f);
                    }
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>Test: bir okun mevcut trace durumu (konsola).</summary>
        public string TraceInfo(int arrowId)
        {
            var res = ConnectionTracer.Trace(Level, arrowId);
            return $"arrow={arrowId} state={Level.Arrows[arrowId].State} connected={res.Connected} exitDir={res.ExitDir}";
        }
    }
}
