﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AmbientServices
{
    [DefaultAmbientService]
    class BasicAmbientCache : IAmbientCache
    {
        static readonly IAmbientSettings _Settings = AmbientServices.Registry<IAmbientSettings>.Implementation;
        static readonly ISetting<int> CallFrequencyToEject = _Settings.GetSetting<int>(nameof(BasicAmbientCache) + "-EjectFrequency", s => Int32.Parse(s), 100);
        static readonly ISetting<int> CountToEject = _Settings.GetSetting<int>(nameof(BasicAmbientCache) + "-ItemCount", s => Int32.Parse(s), 1000);

        private int _expireCount = 0;
        private ConcurrentQueue<TimedQueueEntry> _timedQueue = new ConcurrentQueue<TimedQueueEntry>();
        private ConcurrentQueue<string> _untimedQueue = new ConcurrentQueue<string>();
        private ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();

        public BasicAmbientCache()
        {
        }

        struct TimedQueueEntry
        {
            public string Key;
            public DateTime Expiration;
        }
        class CacheEntry
        {
            public string Key;
            public DateTime? Expiration;
            public object Entry;
        }
        public Task<T> TryGet<T>(string key, TimeSpan? refresh = null, CancellationToken cancel = default(CancellationToken)) where T : class
        {
            CacheEntry entry;
            if (_cache.TryGetValue(key, out entry))
            {
                // refresh expiration?
                if (refresh != null)
                {
                    // update the expiration time in the cache entry and add a NEW timed queue entry (we'll ignore the other one when we dequeue it)
                    DateTime newExpiration = DateTime.UtcNow.Add(refresh.Value);
                    entry.Expiration = newExpiration;
                    _timedQueue.Enqueue(new TimedQueueEntry { Key = key, Expiration = newExpiration });
                }
                EjectIfNeeded();
                // no expiration or NOT expired?
                if (entry.Expiration == null || entry.Expiration >= DateTime.UtcNow) return Task.FromResult<T>(entry.Entry as T);
            }
            else
            {
                EjectIfNeeded();
            }
            return Task.FromResult<T>(null);
        }

        public Task Set<T>(bool localOnly, string itemKey, T item, TimeSpan? maxCacheDuration = null, DateTime? expiration = null, CancellationToken cancel = default(CancellationToken)) where T : class
        {
            DateTime? actualExpiration = null;
            if (maxCacheDuration != null) actualExpiration = DateTime.UtcNow.Add(maxCacheDuration.Value);
            if (expiration != null && expiration.Value.Kind == DateTimeKind.Local) expiration = expiration.Value.ToUniversalTime();
            if (expiration < actualExpiration) actualExpiration = expiration;
            CacheEntry entry = new CacheEntry { Key = itemKey, Expiration = actualExpiration, Entry = item };
            _cache.AddOrUpdate(itemKey, entry, (k, v) => entry);
            if (actualExpiration == null)
            {
                _untimedQueue.Enqueue(itemKey);
            }
            else
            {
                _timedQueue.Enqueue(new TimedQueueEntry { Key = itemKey, Expiration = actualExpiration.Value });
            }
            EjectIfNeeded();
            return Task.CompletedTask;
        }
        void EjectIfNeeded()
        {
            // time to eject?
            while ((System.Threading.Interlocked.Increment(ref _expireCount) % CallFrequencyToEject.Value) == 0 || (_untimedQueue.Count + _timedQueue.Count) > CountToEject.Value)
            {
                // removing at least one timed item (and all expired items)
                TimedQueueEntry qEntry;
                while (_timedQueue.TryDequeue(out qEntry))
                {
                    // can we find this item in the cache?
                    CacheEntry entry;
                    if (_cache.TryGetValue(qEntry.Key, out entry))
                    {
                        // is the expiration still the same?
                        if (qEntry.Expiration == entry.Expiration)
                        {
                            // remove it from the cache
                            _cache.TryRemove(qEntry.Key, out entry);
                        }
                        else // else the item was refreshed, so we should ignore this entry and go around again
                        {
                            continue;
                        }
                    }
                    // else we couldn't find the entry in the cache, so just move to the next entry
                    else
                    {
                        continue;
                    }
                    // peek at the next entry
                    if (_timedQueue.TryPeek(out qEntry))
                    {
                        // has this entry expired?
                        if (entry.Expiration < DateTime.UtcNow) continue;
                        // else the entry hasn't expired and we either removed an entry above or skipped this code, so we can just fall through and exit the loop
                    }
                    // if we get here, there is no reason to look at another timed entry
                    break;
                }
                // remove one untimed entry
                string key;
                while (_untimedQueue.TryDequeue(out key))
                {
                    // can we find this item in the cache?
                    CacheEntry entry;
                    if (_cache.TryGetValue(key, out entry))
                    {
                        // is the expiration still the same (ie. untimed)?
                        if (entry.Expiration == null)
                        {
                            // remove it from the cache
                            _cache.TryRemove(key, out entry);
                        }
                        else // else the item was refreshed, so we should ignore this entry and go around again to remove another entry
                        {
                            continue;
                        }
                    }
                    // else we couldn't find the entry in the cache, so just move to the next entry
                    else
                    {
                        continue;
                    }
                    // if we get here, there is no reason to look at another untimed entry
                    break;
                }
            }
        }

        public Task Remove<T>(bool localOnly, string itemKey, CancellationToken cancel = default(CancellationToken))
        {
            CacheEntry entry;
            _cache.TryRemove(itemKey, out entry);
            // we don't remove the entry from the queue, but that's okay because we'll just ignore that entry when we get to it
            return Task.CompletedTask;
        }

        public Task Clear(bool localOnly = true, CancellationToken cancel = default(CancellationToken))
        {
            _untimedQueue = new ConcurrentQueue<string>();
            _timedQueue = new ConcurrentQueue<TimedQueueEntry>();
            _cache.Clear();
            return Task.CompletedTask;
        }
    }
}