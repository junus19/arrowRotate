using System.Collections.Generic;
using ArrowRotate.Core;
using GameBrain.Casual;
using UnityEngine;

namespace ArrowRotate.Integration
{
    /// <summary>
    /// Hexa Arrows level'ı — hücreler asset'te AÇIKÇA saklanır (arrowJam deseni; elle düzenlenebilir).
    /// Cells dizisi DAİMA ok sırasında ve her ok içinde kuyruk→head sıralıdır (invariant).
    /// Rot değerleri de saklanır: editördeki başlangıç dizilimi oyundakiyle birebir aynıdır.
    /// Scene alanı boş bırakılır — Level.Load() sahne yüklemeyi atlar.
    /// Düzenleme: Arrow Rotate ▸ Level Editor.
    /// </summary>
    [CreateAssetMenu(menuName = "Arrow Rotate/Hexa Level Data", fileName = "HexaLevel_000", order = 0)]
    public class HexaLevelData : LevelData
    {
        [HideInInspector] public int Radius = 5;
        [HideInInspector] public HexaCellSave[] Cells = System.Array.Empty<HexaCellSave>();
        [HideInInspector] public HexaArrowSave[] Arrows = System.Array.Empty<HexaArrowSave>();

        public bool HasCells => Cells != null && Cells.Length > 0;

        public HexaLevel ToHexaLevel()
        {
            var level = new HexaLevel();
            if (Arrows == null || Cells == null) return level;

            for (int i = 0; i < Arrows.Length; i++)
            {
                level.Arrows.Add(new Arrow
                {
                    ArrowId = i,
                    Palette = Arrows[i].Palette,
                    FreezeAt = Arrows[i].FreezeAt
                });
            }

            foreach (var s in Cells) // ok sırası + kuyruk→head sıralı
            {
                var cell = new Cell { Q = s.Q, R = s.R, ArrowId = s.ArrowId, Type = s.Type, A = s.A, B = s.B, Rot = s.Rot, Layer = s.Layer };
                level.AddCell(cell); // Layer 0 → yüzey, 1..2 → gömülü yığın
                level.Arrows[s.ArrowId].Cells.Add((s.Q, s.R));
                if (s.Type == CellType.Head) level.Arrows[s.ArrowId].ExitDir = s.B;
            }
            return level;
        }

        public void FromHexaLevel(HexaLevel level, int radius)
        {
            Radius = radius;
            Arrows = new HexaArrowSave[level.Arrows.Count];
            var cells = new List<HexaCellSave>();

            foreach (var arrow in level.Arrows)
            {
                Arrows[arrow.ArrowId] = new HexaArrowSave { Palette = arrow.Palette, FreezeAt = arrow.FreezeAt };
                foreach (var pos in arrow.Cells)
                {
                    var c = level.GetArrowCell(arrow.ArrowId, pos); // yüzey ya da gömülü — okun kendi hücresi
                    cells.Add(new HexaCellSave { Q = c.Q, R = c.R, ArrowId = c.ArrowId, Type = c.Type, A = c.A, B = c.B, Rot = c.Rot, Layer = c.Layer });
                }
            }
            Cells = cells.ToArray();
        }
    }

    [System.Serializable]
    public class HexaCellSave
    {
        public int Q;
        public int R;
        public int ArrowId;
        public CellType Type;
        public int A = -1; // lokal giriş kenarı (tail: -1)
        public int B;      // lokal çıkış kenarı / head: uçuş yönü
        public int Rot;    // 0-5 — başlangıç karışıklığı asset'te saklanır
        public int Layer;  // 0 = yüzey (varsayılan → eski asset'ler uyumlu), 1..2 = gömülü katman
    }

    [System.Serializable]
    public class HexaArrowSave
    {
        public int Palette;  // görsel renk (arrowId DEĞİL)
        public int FreezeAt; // 0 = buzsuz, 1..3 = eşik
    }
}
