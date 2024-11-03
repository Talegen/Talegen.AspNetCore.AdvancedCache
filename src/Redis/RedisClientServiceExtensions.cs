namespace Talegen.AspNetCore.AdvancedCache.Redis
{
    using System;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.DependencyInjection;

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
        /// <returns>The <see cref="IServiceCollection" /> so that additional calls can be chained.</returns>
        public static IServiceCollection AddRedisClientCache(this IServiceCollection services, Action<RedisClientCacheOptions> setupAction)
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
            services.AddSingleton<IAdvancedDistributedCache, AdvancedRedisCache>();
            services.AddSingleton<IDistributedCache>(r =>
            {
                return r.GetRequiredService<IAdvancedDistributedCache>();
            });

            return services;
        }
    }
}