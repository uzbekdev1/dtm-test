﻿/* 
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

using System.Windows.Forms;
using System.Xml.Serialization;

namespace OmrMarkEngine.Template
{
    /// <summary>
    ///     OMR True/False field
    /// </summary>
    [XmlType("OmrQuestionField", Namespace = "urn:scan-omr:template")]
    public class OmrTrueFalseField : OmrBubbleContainer
    {
        // Horizontal
        private Orientation m_orientation = Orientation.Horizontal;

        /// <summary>
        ///     Gets or sets the orientation
        /// </summary>
        [XmlAttribute("orientation")]
        public Orientation Orientation
        {
            get { return m_orientation; }
            set
            {
                m_orientation = value;
                OnPropertyChange("Orientation");
            }
        }
    }
}