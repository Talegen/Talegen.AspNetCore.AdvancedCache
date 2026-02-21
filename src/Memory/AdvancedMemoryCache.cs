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
namespace Talegen.AspNetCore.AdvancedCache.Memory
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO.Enumeration;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Distributed;

    /// <summary>
    /// This class implements an advanced memory cache that can be used to store data in memory.
    /// </summary>
    public class AdvancedMemoryCache : IAdvancedDistributedCache
    {

        /// <summary>
        /// Contains the disposed state.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Contains the memory dictionary.
        /// </summary>
        private static ConcurrentDictionary<string, string> memoryDictionary = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// This method is used to find keys that match the specified pattern.
        /// </summary>
        /// <param name="pattern">Contains the search pattern to use.</param>
        /// <returns>Returns an enumerable list of keys found.</returns>
        public IEnumerable<string> FindKeys(string pattern)
        {
            // returns all keys that match the pattern
            return memoryDictionary.Keys.Where(k => FileSystemName.MatchesSimpleExpression(pattern, k));
        }

        /// <summary>
        /// This method is used to find keys that match the specified pattern asynchronously.
        /// </summary>
        /// <param name="pattern">Contains the search pattern to use.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns an enumerable list of keys found.</returns>
        public async Task<IEnumerable<string>> FindKeysAsync(string pattern, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(this.FindKeys(pattern));
        }

        /// <summary>
        /// This method is used to get the value of the specified key.
        /// </summary>
        /// <param name="key">Contains the key to retrieve.</param>
        /// <returns>Returns the value of the key.</returns>
        public byte[]? Get(string key)
        {
            return Encoding.UTF8.GetBytes(memoryDictionary[key]);
        }

        /// <summary>
        /// This method is used to get the value of the specified key asynchronously.
        /// </summary>
        /// <param name="key">Contains the key to retrieve.</param>
        /// <param name="token">Contains a cancellation token.</param>
        /// <returns>Returns the value of the key.</returns>
        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return await Task.FromResult(this.Get(key));
        }

        /// <summary>
        /// This method is used to refresh the specified key.
        /// </summary>
        /// <param name="key">Contains a key.</param>
        /// <exception cref="NotImplementedException">This method is not implemented for this cache type.</exception>
        public void Refresh(string key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is used to refresh a specified key. 
        /// </summary>
        /// <param name="key">Contains the key to retrieve.</param>
        /// <param name="token">Contains a cancellation token.</param>
        /// <returns>Returns a task</returns>
        /// <exception cref="NotImplementedException">This method is not implemented for this cache type.</exception>
        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is used to remove the specified key.
        /// </summary>
        /// <param name="key">Contains the key to remove.</param>
        public void Remove(string key)
        {
            memoryDictionary.TryRemove(key, out _);
        }

        /// <summary>
        /// This method is used to remove the specified key asynchronously.
        /// </summary>
        /// <param name="key">Contains the key to remove.</param>
        /// <param name="token">Contains a cancellation token.</param>
        /// <returns>Returns a task</returns>
        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            this.Remove(key);
            return Task.CompletedTask;
        }

        /// <summary>
        /// This method is used to remove all keys that match the specified pattern.
        /// </summary>
        /// <param name="pattern">Contains a pattern to find and remove.</param>
        /// <returns>Returns the number of keys removed.</returns>
        public long RemovePattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                throw new ArgumentNullException(nameof(pattern));
            }
            // Find all keys that match the pattern and remove them
            var keys = this.FindKeys(pattern);
            return this.RemoveRange(keys.ToArray());
        }

        /// <summary>
        /// This method is used to remove all keys that match the specified pattern asynchronously.
        /// </summary>
        /// <param name="pattern">Contains a pattern to find and remove.</param>
        /// <param name="cancellationToken">Contains a cancellation token.</param>
        /// <returns>Returns the number of keys removed.</returns>
        public async Task<long> RemovePatternAsync(string pattern, CancellationToken token = default)
        {
            return await Task.Run(() => this.RemovePattern(pattern), token);
        }

        /// <summary>
        /// This method is used to remove a range of keys.
        /// </summary>
        /// <param name="keys">Contains the keys to remove.</param>
        public long RemoveRange(string[] keys)
        {
            long removedCount = keys.Length;
            foreach (var key in keys)
            {
                this.Remove(key);
            }
            return removedCount;
        }

        /// <summary>
        /// This method is used to remove a range of keys asynchronously.
        /// </summary>
        /// <param name="keys">Contains the keys to remove.</param>
        /// <param name="token">Contains a cancellation token.</param>
        /// <returns>Returns the number of keys removed.</returns>
        public async Task<long> RemoveRangeAsync(string[] keys, CancellationToken token = default)
        {
            return await Task.Run(() => this.RemoveRange(keys), token);
        }

        /// <summary>
        /// This method is used to set the specified key with the specified value.
        /// </summary>
        /// <param name="key">Contains the key to set.</param>
        /// <param name="value">Contains the value to set.</param>
        /// <param name="options">Contains the cache entry options.</param>
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            memoryDictionary[key] = Encoding.UTF8.GetString(value);
        }

        /// <summary>
        /// This method is used to set the specified key with the specified value asynchronously.
        /// </summary>
        /// <param name="key">Contains the key to set.</param>
        /// <param name="value">Contains the value to set.</param>
        /// <param name="options">Contains the cache entry options.</param>
        /// <param name="token">Contains a cancellation token.</param>
        /// <returns>Returns a task.</returns>
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            this.Set(key, value, options);
            return Task.CompletedTask;
        }

        /// <summary>
        /// This method is used to check if a field exists in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value fieldName.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a value indicating whether the field exists.</returns>
        public async Task<bool> HashFieldExistsAsync(string hashKey, string fieldName, CancellationToken token = default)
        {
            if (hashKey == null)
            {
                throw new ArgumentNullException(nameof(hashKey));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            bool result = false;
            
            if (memoryDictionary.ContainsKey(hashKey))
            {
                string hashValue = memoryDictionary[hashKey];
                result = hashValue.Contains(fieldName);
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// This method is used to get a value in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value field Name.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a string representation of the value.</returns>
        public async Task<string?> HashGetAsync(string hashKey, string fieldName, CancellationToken token = default)
        {
            string result = string.Empty;
            if (memoryDictionary.ContainsKey(hashKey))
            {
                string hashValue = memoryDictionary[hashKey];
                if (hashValue.Contains(fieldName))
                {
                    result = hashValue;
                }
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// This method is used to get all values in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a dictionary of field and values.</returns>
        public async Task<Dictionary<string, string>> HashGetAllAsync(string hashKey, CancellationToken token = default)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            
            if (memoryDictionary.ContainsKey(hashKey))
            {
                string hashValue = memoryDictionary[hashKey];
                result.Add(hashKey, hashValue);
            }
            
            return await Task.FromResult(result);
        }

        /// <summary>
        /// This method is used to delete a field from the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value fieldName.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">Thrown if parameter are null.</exception>
        public async Task<bool> HashRemoveAsync(string hashKey, string fieldName, CancellationToken token = default)
        {
            bool result = memoryDictionary.ContainsKey(hashKey) && memoryDictionary[hashKey].Contains(fieldName);
            
            if (result)
            {
                memoryDictionary.TryRemove(hashKey, out _); // Remove the hash key if it exists
            }
            
            return await Task.FromResult(result);
        }

        /// <summary>
        /// This method is used to set a value in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value field Name.</param>
        /// <param name="value">Contains the value.</param>
        /// <param name="expiration">Contains an optional expiration time.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns teh value set.</returns>
        public async Task<bool> HashSetAsync(string hashKey, string fieldName, string value, TimeSpan? expiration = null, CancellationToken token = default)
        {
            bool result = false;
            
            if (memoryDictionary.ContainsKey(hashKey))
            {
                string hashValue = memoryDictionary[hashKey];
                if (hashValue.Contains(fieldName))
                {
                    memoryDictionary[hashKey] = value;
                    result = true;
                }
            }
         
            return await Task.FromResult(result);
        }

        /// <summary>
        /// This method is used to increment a value in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value field Name.</param>
        /// <param name="value">Contains the value to increment by.</param>
        /// <param name="expiration">Contains an optional expiration time.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns the incremented value.</returns>
        public Task<long> HashIncrementAsync(string hashKey, string fieldName, long value = 1, TimeSpan? expiration = null, CancellationToken token = default)
        {
            long result = 0;
            if (memoryDictionary.ContainsKey(hashKey))
            {
                string hashValue = memoryDictionary[hashKey];
                if (hashValue.Contains(fieldName))
                {
                    long.TryParse(hashValue, out result);
                    result += value;
                    memoryDictionary[hashKey] = result.ToString();
                }
            }
            return Task.FromResult(result);
        }

        /// <summary>
        /// This method is used to decrement a value in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value field Name.</param>
        /// <param name="value">Contains the value to decrement by.</param>
        /// <param name="expiration">Contains an optional expiration time.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns the decremented value.</returns>
        public async Task<long> HashDecrementAsync(string hashKey, string fieldName, long value = 1, TimeSpan? expiration = null, CancellationToken token = default)
        {
            long result = 0;
            if (memoryDictionary.ContainsKey(hashKey))
            {
                string hashValue = memoryDictionary[hashKey];
                if (hashValue.Contains(fieldName))
                {
                    long.TryParse(hashValue, out result);
                    result -= value;
                    memoryDictionary[hashKey] = result.ToString();
                }
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// This method is used to set a key expiration.
        /// </summary>
        /// <param name="hashKey">Contains the key to expire.</param>
        /// <param name="expiration">Contains the timespan for expiration.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a value indicating success.</returns>
        public async Task<bool> KeyExpireAsync(string hashKey, TimeSpan expiration, CancellationToken token = default)
        {
            bool result = false;
            if (memoryDictionary.ContainsKey(hashKey))
            {
                memoryDictionary[hashKey] = memoryDictionary[hashKey];
                result = true;
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// This method is used to set a field expiration.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldNames">Contains the field names.</param>
        /// <param name="expiration">Contains the timespan for expiration.</param>
        /// <param name="token"
        /// <returns>Returns a value indicating success.</returns>
        public async Task<bool> HashFieldsExpireAsync(string hashKey, string[] fieldNames, TimeSpan expiration, CancellationToken token = default)
        {
            bool result = false;
            if (hashKey == null)
            {
                throw new ArgumentNullException(nameof(hashKey));
            }
            if (fieldNames == null || fieldNames.Length == 0)
            {
                throw new ArgumentNullException(nameof(fieldNames));
            }

            if (memoryDictionary.ContainsKey(hashKey))
            {
                string hashValue = memoryDictionary[hashKey];
                foreach (var fieldName in fieldNames)
                {
                    if (hashValue.Contains(fieldName))
                    {
                        memoryDictionary[hashKey] = hashValue;
                        result = true;
                    }
                }
            }
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Attempts to acquire a lock for the specified key with a given expiration time, using an asynchronous
        /// operation.
        /// </summary>
        /// <param name="key">The unique identifier for the lock to acquire. Cannot be null.</param>
        /// <param name="expirationTime">The duration for which the lock should be held before it expires.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the lock was
        /// successfully acquired; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null.</exception>
        public async Task<bool> LockAsync(string key, TimeSpan expirationTime, CancellationToken cancellationToken = default)
        {
            bool result = false;
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (!memoryDictionary.ContainsKey(key))
            {
                memoryDictionary[key] = "LOCKED";
                result = true;
            }
            return await Task.FromResult(result);
        }

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
            if (!this.disposed && disposing)
            {
            }

            this.disposed = true;
        }

        #endregion
    }
}
