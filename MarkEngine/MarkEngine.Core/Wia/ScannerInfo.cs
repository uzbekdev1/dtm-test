/* 
 * Optical Mark Recognition 
 * Copyright 2015, Justin Fyfe
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * Author: Justin
 * Date: 4-16-2015
 */

using System.Linq;
using WIA;

namespace OmrMarkEngine.Wia
{
    /// <summary>
    ///     Scanner info
    /// </summary>
    public class ScannerInfo
    {
        public ScannerInfo(string name, string id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; set; }
        public string Id { get; set; }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        ///     Get the device
        /// </summary>
        public Device GetDevice()
        {
            var manager = new DeviceManager();
            return manager.DeviceInfos.OfType<DeviceInfo>().FirstOrDefault(o => o.DeviceID == Id).Connect();
        }
    }
}