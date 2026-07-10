using UnityEngine;

namespace GameBrain.Store
{
    /// <summary>Logging seam so the shop core never calls UnityEngine.Debug directly (testability).</summary>
    public interface IShopLogger
    {
        void Log(string message);
        void Warn(string message);
        void Error(string message);
    }

    /// <summary>Discards all messages. Default for tests and headless usage.</summary>
    public sealed class NullShopLogger : IShopLogger
    {
        public static readonly NullShopLogger Instance = new NullShopLogger();
        public void Log(string message) { }
        public void Warn(string message) { }
        public void Error(string message) { }
    }

    /// <summary>Routes shop messages to the Unity console with a [Shop] prefix.</summary>
    public sealed class UnityShopLogger : IShopLogger
    {
        public void Log(string message) => Debug.Log($"[Shop] {message}");
        public void Warn(string message) => Debug.LogWarning($"[Shop] {message}");
        public void Error(string message) => Debug.LogError($"[Shop] {message}");
    }
}
