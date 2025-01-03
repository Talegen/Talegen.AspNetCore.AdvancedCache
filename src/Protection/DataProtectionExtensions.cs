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
namespace Talegen.AspNetCore.AdvancedCache.Protection
{
    using System;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.DataProtection.KeyManagement;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// This class contains data protection extensions.
    /// </summary>
    public static class DataProtectionExtensions
    {
        /// <summary>
        /// This method is used to persist keys to a distributed store.
        /// </summary>
        /// <param name="builder">Contains the data protection builder.</param>
        /// <param name="key">Contains the optional cache key to use for storage.</param>
        /// <returns>Returns the data protection builder.</returns>
        /// <exception cref="ArgumentNullException">Thrown if builder is not specified.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the cache service is not found.</exception>
        public static IDataProtectionBuilder PersistKeysToDistributedCache(this IDataProtectionBuilder builder, string key = "DataProtection-Keys")
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton(services =>
            {
                var cache = services.GetService<IAdvancedDistributedCache>();
                return cache == null
                    ? throw new InvalidOperationException("An advanced distributed cache is required to persist keys to a distributed store. Make sure you call AddRedisClientCache before Data protection logic.")
                    : (IConfigureOptions<KeyManagementOptions>) new ConfigureOptions<KeyManagementOptions>(options => options.XmlRepository = new DistributedDataProtectionRepository(cache, key));
            });

            return builder;
        }
    }
}
