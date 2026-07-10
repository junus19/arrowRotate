using System;

namespace EC.Core.Common
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFactory<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        T Create(object[] args = null);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Type GetItemType();
    }
}
