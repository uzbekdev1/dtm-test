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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.Windows.Forms.Design;
using System.Xml.Serialization;
using OmrMarkEngine.Core.Processor;
using OmrMarkEngine.Core.Template;

namespace OmrMarkEngine.Template
{
    [XmlType("OmrTemplate", Namespace = "urn:scan-omr:template")]
    [XmlRoot("template", Namespace = "urn:scan-omr:template")]
    public class OmrTemplate : OmrBoundObject
    {
        private const string MAX_VERSION = "0.8.0.0";
        private Image m_imageSource;

        // Backing fields
        private string m_sourcePath;

        /// <summary>
        ///     Creates a new Omr template
        /// </summary>
        public OmrTemplate()
        {
            Version = MAX_VERSION;
            ScanThreshold = 120;
            Id = "MyForm";
            Fields = new List<OmrQuestionField>();
        }

        /// <summary>
        ///     Gets the version of the template
        /// </summary>
        [XmlAttribute("version")]
        public string Version { get; set; }

        /// <summary>
        ///     Filename
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public string FileName { get; set; }

        /// <summary>
        ///     Source image path
        /// </summary>
        [XmlElement("sourcePath")]
        [Description("The image that this template is based off")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string SourcePath
        {
            get { return m_sourcePath; }
            set
            {
                m_sourcePath = value;
                OnPropertyChange("SourcePath");
            }
        }

        /// <summary>
        ///     Gets the image source from which the template was created if applicable
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public Image ImageSource
        {
            get { return m_imageSource; }
            set
            {
                m_imageSource = value;
                OnPropertyChange("ImageSource");
            }
        }

        /// <summary>
        ///     Fields
        /// </summary>
        [XmlElement("barcodeQuestion", Type = typeof(OmrBarcodeField))]
        [XmlElement("questionBubble", Type = typeof(OmrBubbleField))]
        public List<OmrQuestionField> Fields { get; private set; }


        /// <summary>
        ///     Gets a list of fields collapsed from all containers
        /// </summary>
        [XmlIgnore]
        public List<OmrQuestionField> FlatFields
        {
            get
            {
                var retVal = new List<OmrQuestionField>();
                foreach (var itm in Fields)
                    if (itm is OmrBubbleContainer)
                        retVal.AddRange((itm as OmrBubbleContainer).Children);
                    else
                        retVal.Add(itm);
                return retVal;
            }
        }

        /// <summary>
        ///     Scan threshold
        /// </summary>
        [XmlAttribute("scanThreshold")]
        public int ScanThreshold { get; set; }

        /// <summary>
        ///     Script fired after processing is completed
        /// </summary>
        [XmlElement("script")]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public OmrTemplateScript Script { get; set; }

        /// <summary>
        ///     Save the file
        /// </summary>
        public void Save()
        {
            // Load from stream
            using (var fs = File.Create(FileName))
            {
                var xsz = new XmlSerializer(typeof(OmrTemplate));
                xsz.Serialize(fs, this);
            }
        }

        /// <summary>
        ///     Load from the file
        /// </summary>
        public static OmrTemplate Load(string fileName)
        {
            // Load from stream
            using (var fs = File.OpenRead(fileName))
            {
                var xsz = new XmlSerializer(typeof(OmrTemplate));
                var retVal = xsz.Deserialize(fs) as OmrTemplate;
                if (new Version(retVal.Version) > new Version(MAX_VERSION))
                    throw new TemplateVersionException(retVal.Version, MAX_VERSION);

                retVal.FileName = fileName;
                return retVal;
            }
        }

        /// <summary>
        ///     Create an OmrTemplate instance from the scanned image
        /// </summary>
        public static OmrTemplate FromFile(string fileName)
        {
            var retVal = new OmrTemplate();
            retVal.SourcePath = fileName;

            // Input image
            var inputImage = new ScannedImage(fileName);

            // Analyze the image
            inputImage.Analyze();
            retVal.ImageSource = inputImage.Image;
            retVal.ProcessImageData(inputImage);

            return retVal;
        }
    }
}