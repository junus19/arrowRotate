using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace EC.Utils
{
    public class CoroutineHandler
    {
        private readonly Dictionary<string, CoroutineHandle> _namedCoroutines;
        private readonly List<CoroutineHandle> _anonymousCoroutines;
        private readonly string _ownerName;

        public int ActiveCoroutineCount => GetActiveCoroutineCount();
        public string OwnerName => _ownerName;

        public CoroutineHandler(string ownerName = null)
        {
            _ownerName = ownerName ?? "Unknown";
            _namedCoroutines = new Dictionary<string, CoroutineHandle>();
            _anonymousCoroutines = new List<CoroutineHandle>();
        }

        public CoroutineHandle StartCoroutine(string name, IEnumerator coroutine)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Coroutine name cannot be null or empty");

            if (!_namedCoroutines.ContainsKey(name))
                _namedCoroutines[name] = new CoroutineHandle(name);

            _namedCoroutines[name].Start(coroutine);
            return _namedCoroutines[name];
        }

        public CoroutineHandle StartCoroutine(IEnumerator coroutine)
        {
            CoroutineHandle handle = new CoroutineHandle();
            handle.Start(coroutine);
            _anonymousCoroutines.Add(handle);
            return handle;
        }

        public void StopCoroutine(string name)
        {
            if (!_namedCoroutines.TryGetValue(name, out CoroutineHandle coroutine)) return;
            coroutine.Stop();
        }

        public void StopCoroutine(CoroutineHandle handle) => handle?.Stop();

        public void StopAllCoroutines()
        {
            foreach (CoroutineHandle handle in _namedCoroutines.Values)
            {
                handle.Stop();
            }

            foreach (CoroutineHandle handle in _anonymousCoroutines)
            {
                handle.Stop();
            }
        }

        public bool IsCoroutineRunning(string name) => _namedCoroutines.ContainsKey(name) && _namedCoroutines[name].IsRunning;

        public CoroutineHandle GetCoroutineHandle(string name) => _namedCoroutines.ContainsKey(name) ? _namedCoroutines[name] : null;

        public void CleanupStoppedCoroutines() => _anonymousCoroutines.RemoveAll(handle => !handle.IsRunning);

        public List<string> GetRunningCoroutineNames() => (from keyValuePair in _namedCoroutines where keyValuePair.Value.IsRunning select keyValuePair.Key).ToList();

        private int GetActiveCoroutineCount() => _namedCoroutines.Values.Count(handle => handle.IsRunning) + _anonymousCoroutines.Count(handle => handle.IsRunning);

        public void Dispose()
        {
            StopAllCoroutines();
            _namedCoroutines.Clear();
            _anonymousCoroutines.Clear();
        }
    }
}
