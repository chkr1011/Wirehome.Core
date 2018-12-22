using System.Collections;
using System.Collections.Generic;

namespace Wirehome.Core.Model
{
    public class ConcurrentWirehomeList<TItem> : IList<TItem>
    {
        private readonly List<TItem> _items = new List<TItem>();

        public int Count
        {
            get
            {
                lock (_items)
                {
                    return _items.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public IEnumerator<TItem> GetEnumerator()
        {
            lock (_items)
            {
                return new List<TItem>(_items).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_items)
            {
                return new List<TItem>(_items).GetEnumerator();
            }
        }

        public void Add(TItem item)
        {
            lock (_items)
            {
                _items.Add(item);
            }
        }

        public void Clear()
        {
            lock (_items)
            {
                _items.Clear();
            }
        }

        public bool Contains(TItem item)
        {
            lock (_items)
            {
                return _items.Contains(item);
            }
        }

        public void CopyTo(TItem[] array, int arrayIndex)
        {
            lock (_items)
            {
                _items.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(TItem item)
        {
            lock (_items)
            {
                return _items.Remove(item);
            }
        }

        public int IndexOf(TItem item)
        {
            lock (_items)
            {
                return _items.IndexOf(item);
            }
        }

        public void Insert(int index, TItem item)
        {
            lock (_items)
            {
                _items.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (_items)
            {
                _items.RemoveAt(index);
            }
        }

        public TItem this[int index]
        {
            get
            {
                lock (_items)
                {
                    return _items[index];
                }
            }

            set
            {
                lock (_items)
                {
                    _items[index] = value;
                }
            }
        }
    }
}
