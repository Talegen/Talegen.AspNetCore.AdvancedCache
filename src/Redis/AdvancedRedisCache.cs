/*
 * Talegen ASP.net Advanced Cache Library
 * (c) Copyright Talegen, LLC.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
*/
namespace Talegen.AspNetCore.AdvancedCache.Redis
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Options;
    using StackExchange.Redis;

    /// <summary>
    /// This class implements an implementation of IDistributedCache for the Redis cache server.
    /// </summary>
    public class AdvancedRedisCache : IAdvancedDistributedCache
    {
        #region Private Constants

        /// <summary>
        /// </summary>
        /// <remarks>
        /// Below is the command and formatting of commands to send to update the hash ----- KEYS[1] = = key ARGV[1] = absolute-expiration - ticks as long (-1
        /// for none) ARGV[2] = sliding-expiration - ticks as long (-1 for none) ARGV[3] = relative-expiration (long, in seconds, -1 for none) -
        /// Min(absolute-expiration - Now, sliding-expiration) ARGV[4] = data - byte[]
        /// NOTE: this order should not change LUA script depends on it
        /// </remarks>
        private const string SetScript = (@"
                redis.call('HMSET', KEYS[1], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', ARGV[4])
                if ARGV[3] ~= '-1' then
                  redis.call('EXPIRE', KEYS[1], ARGV[3])
                end
                return 1");

        /// <summary>
        /// Contains the absolute expiration key for the hash.
        /// </summary>
        private const string AbsoluteExpirationKey = "absexp";

        /// <summary>
        /// Contains the sliding expiration key for the hash.
        /// </summary>
        private const string SlidingExpirationKey = "sldexp";

        /// <summary>
        /// Contains the data key for the hash.
        /// </summary>
        private const string DataKey = "data";

        /// <summary>
        /// Contains a not present value.
        /// </summary>
        private const long NotPresent = -1;

        #endregion

        #region Private Fields

        /// <summary>
        /// Contains the disposed state.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Contains the Redis connection multiplexer.
        /// </summary>
        /// <remarks>Setting as volatile denotes the connection may be accessed by multiple threads.</remarks>
        private volatile ConnectionMultiplexer connection;

        /// <summary>
        /// Contains an instance of the Redis database cache.
        /// </summary>
        private IDatabase cache;

        /// <summary>
        /// Contains the Redis client cache options.
        /// </summary>
        private readonly RedisClientCacheOptions options;

        /// <summary>
        /// Contains the Redis instance name.
        /// </summary>
        private readonly string instance;

        /// <summary>
        /// Contains a lock used for interaction with the connection object.
        /// </summary>
        private readonly SemaphoreSlim connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvancedRedisCache" /> class.
        /// </summary>
        /// <param name="optionsAccessor">Contains the Redis client cache options accessor.</param>
        /// <param name="minSupportedCommands">Contains the minimum supported server commands.</param>
        public AdvancedRedisCache(IOptions<RedisClientCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            this.options = optionsAccessor.Value;

            // This allows partitioning a single backend cache for use with multiple apps/services.
            this.instance = this.options.InstanceName ?? string.Empty;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the internal connection multiplexer.
        /// </summary>
        public ConnectionMultiplexer Connection => this.connection;

        /// <summary>
        /// Gets the internal cache database.
        /// </summary>
        public IDatabase Cache => this.cache;

        /// <summary>
        /// Gets a complete connected state for the Redis client.
        /// </summary>
        public bool IsConnected => this.cache != null && this.connection != null && this.connection.IsConnected;

        #endregion

        #region Public IDistributedCache Methods

        /// <summary>
        /// This method gets a byte array of data from the cache.
        /// </summary>
        /// <param name="key">Contains the key to find.</param>
        /// <returns>Returns the data found.</returns>
        public byte[] Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return this.GetAndRefresh(key, getData: true);
        }

        //// <summary>
        /// This method gets a byte array of data from the cache.
        /// </summary>
        /// <param name="key">Contains the key to find.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns></returns>
        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            return await this.GetAndRefreshAsync(key, getData: true, token: token);
        }

        /// <summary>
        /// This method sets a byte array of data to the cache.
        /// </summary>
        /// <param name="key">Contains the key to set.</param>
        /// <param name="value">Contains the byte data to set.</param>
        /// <param name="options">Contains the cache entry options.</param>
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Connect();

            DateTimeOffset creationTime = DateTimeOffset.UtcNow;
            DateTimeOffset? absoluteExpiration = GetAbsoluteExpiration(creationTime, options);
            this.Cache.ScriptEvaluate(SetScript, new RedisKey[] { this.instance + key },
                new RedisValue[]
                {
                        absoluteExpiration?.Ticks ?? NotPresent,
                        options.SlidingExpiration?.Ticks ?? NotPresent,
                        GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
                        value
                });
        }

        /// <summary>
        /// This method sets a byte array of data to the cache.
        /// </summary>
        /// <param name="key">Contains the key to set.</param>
        /// <param name="value">Contains the byte data to set.</param>
        /// <param name="options">Contains the cache entry options.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a task.</returns>
        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            token.ThrowIfCancellationRequested();

            await this.ConnectAsync(token);

            DateTimeOffset creationTime = DateTimeOffset.UtcNow;
            DateTimeOffset? absoluteExpiration = GetAbsoluteExpiration(creationTime, options);

            await this.Cache.ScriptEvaluateAsync(SetScript, new RedisKey[] { this.instance + key },
                new RedisValue[]
                {
                        absoluteExpiration?.Ticks ?? NotPresent,
                        options.SlidingExpiration?.Ticks ?? NotPresent,
                        GetExpirationInSeconds(creationTime, absoluteExpiration, options) ?? NotPresent,
                        value
                });
        }

        /// <summary>
        /// This method is used to simply reset the expiration date for the cache item.
        /// </summary>
        /// <param name="key">Contains the key to find and refresh.</param>
        public void Refresh(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            this.GetAndRefresh(key, getData: false);
        }

        /// <summary>
        /// This method is used to simply reset the expiration date for the cache item.
        /// </summary>
        /// <param name="key">Contains the key to find and refresh.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a task.</returns>
        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await this.GetAndRefreshAsync(key, getData: false, token: token);
        }

        /// <summary>
        /// This method is used to remove a key from cache.
        /// </summary>
        /// <param name="key">Contains the key to remove.</param>
        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            this.Connect();

            this.Cache.KeyDelete(this.instance + key);
        }

        /// <summary>
        /// This method is used to remove a key from cache.
        /// </summary>
        /// <param name="key">Contains the key to remove.</param>
        /// <param name="token">Contains optional cancellation token.</param>
        /// <returns>Returns a task.</returns>
        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            await this.ConnectAsync(token);

            await this.Cache.KeyDeleteAsync(this.instance + key);
        }

        #endregion

        #region Public IAdvancedDistributedCache

        /// <summary>
        /// This method is used to find one or more cache keys that match a specified pattern.
        /// </summary>
        /// <param name="cache">Contains the cache database to search.</param>
        /// <param name="pattern">Contains the key search pattern.</param>
        /// <returns>Returns an enumerable list of key names matching the pattern.</returns>
        public IEnumerable<string> FindKeys(string pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            this.Connect();
            
            return this.Cache.FindKeys(pattern);
        }

        /// <summary>
        /// This method is used to find one or more cache keys that match a specified pattern.
        /// </summary>
        /// <param name="pattern">Contains the key search pattern.</param>
        /// <param name="cancellationToken">Contains an optional cancellation token.</param>
        /// <returns>Returns an enumerable list of key names matching the pattern.</returns>
        public async Task<IEnumerable<string>> FindKeysAsync(string pattern, CancellationToken cancellationToken = default)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            await this.ConnectAsync().ConfigureAwait(false);

            return await this.Cache.FindKeysAsync(pattern, token: cancellationToken).ConfigureAwait(false);
        }


        /// <summary>
        /// Removes all the keys.
        /// </summary>
        /// <param name="keys">Contains the keys to remove.</param>
        public void RemoveRange(string[] keys)
        {
            if (keys != null)
            {
                RedisKey[] redisKeys = new RedisKey[keys.Count()];
                int i = 0;
                foreach (var key in keys)
                {
                    redisKeys[i++] = new RedisKey(key);
                }

                if (redisKeys.Length > 0)
                {
                    this.Cache.KeyDelete(redisKeys);
                }
            }
        }

        /// <summary>
        /// Removes all the keys.
        /// </summary>
        /// <param name="keys">Contains the keys to remove.</param>
        public async Task RemoveRangeAsync(string[] keys)
        {
            if (keys != null)
            {
                RedisKey[] redisKeys = new RedisKey[keys.Count()];
                int i = 0;
                foreach (var key in keys)
                {
                    redisKeys[i++] = new RedisKey(key);
                }

                if (redisKeys.Length > 0)
                {
                    await this.Cache.KeyDeleteAsync(redisKeys);
                }
            }
        }

        /// <summary>
        /// Removes the value with the given key.
        /// </summary>
        /// <param name="pattern">A string identifying the requested value.</param>
        public void RemovePattern(string pattern)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            this.Connect();

            var keys = this.Cache.FindKeys(pattern);

            this.RemoveRange(keys.ToArray());
        }

        /// <summary>
        /// Removes the value with the given key.
        /// </summary>
        /// <param name="pattern">A string identifying the requested value.</param>
        /// <param name="cancellationToken">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async Task RemovePatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            await this.ConnectAsync();

            var keys = await this.Cache.FindKeysAsync(pattern);

            await this.RemoveRangeAsync(keys.ToArray());
        }


        /// <summary>
        /// This method is used to get a value in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value fieldName.</param>
        /// <returns>Returns a string representation of the value.</returns>
        public async Task<string> HashGetAsync(string hashKey, string fieldName)
        {
            if (hashKey == null)
            {
                throw new ArgumentNullException(nameof(hashKey));
            }
            
            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            string? result = null;

            await this.ConnectAsync();

            RedisValue redisValue = await this.Cache.HashGetAsync(hashKey, fieldName);

            if (!redisValue.IsNullOrEmpty)
            {
                result = Encoding.UTF8.GetString(redisValue);
            }

            return result;
        }

        /// <summary>
        /// This method is used to set a value in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value key.</param>
        /// <param name="value">Contains the value.</param>
        /// <returns>Returns teh value set.</returns>
        public async Task<bool> HashSetAsync(string hashKey, string fieldName, string value, TimeSpan? expiration = null)
        {
            if (hashKey == null)
            {
                throw new ArgumentNullException(nameof(hashKey));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (expiration == null)
            {
                expiration = TimeSpan.FromHours(1);
            }

            await this.ConnectAsync();
            bool result = await this.Cache.HashSetAsync(hashKey, fieldName, value);
            
            // if this was new, set the expiration
            if (result)
            {
                await this.Cache.KeyExpireAsync(hashKey, expiration);
            }

            return result;
        }

        /// <summary>
        /// This method is used to increment a value in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value field Name.</param>
        /// <param name="value">Contains the value to increment by.</param>
        /// <param name="expiration">Contains an optional expiration time.</param>
        /// <returns>Returns the incremented value.</returns>
        public async Task<long> HashIncrementAsync(string hashKey, string fieldName, long value = 1, TimeSpan? expiration = null)
        {
            if (hashKey == null)
            {
                throw new ArgumentNullException(nameof(hashKey));
            }
            
            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            await this.ConnectAsync();

            var result = await this.Cache.HashIncrementAsync(hashKey, fieldName, value);

            if (expiration.HasValue && this.options.MinimumServerCommands == RedisMinimumServerCommands.SevenTwo)
            {
                await this.HashFieldsExpireAsync(hashKey, new string[] { fieldName }, expiration.Value);
            }

            return result;
        }

        /// <summary>
        /// This method is used to decrement a value in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value field Name.</param>
        /// <param name="value">Contains the value to decrement by.</param>
        /// <param name="expiration">Contains an optional expiration time.</param>
        /// <returns>Returns the decremented value.</returns>
        public async Task<long> HashDecrementAsync(string hashKey, string fieldName, long value = 1, TimeSpan? expiration = null)
        {
            if (hashKey == null)
            {
                throw new ArgumentNullException(nameof(hashKey));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            await this.ConnectAsync();

            var result = await this.Cache.HashDecrementAsync(hashKey, fieldName, value);

            if (expiration.HasValue && this.options.MinimumServerCommands == RedisMinimumServerCommands.SevenTwo)
            {
                await this.HashFieldsExpireAsync(hashKey, new string[] { fieldName }, expiration.Value);
            }

            return result;
        }

        /// <summary>
        /// This method is used to get all values in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key</param>
        /// <returns>Returns a dictionary of field and values.</returns>
        public async Task<Dictionary<string, string>> HashGetAllAsync(string hashKey)
        {
            if (hashKey == null)
            {
                throw new ArgumentNullException(nameof(hashKey));
            }
            await this.ConnectAsync();
            Dictionary<string, string> result = new Dictionary<string, string>();

            HashEntry[] entries = await this.Cache.HashGetAllAsync(hashKey);

            if (entries != null && entries.Length > 0)
            {
                result = entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
            }
            
            return result;
        }

        /// <summary>
        /// This method is used to set a key expiration.
        /// </summary>
        /// <param name="hashKey">Contains the key to expire.</param>
        /// <param name="expiration">Contains the timespan for expiration.</param>
        /// <returns>Returns a value indicating success.</returns>
        public async Task<bool> KeyExpireAsync(string hashKey, TimeSpan expiration)
        {
            if (hashKey == null)
            {
                throw new ArgumentNullException(nameof(hashKey));
            }
            await this.ConnectAsync();
            return await this.Cache.KeyExpireAsync(hashKey, expiration);
        }

        /// <summary>
        /// This method is used to set a field expiration.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the field name.</param>
        /// <param name="expiration">Contains the timespan for expiration.</param>
        /// <returns>Returns a value indicating success.</returns>
        /// <exception cref="NotSupportedException">HashFieldExpireAsync is not supported on Redis versions less than 7.2.</exception>"
        public async Task<bool> HashFieldsExpireAsync(string hashKey, string[] fieldNames, TimeSpan expiration)
        {
            if (hashKey == null)
            {
                throw new ArgumentNullException(nameof(hashKey));
            }
            if (fieldNames == null)
            {
                throw new ArgumentNullException(nameof(fieldNames));
            }

            bool result = false;

            if (this.options.MinimumServerCommands == RedisMinimumServerCommands.SevenTwo)
            {
                await this.ConnectAsync();
                RedisValue[] redisFieldNames = new RedisValue[fieldNames.Length];
                for (int i = 0; i < fieldNames.Length; i++)
                {
                    redisFieldNames[i] = fieldNames[i];
                }

                var results = await this.Cache.HashFieldExpireAsync(hashKey, redisFieldNames, expiration);
                result = results?.Length > 0 && results[0] > 0;
            }
            else
            {
                throw new NotSupportedException("HashFieldExpireAsync is not supported on Redis versions less than 7.2.");
            }

            return result;
        }
        #endregion

        #region IDisposeable Methods

            /// <summary>
            /// This method is used to dispose of the internal disposable objects.
            /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This method implements object disposal.
        /// </summary>
        /// <param name="disposing">Contains a value indicating whether the disposal is occurring.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed && disposing && this.connection != null)
            {
                this.connection.Close();
            }

            this.disposed = true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This method is used to get a expiration in seconds.
        /// </summary>
        /// <param name="creationTime">Contains the creation time.</param>
        /// <param name="absoluteExpiration">Contains the absolute expiration.</param>
        /// <param name="options">Contains cache entry options.</param>
        /// <returns>Returns a nullable value in seconds.</returns>
        private static long? GetExpirationInSeconds(DateTimeOffset creationTime, DateTimeOffset? absoluteExpiration, DistributedCacheEntryOptions options)
        {
            long? result = null;

            if (absoluteExpiration.HasValue && options.SlidingExpiration.HasValue)
            {
                result = (long)Math.Min((absoluteExpiration.Value - creationTime).TotalSeconds, options.SlidingExpiration.Value.TotalSeconds);
            }
            else if (absoluteExpiration.HasValue)
            {
                result = (long)(absoluteExpiration.Value - creationTime).TotalSeconds;
            }
            else if (options.SlidingExpiration.HasValue)
            {
                result = (long)options.SlidingExpiration.Value.TotalSeconds;
            }

            return result;
        }

        /// <summary>
        /// This method is used to get the absolute expiration from a set of options.
        /// </summary>
        /// <param name="creationTime">Contains the creation time.</param>
        /// <param name="options">Contains the distributed cache entry options.</param>
        /// <returns>Returns the date time offset if calculated.</returns>
        private static DateTimeOffset? GetAbsoluteExpiration(DateTimeOffset creationTime, DistributedCacheEntryOptions options)
        {
            if (options.AbsoluteExpiration.HasValue && options.AbsoluteExpiration <= creationTime)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(DistributedCacheEntryOptions.AbsoluteExpiration),
                    options.AbsoluteExpiration.Value,
                    "The absolute expiration value must be in the future.");
            }

            DateTimeOffset? absoluteExpiration = options.AbsoluteExpiration;

            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                absoluteExpiration = creationTime + options.AbsoluteExpirationRelativeToNow;
            }

            return absoluteExpiration;
        }

        /// <summary>
        /// This method is used to create the cache connection.
        /// </summary>
        private void Connect()
        {
            // if not already created...
            if (this.cache == null)
            {
                // lock connection
                this.connectionLock.Wait();

                try
                {
                    if (this.options.ConfigurationOptions != null)
                    {
                        this.connection = ConnectionMultiplexer.Connect(this.options.ConfigurationOptions);
                    }
                    else
                    {
                        this.connection = ConnectionMultiplexer.Connect(this.options.Configuration);
                    }

                    this.cache = this.connection.GetDatabase();
                }
                finally
                {
                    this.connectionLock.Release();
                }
            }
        }

        /// <summary>
        /// This method is used to create the cache connection.
        /// </summary>
        /// <param name="token">Contains an optional token.</param>
        /// <returns>Returns a task.</returns>
        private async Task ConnectAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            // if cache is not connected...
            if (this.cache == null)
            {
                // lock connection
                await this.connectionLock.WaitAsync(token);

                try
                {
                    if (this.options.ConfigurationOptions != null)
                    {
                        this.connection = await ConnectionMultiplexer.ConnectAsync(this.options.ConfigurationOptions);
                    }
                    else
                    {
                        this.connection = await ConnectionMultiplexer.ConnectAsync(this.options.Configuration);
                    }

                    this.cache = this.connection.GetDatabase();
                }
                finally
                {
                    this.connectionLock.Release();
                }
            }
        }

        /// <summary>
        /// This method is used to get and refresh the cache hash expiration information.
        /// </summary>
        /// <param name="key">Contains the key to find.</param>
        /// <param name="getData">Contains a value indicating whether the data is to be returned.</param>
        /// <returns>Returns a byte array of the data found.</returns>
        private byte[] GetAndRefresh(string key, bool getData)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            this.Connect();

            // This also resets the LRU status as desired.
            RedisValue[] results;

            if (getData)
            {
                results = this.cache.HashMemberGet(this.instance + key, AbsoluteExpirationKey, SlidingExpirationKey, DataKey);
            }
            else
            {
                results = this.cache.HashMemberGet(this.instance + key, AbsoluteExpirationKey, SlidingExpirationKey);
            }

            if (results.Length >= 2)
            {
                this.MapMetadata(results, out DateTimeOffset? absExpr, out TimeSpan? sldExpr);
                this.Refresh(key, absExpr, sldExpr);
            }

            if (results.Length >= 3 && results[2].HasValue)
            {
                return results[2];
            }

            return null;
        }

        /// <summary>
        /// This method is used to get and refresh the cache hash expiration information.
        /// </summary>
        /// <param name="key">Contains the key to find.</param>
        /// <param name="getData">Contains a value indicating whether the data is to be returned.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a byte array of the data found.</returns>
        private async Task<byte[]> GetAndRefreshAsync(string key, bool getData, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            await this.ConnectAsync(token);

            // This also resets the LRU status as desired.
            RedisValue[] results;

            if (getData)
            {
                results = await this.cache.HashMemberGetAsync(this.instance + key, AbsoluteExpirationKey, SlidingExpirationKey, DataKey);
            }
            else
            {
                results = await this.cache.HashMemberGetAsync(this.instance + key, AbsoluteExpirationKey, SlidingExpirationKey);
            }

            if (results.Length >= 2)
            {
                this.MapMetadata(results, out DateTimeOffset? absExpr, out TimeSpan? sldExpr);
                await this.RefreshAsync(key, absExpr, sldExpr, token);
            }

            if (results.Length >= 3 && results[2].HasValue)
            {
                return results[2];
            }

            return null;
        }

        /// <summary>
        /// This method is used to map hash metadata to the specified redis value.
        /// </summary>
        /// <param name="results">Contains the results to map.</param>
        /// <param name="absoluteExpiration">Contains the absolute expiration value to map.</param>
        /// <param name="slidingExpiration">Contains the sliding expiration value to map.</param>
        private void MapMetadata(RedisValue[] results, out DateTimeOffset? absoluteExpiration, out TimeSpan? slidingExpiration)
        {
            absoluteExpiration = null;
            slidingExpiration = null;
            long? absoluteExpirationTicks = (long?)results[0];

            if (absoluteExpirationTicks.HasValue && absoluteExpirationTicks.Value != NotPresent)
            {
                absoluteExpiration = new DateTimeOffset(absoluteExpirationTicks.Value, TimeSpan.Zero);
            }

            long? slidingExpirationTicks = (long?)results[1];

            if (slidingExpirationTicks.HasValue && slidingExpirationTicks.Value != NotPresent)
            {
                slidingExpiration = new TimeSpan(slidingExpirationTicks.Value);
            }
        }

        /// <summary>
        /// This method is used to refresh the specified cache key updating the hash expiration metadata.
        /// </summary>
        /// <param name="key">Contains the key to refresh.</param>
        /// <param name="absoluteExpiration">Contains the absolute expiration date.</param>
        /// <param name="slidingExpiration">Contains the sliding expiration date.</param>
        private void Refresh(string key, DateTimeOffset? absoluteExpiration, TimeSpan? slidingExpiration)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (slidingExpiration.HasValue)
            {
                TimeSpan? expiration;

                // Note Refresh has no effect if there is just an absolute expiration (or neither).
                if (absoluteExpiration.HasValue)
                {
                    TimeSpan relativeExpiration = absoluteExpiration.Value - DateTimeOffset.Now;
                    expiration = relativeExpiration <= slidingExpiration.Value ? relativeExpiration : slidingExpiration;
                }
                else
                {
                    expiration = slidingExpiration;
                }

                this.cache.KeyExpire(this.instance + key, expiration);
            }
        }

        /// <summary>
        /// This method is used to refresh the specified cache key updating the hash expiration metadata.
        /// </summary>
        /// <param name="key">Contains the key to refresh.</param>
        /// <param name="absoluteExpiration">Contains the absolute expiration date.</param>
        /// <param name="slidingExpiration">Contains the sliding expiration date.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a task.</returns>
        private async Task RefreshAsync(string key, DateTimeOffset? absoluteExpiration, TimeSpan? slidingExpiration, CancellationToken token = default)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            token.ThrowIfCancellationRequested();

            if (slidingExpiration.HasValue)
            {
                TimeSpan? expiration;

                // Note Refresh has no effect if there is just an absolute expiration (or neither).
                if (absoluteExpiration.HasValue)
                {
                    TimeSpan relativeExpiration = absoluteExpiration.Value - DateTimeOffset.Now;
                    expiration = relativeExpiration <= slidingExpiration.Value ? relativeExpiration : slidingExpiration;
                }
                else
                {
                    expiration = slidingExpiration;
                }

                await this.cache.KeyExpireAsync(this.instance + key, expiration);
            }
        }

        #endregion
    }
}