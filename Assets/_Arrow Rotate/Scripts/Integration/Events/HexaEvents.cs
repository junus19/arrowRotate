using GameBrain.Utils;

namespace ArrowRotate.Integration
{
    /// <summary>
    /// Oyun-özel EventBus event'leri (CLAUDE.md sözleşmesi).
    /// UI ve diğer sistemler yalnızca bunlara abone olur; doğrudan referans yasak.
    /// </summary>
    /// <summary>HUD çipleri için ok başına bilgi (arrowId ≠ palette ayrımına dikkat).</summary>
    public struct HexaArrowChipInfo
    {
        public int ArrowId;
        public int Palette;
        public int FreezeAt; // 0 = buzsuz
    }

    public struct HexaLevelStartedEvent : IEvent
    {
        public int ArrowCount;
        public int FrozenArrowCount;
        public int Seed;
        public HexaArrowChipInfo[] Arrows;
    }

    public struct HexaRotateEvent : IEvent
    {
        public int ArrowId;
        public int MoveCount;
    }

    public struct HexaArrowConnectedEvent : IEvent
    {
        public int ArrowId;
    }

    public struct HexaArrowExitedEvent : IEvent
    {
        public int ArrowId;
        public int ExitedCount;
        public int TotalCount;
    }

    public struct HexaArrowBlockedEvent : IEvent
    {
        public int ArrowId;
    }

    public struct HexaIceBrokenEvent : IEvent
    {
        public int ArrowId;
    }

    public struct HexaLevelWonEvent : IEvent
    {
        public int MoveCount;
        public float ElapsedSeconds;
    }

    public struct HexaTutorialEvent : IEvent
    {
        public int StepIndex;
    }
}
