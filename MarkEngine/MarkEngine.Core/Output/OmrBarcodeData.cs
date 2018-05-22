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

using System.Xml.Serialization;
using ZXing;

namespace OmrMarkEngine.Output
{
    /// <summary>
    ///     Barcode data
    /// </summary>
    [XmlType("OmrBarcodeData", Namespace = "urn:scan-omr:analysis")]
    public class OmrBarcodeData : OmrOutputData
    {
        /// <summary>
        ///     The format of the barcode
        /// </summary>
        [XmlAttribute("format")]
        public BarcodeFormat Format { get; set; }

        /// <summary>
        ///     Data
        /// </summary>
        [XmlAttribute("data")]
        public string BarcodeData { get; set; }

        /// <summary>
        ///     Barcode data as string
        /// </summary>
        public override string ToString()
        {
            return BarcodeData;
        }
    }
}