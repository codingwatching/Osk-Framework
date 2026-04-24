using System;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _pool;
        private readonly HashSet<T> _inUse;
        private readonly Func<T> _factoryFunc;

        public int Count => _pool.Count + _inUse.Count;
        public int CountUsedItems => _inUse.Count;
        public int CountInactiveItems => _pool.Count;

        public ObjectPool(Func<T> factoryFunc, int initialSize)
        {
            _factoryFunc = factoryFunc;
            _pool = new Stack<T>(initialSize);
            _inUse = new HashSet<T>(initialSize);
            
            for (int i = 0; i < initialSize; i++)
            {
                T item = _factoryFunc();
                if (item != null) _pool.Push(item);
            }
        }

        public T GetItem()
        {
            T item = null;

            // Lấy từ pool cho đến khi được 1 cái không null (đề phòng bị Destroy ngoài ý muốn)
            while (_pool.Count > 0)
            {
                item = _pool.Pop();
                if (item != null && !item.Equals(null)) break;
                item = null;
            }

            if (item == null)
            {
                item = _factoryFunc();
            }

            if (item != null)
            {
                _inUse.Add(item);
            }

            return item;
        }

        public void ReleaseItem(T item)
        {
            if (item == null || item.Equals(null)) return;

            if (_inUse.Remove(item))
            {
                _pool.Push(item);
            }
        }

        public void Refill(int amount = 1)
        {
            for (int i = 0; i < amount; i++)
            {
                T item = _factoryFunc();
                if (item != null) _pool.Push(item);
            }
        }

        public void DestroyAndClean()
        {
            // Hủy các item đang rảnh
            while (_pool.Count > 0)
            {
                T item = _pool.Pop();
                DestroyItem(item);
            }

            // Hủy các item đang dùng (nếu cần dọn sạch hoàn toàn)
            foreach (var item in _inUse)
            {
                DestroyItem(item);
            }
            _inUse.Clear();
        }

        private void DestroyItem(T item)
        {
            if (item == null || item.Equals(null)) return;

            if (item is GameObject go)
                UnityEngine.Object.Destroy(go);
            else if (item is Component comp)
                UnityEngine.Object.Destroy(comp.gameObject);
        }

        public void Clear()
        {
            _pool.Clear();
            _inUse.Clear();
        }
    }
}