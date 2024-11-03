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
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Contains extension methods to add Memory distributed caching services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    public static class MemoryClientServiceExtensions
    {
        /// <summary>
        /// Adds Memory distributed caching services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
        public static IServiceCollection AddMemoryClientCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.AddSingleton<IAdvancedDistributedCache, AdvancedMemoryCache>();
            services.AddSingleton<IDistributedCache>(r =>
            {
                return r.GetRequiredService<IAdvancedDistributedCache>();
            });
            return services;
        }
    }
}
