using System;
using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace EC.Core.Common
{
    /// <summary>
    /// 
    /// </summary>
    public class PoolManager
    {
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Type, IPool<Object>> Pools { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public void Init()
        {
            Pools = new Dictionary<Type, IPool<Object>>();
        }

        /// <summary>
        /// 
        /// </summary>
        public void LateInit()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pool"></param>
        public void Add(Pool<Object> pool)
        {
            if (IsNull(pool) || Contains(pool.GetType())) return;
            Pools.Add(pool.Factory.GetItemType(), pool);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pool"></param>
        public void Remove(IPool<Object> pool)
        {
            if (!Contains(pool)) return;
            Pools.Remove(GetPoolType(pool));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="initialSize"></param>
        /// <param name="maxSize"></param>
        /// <param name="extendable"></param>
        /// <param name="autoInitialize"></param>
        /// <returns></returns>
        public Pool<Object> Create(MonoBehaviour prefab, int initialSize, int maxSize, bool extendable = true, bool autoInitialize = true)
        {
            Type genericPoolType = typeof(IPool<>);
            Type[] typeArgs = new Type[] {prefab.GetType()};
            Type poolType = genericPoolType.MakeGenericType(typeArgs);
            object[] args = {prefab, initialSize, maxSize, extendable, autoInitialize};
            Pool<Object> pool = (Pool<Object>) Activator.CreateInstance(poolType, args);
            Add(pool);
            return pool;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool Contains<T>() where T : MonoBehaviour
        {
            return Pools.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool Contains(Type type)
        {
            return Pools.ContainsKey(type);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        public bool Contains(IPool<Object> pool)
        {
            return Pools.ContainsValue(pool);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        public bool IsNull(IPool<Object> pool)
        {
            return pool == null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        public bool IsNullOrEmpty(IPool<Object> pool)
        {
            return pool == null || pool.Size == 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        public Type GetPoolType(IPool<Object> pool)
        {
            return pool.Factory.GetType();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pool"></param>
        /// <returns></returns>
        public bool TryGetPool(Type type, out IPool<Object> pool)
        {
            pool = !Contains(type) ? null : Pools[type];
            return pool != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pool"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryGetPool<T>(out IPool<Object> pool) where T : Object
        {
            pool = !Contains(typeof(T)) ? null : Pools[typeof(T)];
            return pool != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pool"></param>
        public void DestroyPool(IPool<Object> pool)
        {
            pool.Dispose();
            Remove(pool);
        }

        /// <summary>
        /// 
        /// </summary>
        public void ClearAll()
        {
            foreach (IPool<Object> pool in Pools.Values)
            {
                DestroyPool(pool);
            }

            Pools.Clear();
        }
    }
}
