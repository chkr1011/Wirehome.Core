using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Wirehome.Core.Model
{
    public class WirehomeHashSet<TItem> : ICollection<TItem>
    {
        private readonly ConcurrentDictionary<TItem, byte> _items = new ConcurrentDictionary<TItem, byte>();

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public IEnumerator<TItem> GetEnumerator()
        {
            return _items.Keys.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.Keys.GetEnumerator();
        }

        public void Add(TItem item)
        {
            _items.TryAdd(item, 0);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(TItem item)
        {
            return _items.ContainsKey(item);
        }

        public void CopyTo(TItem[] array, int arrayIndex)
        {
            _items.Keys.CopyTo(array, arrayIndex);
        }

        public bool Remove(TItem item)
        {
            return _items.TryRemove(item, out _);
        }
    }
}
