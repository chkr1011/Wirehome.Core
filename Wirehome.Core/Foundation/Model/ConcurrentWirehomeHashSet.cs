using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Wirehome.Core.Foundation.Model
{
    public class ConcurrentWirehomeHashSet<TItem> : ICollection<TItem>
    {
        private readonly ConcurrentDictionary<TItem, object> _items = new ConcurrentDictionary<TItem, object>();

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
        {
            return _items.Keys.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return _items.Keys.GetEnumerator();
        }

        void ICollection<TItem>.Add(TItem item)
        {
            Add(item);
        }

        public bool Add(TItem item)
        {
            return _items.TryAdd(item, null);
        }

        public bool Remove(TItem item)
        {
            return _items.TryRemove(item, out _);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public void CopyTo(TItem[] array, int arrayIndex)
        {
            _items.Keys.CopyTo(array, arrayIndex);
        }
        
        public bool Contains(TItem item)
        {
            return _items.ContainsKey(item);
        }
    }
}
