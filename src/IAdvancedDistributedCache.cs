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
    }
}