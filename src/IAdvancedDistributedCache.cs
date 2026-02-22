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
namespace Talegen.AspNetCore.AdvancedCache
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Distributed;
    using StackExchange.Redis;

    /// <summary>
    /// This interface provides additional enhancements to the base distributed cache interface.
    /// </summary>
    public interface IAdvancedDistributedCache : IDistributedCache, IDisposable
    {
        /// <summary>
        /// This method is used to find one or more cache keys that match a specified pattern.
        /// </summary>
        /// <param name="pattern">Contains the key search pattern.</param>
        /// <returns>Returns an enumerable list of key names matching the pattern.</returns>
        IEnumerable<string> FindKeys(string pattern);

        /// <summary>
        /// This method is used to find one or more cache keys that match a specified pattern.
        /// </summary>
        /// <param name="pattern">Contains the key search pattern.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns an enumerable list of key names matching the pattern.</returns>
        Task<IEnumerable<string>> FindKeysAsync(string pattern, CancellationToken token = default);

        /// <summary>
        /// Removes all the keys.
        /// </summary>
        /// <param name="keys">Contains the keys to remove.</param>
        /// <returns>Returns the number of keys removed.</returns>
        long RemoveRange(string[] keys);

        /// <summary>
        /// Removes all the keys.
        /// </summary>
        /// <param name="keys">Contains the keys to remove.</param>
        /// <returns>Returns the number of keys removed.</returns>
        Task<long> RemoveRangeAsync(string[] keys, CancellationToken token = default);

        /// <summary>
        /// Removes the value with the given key.
        /// </summary>
        /// <param name="pattern">A string identifying the requested value.</param>
        /// <returns>Returns the number of keys removed.</returns>
        long RemovePattern(string pattern);

        /// <summary>
        /// Removes the value with the given key.
        /// </summary>
        /// <param name="pattern">A string identifying the requested value.</param>
        /// <param name="token">Optional. The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>Returns the number of keys removed.</returns>
        Task<long> RemovePatternAsync(string pattern, CancellationToken token = default);

        /// <summary>
        /// This method is used to check if a field exists in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value fieldName.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a value indicating whether the field exists.</returns>
        Task<bool> HashFieldExistsAsync(string hashKey, string fieldName, CancellationToken token = default);

        /// <summary>
        /// This method is used to get a value in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value field Name.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a string representation of the value.</returns>
        Task<string?> HashGetAsync(string hashKey, string fieldName, CancellationToken token = default);

        /// <summary>
        /// This method is used to get all values in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a dictionary of field and values.</returns>
        Task<Dictionary<string, string>> HashGetAllAsync(string hashKey, CancellationToken token = default);

        /// <summary>
        /// This method is used to set a value in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value field Name.</param>
        /// <param name="value">Contains the value.</param>
        /// <param name="expiration">Contains an optional expiration time.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a value indicating success.</returns>
        Task<bool> HashSetAsync(string hashKey, string fieldName, string value, TimeSpan? expiration = null, CancellationToken token = default);

        /// <summary>
        /// This method is used to delete a field from the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value fieldName.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        Task<bool> HashRemoveAsync(string hashKey, string fieldName, CancellationToken token = default);

        /// <summary>
        /// This method is used to increment a value in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value field Name.</param>
        /// <param name="value">Contains the value to increment by.</param>
        /// <param name="expiration">Contains an optional expiration time.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns the incremented value.</returns>
        Task<long> HashIncrementAsync(string hashKey, string fieldName, long value = 1, TimeSpan? expiration = null, CancellationToken token = default);

        /// <summary>
        /// This method is used to decrement a value in the cache hash bucket.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldName">Contains the value field Name.</param>
        /// <param name="value">Contains the value to decrement by.</param>
        /// <param name="expiration">Contains an optional expiration time.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns the decremented value.</returns>
        Task<long> HashDecrementAsync(string hashKey, string fieldName, long value = 1, TimeSpan? expiration = null, CancellationToken token = default);


        /// <summary>
        /// This method is used to set a key expiration.
        /// </summary>
        /// <param name="hashKey">Contains the key to expire.</param>
        /// <param name="expiration">Contains the timespan for expiration.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a value indicating success.</returns>
        Task<bool> KeyExpireAsync(string hashKey, TimeSpan expiration, CancellationToken token = default);

        /// <summary>
        /// This method is used to set a field expiration.
        /// </summary>
        /// <param name="hashKey">Contains the hash key.</param>
        /// <param name="fieldNames">Contains an array of field names to set expiration.</param>
        /// <param name="expiration">Contains the timespan for expiration.</param>
        /// <param name="token">Contains an optional cancellation token.</param>
        /// <returns>Returns a value indicating success.</returns>
        Task<bool> HashFieldsExpireAsync(string hashKey, string[] fieldNames, TimeSpan expiration, CancellationToken token = default);

        /// <summary>
        /// Attempts to acquire a distributed lock for the specified key with a given expiration time.
        /// </summary>
        /// <remarks>If the lock is already held by another process, the method returns <see
        /// langword="false"/> immediately without waiting. The lock will be automatically released after the specified
        /// expiration time if not released earlier.</remarks>
        /// <param name="key">The unique identifier for the lock to acquire. Cannot be null or empty.</param>
        /// <param name="expirationTime">The duration for which the lock will be held before it expires automatically.</param>
        /// <param name="value">An optional value to associate with the lock. If not provided, a default timestamp value will be used.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the lock acquisition operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the lock was
        /// successfully acquired; otherwise, <see langword="false"/>.</returns>
        Task<bool> TryAcquireLockAsync(string key, TimeSpan expirationTime, string? value = null, CancellationToken cancellationToken = default);
    }
}