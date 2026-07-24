using System.Collections.Generic;

namespace ArrowRotate.Core
{
    public enum ArrowState { Idle, Connected, Flying, Waiting, Done }

    /// <summary>
    /// Bir ok. KRİTİK: ArrowId mantık kimliğidir, Palette yalnızca görsel renktir —
    /// büyük levellarda birden fazla ok aynı paleti paylaşır (SKILL.md §3).
    /// Cells DAİMA kuyruk→head sıralıdır; ardışık hücreler axial komşudur (invariant).
    /// </summary>
    public sealed class Arrow
    {
        public int ArrowId;
        public int Palette;
        public readonly List<(int q, int r)> Cells = new List<(int q, int r)>();
        public int ExitDir;          // çözüm halindeki (rot=0) uçuş yönü
        public int FreezeAt;         // 0 = buzsuz, 1..3 = gereken toplam çıkış eşiği
        public bool Unfrozen;
        public int LockGroup = -1;   // >=0 = bu ok KİLİTLİ (üstü kapalı), bu grup id'sine ait; grubun anahtar hexagonu tetiklenince açılır
        public bool Unlocked;        // runtime: anahtar tetiklendi, kilit açıldı
        public ArrowState State = ArrowState.Idle;
        public bool Exited;
        /// <summary>Bekleyen ok en son hangi engel oka çarpıp döndü (arrowId; -1 = henüz çarpmadı).
        /// Aynı engele tekrar çarpmayı önler — yalnızca engel değişince yeniden çarpar.</summary>
        public int LastBouncedBlocker = -1;

        public int Len => Cells.Count;
        public (int q, int r) TailPos => Cells[0];
        public (int q, int r) HeadPos => Cells[Cells.Count - 1];
        public bool IsFrozen(int exitedCount) => FreezeAt > 0 && !Unfrozen && exitedCount < FreezeAt;
        public bool IsLocked => LockGroup >= 0 && !Unlocked;
    }
}
