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
    using Microsoft.Extensions.Options;
    using StackExchange.Redis;

    /// <summary>
    /// Contains an enumeration of the minimum server commands supported by the Redis server.
    /// </summary>
    public enum RedisMinimumServerCommands
    {
        /// <summary>
        /// Supports 6.x versions of Redis.
        /// </summary>
        Six,

        /// <summary>
        /// Supports 7.2+ versions of Redis.
        /// </summary>
        SevenTwo
    }

    /// <summary>
    /// Configuration options for the <see cref="RedisClientCache" /> class.
    /// </summary>
    public class RedisClientCacheOptions : IOptions<RedisClientCacheOptions>
    {
        /// <summary>
        /// The configuration used to connect to Redis.
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// The configuration used to connect to Redis. This is preferred over Configuration.
        /// </summary>
        public ConfigurationOptions ConfigurationOptions { get; set; }

        /// <summary>
        /// The Redis instance name.
        /// </summary>
        public string InstanceName { get; set; }

        /// <summary>
        /// Gets or sets the minimum server commands supported by the Redis server.
        /// </summary>
        public RedisMinimumServerCommands MinimumServerCommands { get; set; } = RedisMinimumServerCommands.Six;

        /// <summary>
        /// Gets the instance of the Redis cache options
        /// </summary>
        RedisClientCacheOptions IOptions<RedisClientCacheOptions>.Value => this;
    }
}