
namespace Talegen.AspNetCore.AdvancedCache
{
    /// <summary>
    /// This class implements the cache options.
    /// </summary>
    public class CacheOptions
    {
        /// <summary>
        /// Gets or sets the sliding window minutes.
        /// </summary>
        public int SlidingWindowMinutes { get; set; } = 30;

        /// <summary>
        /// Gets or sets the absolute expiration hours.
        /// </summary>
        public int AbsoluteExpirationHours { get; set; } = 1;

    }
}
