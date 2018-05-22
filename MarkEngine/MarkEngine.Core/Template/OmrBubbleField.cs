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

using System.ComponentModel;
using System.Xml.Serialization;

namespace OmrMarkEngine.Template
{
    /// <summary>
    ///     Bubble behavior
    /// </summary>
    public enum BubbleBehaviorType
    {
        One,
        Multi,
        Count
    }

    /// <summary>
    ///     Represents a single bubble
    /// </summary>
    [XmlType("OmrBubbleField", Namespace = "urn:scan-omr:template")]
    public class OmrBubbleField : OmrQuestionField
    {
        private string m_answer;
        private BubbleBehaviorType m_behavior = BubbleBehaviorType.One;

        private string m_question;

        /// <summary>
        ///     Bubble behavior type,
        /// </summary>
        [XmlAttribute("behavior")]
        [Description(
             "Behavior of the bubble. One = Only one bubble in the Question may be selected, Multi = Multiple bubbles may be selected, Sum = The sum of selected bubbles"
         )]
        public BubbleBehaviorType Behavior
        {
            get { return m_behavior; }
            set
            {
                m_behavior = value;
                OnPropertyChange("Behavior");
            }
        }

        /// <summary>
        ///     Gets or sets the question key (that is the key which this belongs to)
        /// </summary>
        [XmlAttribute("key")]
        [Description("The question which the value answers")]
        public string Question
        {
            get { return m_question; }
            set
            {
                m_question = value;
                OnPropertyChange("Question");
            }
        }

        /// <summary>
        ///     Gets or sets the question key (that is the key which this belongs to)
        /// </summary>
        [XmlAttribute("value")]
        [Description("The value this bubble represents when filled")]
        public string Value
        {
            get { return m_answer; }
            set
            {
                m_answer = value;
                OnPropertyChange("Value");
            }
        }
    }
}