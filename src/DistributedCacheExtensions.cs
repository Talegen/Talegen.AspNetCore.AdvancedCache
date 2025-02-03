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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Distributed;
    using Newtonsoft.Json;

    /// <summary>
    /// This class is used to define distributed cache extensions.
    /// </summary>
    public static class DistributedCacheExtensions
    {
        /// <summary>
        /// Contains the serializer options.
        /// </summary>
        private static JsonSerializerSettings serializerOptions = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
        };

        /// <summary>
        /// Initializes static members of the <see cref="DistributedCacheExtensions"/> class.
        /// </summary>
        static DistributedCacheExtensions()
        {
            serializerOptions.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        }

        /// <summary>
        /// This method is used to set a value in the cache.
        /// </summary>
        /// <typeparam name="T">Contains the type of the object to set.</typeparam>
        /// <param name="cache">Contains the distributed cache.</param>
        /// <param name="key">Contains the key to set.</param>
        /// <param name="value">Contains the value to set.</param>
        /// <param name="cacheOptions">Contains optional cache time options.</param>
        /// <param name="cancellationToken">Contains an optional cancellation token.</param>
        /// <returns>Returns a task.</returns>
        public static Task SetAsync<T>(this IDistributedCache cache, string key, T value, CacheOptions? cacheOptions = null, CancellationToken cancellationToken = default)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            DistributedCacheEntryOptions options = null;

            if (cacheOptions != null)
            {
                options = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(cacheOptions.SlidingWindowMinutes))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheOptions.AbsoluteExpirationMinutes));
            }

            return SetAsync(cache, key, value, options);
        }

        /// <summary>
        /// This method is used to set a value in the cache.
        /// </summary>
        /// <typeparam name="T">Contains the type of the object to set.</typeparam>
        /// <param name="cache">Contains the distributed cache.</param>
        /// <param name="key">Contains the key to set.</param>
        /// <param name="value">Contains the value to set.</param>
        /// <param name="options">Contains distributed cache options.</param>
        /// <param name="cancellationToken">Contains an optional cancellation token.</param>
        /// <returns>Returns a task.</returns>
        public static Task SetAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options, CancellationToken cancellationToken = default)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, serializerOptions));

            if (options == null)
            {
                options = new DistributedCacheEntryOptions();
            }

            return cache.SetAsync(key, bytes, options, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>Returns a value indicating success</returns>
        public static bool TryGetValue<T>(this IDistributedCache cache, string key, out T value)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            var val = cache.GetString(key);
            value = default;
            if (val == null) return false;
            value = JsonConvert.DeserializeObject<T>(val, serializerOptions);
            return true;
        }

        /// <summary>
        /// This method is used to get a value from the cache.
        /// </summary>
        /// <typeparam name="T">Contains the type of object to return after deserialization.</typeparam>
        /// <param name="cache">Contains the cache to use.</param>
        /// <param name="key">Contains the key to find.</param>
        /// <param name="cancellationToken">Contains an optional cancellation token.</param>
        /// <returns>Returns the object if found.</returns>
        public static async Task<T> GetNoSetAsync<T>(this IDistributedCache cache, string key, CancellationToken cancellationToken = default)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            T result = default;

            if (cache.TryGetValue(key, out T value) && value != null)
            {
                result = value;
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cache"></param>
        /// <param name="key"></param>
        /// <param name="task"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<T> GetOrSetAsync<T>(this IDistributedCache cache, string key, Func<Task<T>> task, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            if (cache.TryGetValue(key, out T value) && value != null)
            {
                return value;
            }

            value = await task();

            if (value != null)
            {
                await cache.SetAsync<T>(key, value, options);
            }

            return value;
        }
    }
}
