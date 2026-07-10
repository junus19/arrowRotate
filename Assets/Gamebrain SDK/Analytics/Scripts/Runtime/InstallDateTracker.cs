using System;
using UnityEngine;

namespace GameBrain.SDK
{
    public class InstallDateTracker
    {
        private const string INSTALL_DATE_KEY = "game_install_date";
        public DateTime InstallDate { get; private set; }
        public int DaysSinceInstall { get; private set; }

        public InstallDateTracker()
        {
            Initialize();
        }
    
        private void Initialize()
        {
            if (!PlayerPrefs.HasKey(INSTALL_DATE_KEY))
            {
                string nowString = DateTime.UtcNow.ToString("o");
                PlayerPrefs.SetString(INSTALL_DATE_KEY, nowString);
                PlayerPrefs.Save();
                Debug.Log($"[InstallDateTracker] First launch detected. Install date saved: {nowString}");
            }

            string storedDate = PlayerPrefs.GetString(INSTALL_DATE_KEY);
            InstallDate = DateTime.Parse(storedDate, null, System.Globalization.DateTimeStyles.RoundtripKind);

            DaysSinceInstall = GetCurrentDaysSinceInstall();
            Debug.Log($"[InstallDateTracker] Install date: {InstallDate:yyyy-MM-dd} | Days since install: {GetCurrentDaysSinceInstall()} | Hours since install: {GetCurrentHoursSinceInstall()} | Minutes since install : {GetCurrentMinutesSinceInstall()}");
        }

        public TimeSpan CalculateElapsedTimeSince(DateTime fromDateUtc)
        {
            TimeSpan elapsedTime = DateTime.UtcNow - fromDateUtc;
            return elapsedTime;
        }
    
        public int CalculateDaysSince(DateTime fromDateUtc)
        {
            TimeSpan elapsed = DateTime.UtcNow - fromDateUtc;
            return Mathf.Max(0, (int)elapsed.TotalDays);
        }

        public int CalculateHoursSince(DateTime fromDateUtc)
        {
            TimeSpan elapsed = DateTime.UtcNow - fromDateUtc;
            return Mathf.Max(0, (int)elapsed.TotalHours);
        }
    
        public int CalculateMinutesSince(DateTime fromDateUtc)
        {
            TimeSpan elapsed = DateTime.UtcNow - fromDateUtc;
            return Mathf.Max(0, (int)elapsed.TotalMinutes);
        }
    
        public int GetCurrentDaysSinceInstall()
        {
            DaysSinceInstall = CalculateDaysSince(InstallDate);
            return DaysSinceInstall;
        }
    
        public int GetCurrentHoursSinceInstall()
        {
            DaysSinceInstall = CalculateHoursSince(InstallDate);
            return DaysSinceInstall;
        }
    
        public int GetCurrentMinutesSinceInstall()
        {
            DaysSinceInstall = CalculateMinutesSince(InstallDate);
            return DaysSinceInstall;
        }

        [ContextMenu("Reset Install Date (Testing Only)")]
        public void ResetInstallDate()
        {
            PlayerPrefs.DeleteKey(INSTALL_DATE_KEY);
            PlayerPrefs.Save();
            Initialize();
            Debug.LogWarning("[InstallDateTracker] Install date has been reset.");
        }

        public bool HasBeenInstalledForAtLeast(int days)
        {
            return GetCurrentDaysSinceInstall() >= days;
        }

        public bool IsExactlyDay(int day)
        {
            return GetCurrentDaysSinceInstall() == day;
        }
    }
}