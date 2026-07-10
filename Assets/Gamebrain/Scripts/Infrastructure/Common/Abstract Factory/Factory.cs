using System;

namespace EC.Core.Common
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class Factory<T> : IFactory<T>
    {
        /// <summary>
        /// 
        /// </summary>
        protected event Action<T> OnCreate;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="onCreate"></param>
        protected Factory(Action<T> onCreate = null)
        {
            this.OnCreate = onCreate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public T Create(object[] args = null)
        {
            T item = CreateProduct(args);
            OnCreate?.Invoke(item);
            return item;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected abstract T CreateProduct(object[] args = null);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Type GetItemType() => typeof(T);
    }
}
