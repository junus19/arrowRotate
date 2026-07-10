using UnityEngine;

namespace EG.Core.Common
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        /// <summary>
        /// 
        /// </summary>
        protected static bool _applicationIsQuitting;
        
        /// <summary>
        /// 
        /// </summary>
        protected static T _current;

        /// <summary>
        /// 
        /// </summary>
        public static T Current
        {
            get
            {
                if (_current) 
                    return _current;
                
                _current = FindFirstObjectByType<T>();
                
                if (!_applicationIsQuitting && !_current)
                    _current = new GameObject(typeof(T).Name).AddComponent<T>();
                
                return _current;
            }
            private set
            {
                if (_current) 
                    Destroy(value.gameObject);
                else
                    _current = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void Awake()
        {
            Current = this as T;
        }

        /// <summary>
        /// 
        /// </summary>
        private static void OnApplicationQuitting()
        {
            _applicationIsQuitting = true;
            Application.quitting -= OnApplicationQuitting;
        }
    }
}
