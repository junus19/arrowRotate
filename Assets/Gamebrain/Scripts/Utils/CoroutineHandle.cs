using System;
using UnityEngine;
using System.Collections;

namespace EC.Utils
{
    public class CoroutineHandle
    {
        private Coroutine _coroutine;
        private readonly string _name;

        public bool IsRunning => _coroutine != null;
        public string Name => _name;

        public CoroutineHandle(string name = null)
        {
            _name = name ?? Guid.NewGuid().ToString();
        }

        public void Start(IEnumerator coroutine)
        {
            Stop();
            _coroutine = CoroutineExecuter.Instance.StartManagedCoroutine(coroutine);
        }

        public void Stop()
        {
            if (_coroutine == null) return;
            CoroutineExecuter.Instance.StopManagedCoroutine(_coroutine);
            _coroutine = null;
        }
    }
}
