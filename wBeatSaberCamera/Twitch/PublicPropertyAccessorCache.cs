using System;
using System.Collections;
using System.Collections.Generic;

namespace wBeatSaberCamera.Twitch
{
    public class PublicPropertyAccessorCache<T> : IEnumerable<string>
    {
        private readonly Dictionary<string, Func<T, object>> _cache = new Dictionary<string, Func<T, object>>();

        public Func<T, object> this[string key]
        {
            get => _cache[key];
            set => _cache[key] = value;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _cache.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}