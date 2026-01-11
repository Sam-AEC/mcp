using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RevitBridge.Bridge.Performance
{
    /// <summary>
    /// High-performance response cache with TTL and LRU eviction
    /// Reduces redundant API calls for frequently accessed data
    /// </summary>
    public class ResponseCache
    {
        private class CacheEntry
        {
            public object Response { get; set; }
            public DateTime ExpiresAt { get; set; }
            public DateTime LastAccessed { get; set; }
            public int AccessCount { get; set; }
        }

        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly int _maxEntries;
        private readonly TimeSpan _defaultTtl;

        public ResponseCache(int maxEntries = 1000, int defaultTtlSeconds = 60)
        {
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            _maxEntries = maxEntries;
            _defaultTtl = TimeSpan.FromSeconds(defaultTtlSeconds);
        }

        /// <summary>
        /// Get cached response if available and not expired
        /// </summary>
        public bool TryGet(string key, out object response)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow < entry.ExpiresAt)
                {
                    entry.LastAccessed = DateTime.UtcNow;
                    entry.AccessCount++;
                    response = entry.Response;
                    return true;
                }
                else
                {
                    // Expired - remove it
                    _cache.TryRemove(key, out _);
                }
            }

            response = null;
            return false;
        }

        /// <summary>
        /// Store response in cache with optional custom TTL
        /// </summary>
        public void Set(string key, object response, TimeSpan? ttl = null)
        {
            // Evict if we're at capacity
            if (_cache.Count >= _maxEntries)
            {
                EvictLeastRecentlyUsed();
            }

            var entry = new CacheEntry
            {
                Response = response,
                ExpiresAt = DateTime.UtcNow + (ttl ?? _defaultTtl),
                LastAccessed = DateTime.UtcNow,
                AccessCount = 0
            };

            _cache.AddOrUpdate(key, entry, (k, old) => entry);
        }

        /// <summary>
        /// Generate cache key from tool name and payload
        /// </summary>
        public static string GenerateKey(string tool, string payload)
        {
            using (var sha256 = SHA256.Create())
            {
                var combined = $"{tool}:{payload}";
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return $"{tool}:{BitConverter.ToString(hash).Replace("-", "").Substring(0, 16)}";
            }
        }

        /// <summary>
        /// Invalidate all cache entries (e.g., after document modification)
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Invalidate cache entries matching a pattern
        /// </summary>
        public void InvalidatePattern(string pattern)
        {
            var keysToRemove = _cache.Keys
                .Where(k => k.Contains(pattern))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public object GetStats()
        {
            var now = DateTime.UtcNow;
            var entries = _cache.Values.ToList();

            return new
            {
                totalEntries = entries.Count,
                maxEntries = _maxEntries,
                expiredEntries = entries.Count(e => e.ExpiresAt < now),
                totalAccesses = entries.Sum(e => e.AccessCount),
                averageAccessCount = entries.Any() ? entries.Average(e => e.AccessCount) : 0,
                oldestEntry = entries.Any() ? entries.Min(e => e.LastAccessed) : DateTime.MinValue,
                newestEntry = entries.Any() ? entries.Max(e => e.LastAccessed) : DateTime.MinValue
            };
        }

        /// <summary>
        /// Evict least recently used entries when at capacity
        /// </summary>
        private void EvictLeastRecentlyUsed()
        {
            var entriesToEvict = _cache
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(_maxEntries / 10) // Evict 10% at a time
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in entriesToEvict)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }
}
