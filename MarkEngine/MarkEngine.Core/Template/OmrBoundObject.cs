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
using System.Drawing;
using System.Xml.Serialization;
using OmrMarkEngine.Core.Processor;

namespace OmrMarkEngine.Template
{
    /// <summary>
    ///     Bound object
    /// </summary>
    [XmlType("OmrBoundObject", Namespace = "urn:scan-omr:template")]
    public abstract class OmrBoundObject : OmrIdentifiedObject
    {
        private PointF m_bottomLeft;
        private PointF m_bottomRight;
        // Backing fields
        private PointF m_topLeft;
        private PointF m_topRight;

        /// <summary>
        ///     Gets or sets the top left corner
        /// </summary>
        [XmlIgnore]
        public PointF TopLeft
        {
            get { return m_topLeft; }
            set
            {
                m_topLeft = value;
                OnPropertyChange("TopLeft");
            }
        }

        /// <summary>
        ///     Gets or sets the top right
        /// </summary>
        [XmlIgnore]
        public PointF TopRight
        {
            get { return m_topRight; }
            set
            {
                m_topRight = value;
                OnPropertyChange("TopRight");
            }
        }

        /// <summary>
        ///     Gets or sets the bottom right
        /// </summary>
        [XmlIgnore]
        public PointF BottomRight
        {
            get { return m_bottomRight; }
            set
            {
                m_bottomRight = value;
                OnPropertyChange("BottomRight");
            }
        }

        /// <summary>
        ///     GEts or sets the bottom left
        /// </summary>
        [XmlIgnore]
        public PointF BottomLeft
        {
            get { return m_bottomLeft; }
            set
            {
                m_bottomLeft = value;
                OnPropertyChange("BottomLeft");
            }
        }

        /// <summary>
        ///     Gets the topleft in the image
        /// </summary>
        [XmlAttribute("topLeft")]
        [Browsable(false)]
        public string XmlTopLeft
        {
            get { return string.Format("{0},{1}", TopLeft.X, TopLeft.Y); }
            set
            {
                var comps = value.Split(',');
                TopLeft = new PointF(float.Parse(comps[0]), float.Parse(comps[1]));
            }
        }

        /// <summary>
        ///     Gets the bottomleft in the image
        /// </summary>
        [XmlAttribute("bottomLeft")]
        [Browsable(false)]
        public string XmlBottomLeft
        {
            get { return string.Format("{0},{1}", BottomLeft.X, BottomLeft.Y); }
            set
            {
                var comps = value.Split(',');
                BottomLeft = new PointF(float.Parse(comps[0]), float.Parse(comps[1]));
            }
        }

        /// <summary>
        ///     Gets the topright
        /// </summary>
        [XmlAttribute("topRight")]
        [Browsable(false)]
        public string XmlTopRight
        {
            get { return string.Format("{0},{1}", TopRight.X, TopRight.Y); }
            set
            {
                var comps = value.Split(',');
                TopRight = new PointF(float.Parse(comps[0]), float.Parse(comps[1]));
            }
        }

        /// <summary>
        ///     Gets the bottom right
        /// </summary>
        [XmlAttribute("bottomRight")]
        [Browsable(false)]
        public string XmlBottomRight
        {
            get { return string.Format("{0},{1}", BottomRight.X, BottomRight.Y); }
            set
            {
                var comps = value.Split(',');
                BottomRight = new PointF(float.Parse(comps[0]), float.Parse(comps[1]));
            }
        }

        /// <summary>
        ///     Process image data
        /// </summary>
        /// <param name="image"></param>
        protected override void ProcessImageData(ScannedImage image)
        {
            base.ProcessImageData(image);
            if (image.IsScannable)
            {
                m_topLeft = image.FormArea[0];
                m_topRight = image.FormArea[1];
                m_bottomRight = image.FormArea[2];
                m_bottomLeft = image.FormArea[3];
            }
        }
    }
}