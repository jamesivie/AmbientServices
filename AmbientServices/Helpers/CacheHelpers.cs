﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AmbientServices
{
    /// <summary>
    /// A class that provides caching using either a specified cache or the ambient cache.
    /// </summary>
    /// <typeparam name="TOWNER">The type that owns the items to be cached.</typeparam>
    public class AmbientCache<TOWNER>
    {
        private static readonly string DefaultCacheKeyPrefix = typeof(TOWNER).Name + "-";
        private static readonly AmbientService<IAmbientCache> _Cache = Ambient.GetService<IAmbientCache>();

        private IAmbientCache? _explicitCache;
        private string _cacheKeyPrefix = DefaultCacheKeyPrefix;

        /// <summary>
        /// Creates the AmbientCache using the ambient cache service.
        /// </summary>
        /// <param name="cacheKeyPrefix">An optional cache key prefix for all items cached through this class.  Uses the type name if not specified.</param>
        public AmbientCache(string? cacheKeyPrefix = null)
            : this (null, cacheKeyPrefix)
        {
        }
        /// <summary>
        /// Creates the AmbientCache using the specified cache service.
        /// </summary>
        /// <param name="cache">An explicit <see cref="IAmbientCache"/> to use.</param>
        /// <param name="cacheKeyPrefix">An optional cache key prefix for all items cached through this class.  Uses the type name if not specified.</param>
        public AmbientCache(IAmbientCache? cache, string? cacheKeyPrefix = null)
        {
            _explicitCache = cache;
            if (cacheKeyPrefix != null) _cacheKeyPrefix = cacheKeyPrefix;
        }
        /// <summary>
        /// Retrieves the item with the specified key from the cache (if possible).
        /// </summary>
        /// <typeparam name="T">The type of the cached object.</typeparam>
        /// <param name="itemKey">The unique key used when the object was cached.</param>
        /// <param name="refresh">An optional <see cref="TimeSpan"/> indicating the length of time to extend the lifespan of the cached item.  Defaults to null, meaning not to update the expiration time.  Some cache implementations may ignore this value.</param>
        /// <param name="cancel">The optional <see cref="CancellationToken"/>.</param>
        /// <returns>The cached object, or null if it was not found in the cache.</returns>
        public Task<T?> Retrieve<T>(string itemKey, TimeSpan? refresh = null, CancellationToken cancel = default(CancellationToken)) where T : class
        {
            IAmbientCache? cache = _explicitCache ?? _Cache.Local;
            if (cache == null) return Task.FromResult<T?>(null);
            return cache.Retrieve<T>(_cacheKeyPrefix + itemKey, refresh, cancel);
        }
        /// <summary>
        /// Stores the specified item in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the item to be cached.</typeparam>
        /// <param name="localOnly">Whether or not this item should only be stored in the local cache (as opposed to a nonlocal shared cache).  When true, only the local cache will be checked.  When false, the local cache will be checked first, followed by the shared cache.</param>
        /// <param name="itemKey">A string that uniquely identifies the item being cached.</param>
        /// <param name="item">The item to be cached.</param>
        /// <param name="maxCacheDuration">An optional <see cref="TimeSpan"/> indicating the maximum amount of time to keep the item in the cache.</param>
        /// <param name="expiration">An optional <see cref="DateTime"/> indicating a fixed time for when the item should expire from the cache.</param>
        /// <param name="cancel">The optional <see cref="CancellationToken"/>.</param>
        /// <remarks>
        /// If both <paramref name="expiration"/> and <paramref name="maxCacheDuration"/> are set, the earlier expiration will be used.
        /// </remarks>
        public Task Store<T>(bool localOnly, string itemKey, T item, TimeSpan? maxCacheDuration = null, DateTime? expiration = null, CancellationToken cancel = default(CancellationToken)) where T : class
        {
            IAmbientCache? cache = _explicitCache ?? _Cache.Local;
            if (cache == null) return Task.CompletedTask;
            return cache.Store<T>(localOnly, _cacheKeyPrefix + itemKey, item, maxCacheDuration, expiration, cancel);
        }
        /// <summary>
        /// Removes the specified item from the cache.
        /// </summary>
        /// <typeparam name="T">The type of the item to be cached.</typeparam>
        /// <param name="localOnly">Whether or not this item should only be removed from the local cache (as opposed to a nonlocal shared cache).  When true, only the local cache will be checked.  When false, the local cache will be checked first, followed by the shared cache.</param>
        /// <param name="itemKey">A string that uniquely identifies the item being cached.</param>
        /// <param name="cancel">The optional <see cref="CancellationToken"/>.</param>
        public Task Remove<T>(bool localOnly, string itemKey, CancellationToken cancel = default(CancellationToken))
        {
            IAmbientCache? cache = _explicitCache ?? _Cache.Local;
            if (cache == null) return Task.CompletedTask;
            return cache.Remove<T>(localOnly, _cacheKeyPrefix + itemKey, cancel);
        }
        /// <summary>
        /// Flushes everything from the cache.
        /// </summary>
        /// <param name="localOnly">Whether or not to clear only the local cache.</param>
        /// <param name="cancel">The optional <see cref="CancellationToken"/>.</param>
        public Task Clear(bool localOnly = true, CancellationToken cancel = default(CancellationToken))
        {
            IAmbientCache? cache = _explicitCache ?? _Cache.Local;
            if (cache == null) return Task.CompletedTask;
            return cache.Clear(localOnly, cancel);
        }
    }
}
