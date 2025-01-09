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
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    /// <summary>
    /// This class is used for adding Distributed Cache according to configuration
    /// </summary>
    public static class RedisClientServiceExtensions
    {
        /// <summary>
        /// Adds Redis distributed caching services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">An <see cref="Action{RedisCacheOptions}" /> to configure the provided <see cref="RedisCacheOptions" />.</param>
        /// <param name="keyedServiceId">An optional keyed service identifier.</param>
        /// <param name="registerIDistributedCache">A value indicating whether to register the advanced service as an override of <see cref="IDistributedCache" /> service.</param>
        /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
        public static IServiceCollection AddRedisClientCache(this IServiceCollection services, Action<RedisClientCacheOptions> setupAction, string keyedServiceId = "", bool registerIDistributedCache = false)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            services.Configure(setupAction);

            // If the keyed service id is empty, then we are using the default configuration.
            if (string.IsNullOrWhiteSpace(keyedServiceId))
            {
                services.TryAddSingleton<IAdvancedDistributedCache, AdvancedRedisCache>();

                if (registerIDistributedCache)
                {
                    services.TryAddSingleton<IDistributedCache>(r =>
                    {
                        return r.GetRequiredService<IAdvancedDistributedCache>();
                    });
                }
            }
            else
            {
                // If the keyed service id is not empty, then we are using a named configuration.
                services.TryAddKeyedSingleton<IAdvancedDistributedCache, AdvancedRedisCache>(keyedServiceId);
            }

            return services;
        }
    }
}