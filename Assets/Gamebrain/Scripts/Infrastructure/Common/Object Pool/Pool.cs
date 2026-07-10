using System;
using System.Collections.Generic;

namespace EC.Core.Common
{
    public class Pool<T> : IPool<T> where T: class
    {
        protected readonly IFactory<T> _factory;
        protected readonly Stack<T> _items;
        protected readonly uint _seed;
        protected uint _size;

        public IFactory<T> Factory => _factory;
        public IEnumerable<T> Items => _items;
        public uint Size => _size;

        /// <summary>
        /// 
        /// </summary>
        public event Action<T> OnCreate;

        /// <summary>
        /// 
        /// </summary>
        public event Action<T> OnGet;

        /// <summary>
        /// 
        /// </summary>
        public event Action<T> OnRelease;

        /// <summary>
        ///
        /// </summary>
        private Pool()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="seed"></param>
        /// <param name="OnCreate"></param>
        /// <param name="OnGet"></param>
        /// <param name="OnRelease"></param>
        public Pool(IFactory<T> factory, uint seed = 10, Action<T> OnCreate = null, Action<T> OnGet = null, Action<T> OnRelease = null, bool autoInitialize = false)
        {
            if (factory == null)
            {
                const string ExceptionMessage = "Factory is null!";
                throw new NullReferenceException(ExceptionMessage);
            }

            this._factory = factory;
            this._seed = seed;
            this.OnCreate += OnCreate;
            this.OnGet += OnGet;
            this.OnRelease += OnRelease;
            this._items = new Stack<T>((int) _seed);
            if(autoInitialize) Initialize();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Initialize()
        {
            for (int index = 0; index < _seed; index++)
            {
                CreateItem();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void CreateItem(object[] args = null)
        {
            _size++;
            T item = _factory.Create(args);
            _items.Push(item);
            OnCreate?.Invoke(item);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual T Get()
        {
            if (_items.Count == 0) return null;

            T item = _items.Pop();
            OnGet?.Invoke(item);
            return item;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryGet(out T item)
        {
            item = Get();
            return item is not null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="releasedItem"></param>
        public virtual void Release(T releasedItem)
        {
            if(_items.Contains(releasedItem)) 
                return;
            _items.Push(releasedItem);
            OnRelease?.Invoke(releasedItem);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Dispose()
        {
            this.OnCreate = null;
            this.OnGet = null;
            this.OnRelease = null;
            
            for (int index = 0, count = _items.Count; index < count; index++)
            {
                _items.Pop();
            }
        }
    }

    public class ExpandablePool<T> : Pool<T> where T: class
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly uint _maximumSize;

        /// <summary>
        /// 
        /// </summary>
        public uint MaximumSize => _maximumSize;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="seed"></param>
        /// <param name="maximumSize"></param>
        /// <param name="OnCreate"></param>
        /// <param name="OnGet"></param>
        /// <param name="OnRelease"></param>
        /// <param name="autoInitialize"></param>
        public ExpandablePool(IFactory<T> factory, uint seed = 10, uint maximumSize = 1000, Action<T> OnCreate = null, Action<T> OnGet = null,
            Action<T> OnRelease = null, bool autoInitialize = false) : base(factory, seed, OnCreate, OnGet, OnRelease, autoInitialize)
        {
            this._maximumSize = maximumSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override T Get()
        {
            if (_items.Count == 0 && _size < _maximumSize) 
                CreateItem();
            return base.Get();
        }
    }
}
