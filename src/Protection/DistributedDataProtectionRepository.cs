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
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.AspNetCore.DataProtection.Repositories;
    using Microsoft.Extensions.Caching.Distributed;

    /// <summary>
    /// This class implements the XML repository for distributed data protection.
    /// </summary>
    public class DistributedDataProtectionRepository : IXmlRepository
    {
        private readonly IAdvancedDistributedCache cache;
        private readonly string key;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributedDataProtectionRepository"/> class.
        /// </summary>
        /// <param name="cache">Contains the advanced distributed cache.</param>
        /// <param name="key">Contains the key to store the keys.</param>
        /// <exception cref="ArgumentNullException">Thrown if cache is not initiated.</exception>
        public DistributedDataProtectionRepository(IAdvancedDistributedCache cache, string key)
        {
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this.key = key;
        }

        /// <summary>
        /// This method is used to get all elements.
        /// </summary>
        /// <returns>Returns a read-only collection of elements found.</returns>
        public IReadOnlyCollection<XElement> GetAllElements()
        {
            IReadOnlyCollection<XElement> result;

            var data = this.cache.GetString(key);

            if (!string.IsNullOrWhiteSpace(data))
            {
                var doc = XDocument.Parse(data); 
                result = doc.Root.Elements().ToList().AsReadOnly();
            }
            else
            {
                result = new List<XElement>().AsReadOnly();
            }

            return result;
        }

        /// <summary>
        /// This method is used to store an element.
        /// </summary>
        /// <param name="element">Contains the element to store.</param>
        /// <param name="friendlyName">Contains a friendly name.</param>
        public void StoreElement(XElement element, string friendlyName)
        {
            var data = this.cache.GetString(this.key);
            XDocument doc = string.IsNullOrEmpty(data) ? new XDocument(new XElement("keys")) : XDocument.Parse(data); doc.Root.Add(element);
            this.cache.SetString(this.key, doc.ToString());
        }
    }
}
