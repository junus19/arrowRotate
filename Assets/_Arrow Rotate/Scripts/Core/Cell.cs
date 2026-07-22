namespace ArrowRotate.Core
{
    public enum CellType { Tail, Mid, Head }

    /// <summary>
    /// Tek hex hücresi. a/b LOKAL kenar yönleridir (0-5); çözülmüş halde rot=0 iken lokal = dünya.
    /// Dünya yönü daima (local + rot) % 6. Her tap: rot = (rot + 1) % 6.
    /// tail: B=çıkış (A yok, -1) · mid: A=giriş, B=çıkış · head: A=giriş, B=uçuş yönü.
    /// Layer: 0 = yüzey (oynanabilir), 1..2 = gömülü. Üstteki hücre temizlenince bir katman yükselir.
    /// </summary>
    public sealed class Cell
    {
        public int Q;
        public int R;
        public int ArrowId;
        public CellType Type;
        public int A = -1;
        public int B;
        public int Rot;
        public int Layer; // 0 = yüzey; 1..MaxBuriedLayers = gömülü

        public (int q, int r) Pos => (Q, R);
        public int WorldA => (A + Rot) % 6;
        public int WorldB => (B + Rot) % 6;
    }
}
