using UnityEngine;
using System.Collections;

namespace EC.Utils
{
    public class CoroutineExecuter : MonoBehaviour
    {
        private static CoroutineExecuter _instance;

        public static CoroutineExecuter Instance
        {
            get
            {
                if (_instance != null) return _instance;
                GameObject go = new GameObject("Coroutine Executer");
                _instance = go.AddComponent<CoroutineExecuter>();
                DontDestroyOnLoad(go);

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public Coroutine StartManagedCoroutine(IEnumerator coroutine)
        {
            return StartCoroutine(coroutine);
        }

        public void StopManagedCoroutine(Coroutine coroutine)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
    }
}
