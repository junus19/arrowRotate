using System;
using System.Collections.Generic;

namespace EC.Core.Common
{
    public interface IPool<T>
    {
        /// <summary>
        /// 
        /// </summary>
        IFactory<T> Factory { get; }
        
        /// <summary>
        /// 
        /// </summary>
        IEnumerable<T> Items { get; }
        
        /// <summary>
        /// 
        /// </summary>
        uint Size { get; }

        /// <summary>
        /// 
        /// </summary>
        event Action<T> OnCreate;

        /// <summary>
        /// 
        /// </summary>
        event Action<T> OnGet;

        /// <summary>
        /// 
        /// </summary>
        event Action<T> OnRelease;
        
        /// <summary>
        /// 
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        T Get();
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        bool TryGet(out T item);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="releasedItem"></param>
        void Release(T releasedItem);
        
        /// <summary>
        /// 
        /// </summary>
        void Dispose();
    }
}
