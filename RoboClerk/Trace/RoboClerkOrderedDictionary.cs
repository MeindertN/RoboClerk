using System.Collections;
using System.Collections.Generic;

namespace RoboClerk
{
    public class RoboClerkOrderedDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private List<KeyValuePair<TKey, TValue>> items = new List<KeyValuePair<TKey, TValue>>();
        public RoboClerkOrderedDictionary()
        {

        }

        public TValue this[TKey key]
        {
            get
            {
                var item = items.Find(f => (f.Key.Equals(key)));
                if (item.Equals(default(KeyValuePair<TKey, TValue>)))
                {
                    return default(TValue);
                }
                else
                {
                    return item.Value;
                }
            }

            set
            {
                for (int i = 0; i < items.Count; ++i)
                {
                    if (items[i].Key.Equals(key))
                    {
                        items[i] = new KeyValuePair<TKey, TValue>(key, value);
                        return;
                    }
                }
                items.Add(new KeyValuePair<TKey, TValue>(key, value));
            }
        }

        public bool ContainsKey(TKey key)
        {
            var item = items.Find(f => (f.Key.Equals(key)));
            return !item.Equals(default(KeyValuePair<TKey, TValue>));
        }

        public void Add(TKey key, TValue value)
        {
            items.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)items).GetEnumerator();
        }

        public int Count
        {
            get => items.Count;
        }
    }
}
