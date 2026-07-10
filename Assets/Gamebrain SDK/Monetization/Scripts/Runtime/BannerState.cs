using System;
using UnityEngine;

namespace GameBrain.SDK.Monetization
{
    /// <summary>
    /// Static bridge exposing the native banner's visibility + pixel height to UI code, so views (e.g. the
    /// bottom nav bar) can offset themselves without referencing the define-guarded MAX module directly.
    /// The ad module is the only writer; UI only reads and subscribes.
    /// </summary>
    public static class BannerState
    {
        public static bool IsVisible { get; private set; }

        /// <summary>Banner height in SCREEN PIXELS (convert to canvas units with canvas.scaleFactor).</summary>
        public static float HeightPx { get; private set; }

        /// <summary>
        /// False until the ad module reports for the first time. While false, UI may PREDICT visibility
        /// from AnalyticsConfig (AdsEnabled + BannerEnabled) so layout is correct before the SDK is up.
        /// </summary>
        public static bool HasReported { get; private set; }

        public static event Action OnChanged;

        public static void Set(bool visible, float heightPx)
        {
            if (HasReported && IsVisible == visible && Mathf.Approximately(HeightPx, heightPx))
                return;

            HasReported = true;
            IsVisible = visible;
            HeightPx = heightPx;
            OnChanged?.Invoke();
        }
    }
}
