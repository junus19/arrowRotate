using System.Collections.Generic;
using ArrowRotate.Core;

namespace ArrowRotate.Logic
{
    public readonly struct RayBlocker
    {
        public readonly int ArrowId;
        public readonly int Dist;   // head'den hex-adım uzaklığı (1 tabanlı)
        public readonly Cell Cell;

        public RayBlocker(int arrowId, int dist, Cell cell)
        {
            ArrowId = arrowId;
            Dist = dist;
            Cell = cell;
        }
    }

    /// <summary>
    /// Uçuş ışını taraması (SKILL.md §4.2 / prototip rayBlockers birebir).
    /// Okun KENDİ hücreleri engel sayılmaz; Exited (uçmakta/çıkmış) oklar engel sayılmaz.
    /// </summary>
    public static class RayScanner
    {
        public const int MaxSteps = 16;

        public static List<RayBlocker> Blockers(HexaLevel level, int arrowId, Cell headCell, int exitDir, int maxSteps = MaxSteps)
        {
            var result = new List<RayBlocker>();
            var (dq, dr) = HexCoord.Dirs[exitDir];
            int q = headCell.Q, r = headCell.R;
            for (int k = 1; k <= maxSteps; k++)
            {
                q += dq; r += dr;
                var cc = level.GetCell(q, r);
                if (cc != null && cc.ArrowId != arrowId && !level.Arrows[cc.ArrowId].Exited)
                    result.Add(new RayBlocker(cc.ArrowId, k, cc));
            }
            return result;
        }

        public static bool IsClear(HexaLevel level, int arrowId, Cell headCell, int exitDir, int maxSteps = MaxSteps)
            => Blockers(level, arrowId, headCell, exitDir, maxSteps).Count == 0;
    }
}
