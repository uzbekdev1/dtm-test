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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using FyfeSoftware.Sketchy.Core;
using FyfeSoftware.Sketchy.Core.Shapes;

namespace OmrMarkEngine.Template.Design
{
    /// <summary>
    ///     Represents a shape with an image
    /// </summary>
    [Serializable]
    public class BackgroundImageShape : AbstractStyledShape
    {
        // Image backing field
        [NonSerialized] private Image m_image;

        // Omr template
        private readonly OmrTemplate m_template;

        /// <summary>
        ///     The background image shape ctor
        /// </summary>
        public BackgroundImageShape(OmrTemplate template)
        {
            m_template = template;
            if (File.Exists(template.SourcePath))
                Image = Image.FromFile(template.SourcePath);
            else if (File.Exists(Path.Combine(Path.GetDirectoryName(template.FileName), template.SourcePath)))
                Image = Image.FromFile(Path.Combine(Path.GetDirectoryName(template.FileName), template.SourcePath));
            else
                Image = new Bitmap((int) template.BottomRight.X, (int) template.BottomRight.Y,
                    PixelFormat.Format24bppRgb);
            m_template.PropertyChanged += m_template_PropertyChanged;
            Tag = m_template;
        }

        /// <summary>
        ///     The image
        /// </summary>
        public Image Image
        {
            get { return m_image; }
            set
            {
                m_image = value;
                Position = new PointF(0, 0);
                Size = m_image.Size;

                if (GetCanvas() != null)
                    GetCanvas().Size = new Size((int) Size.Width, (int) Size.Height);

                // Redraw on change
                if (GetCanvas() != null)
                    GetCanvas().Invalidate();
            }
        }

        /// <summary>
        ///     Sizing mode
        /// </summary>
        public override SizeModeType AllowedSizing
        {
            get { return SizeModeType.None; }
        }

        /// <summary>
        ///     Template property has changed
        /// </summary>
        private void m_template_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SourcePath")
            {
                Image.Dispose();

                if (File.Exists(m_template.SourcePath))
                    Image = Image.FromFile(m_template.SourcePath);
                else if (File.Exists(Path.Combine(Path.GetDirectoryName(m_template.FileName), m_template.SourcePath)))
                    Image =
                        Image.FromFile(Path.Combine(Path.GetDirectoryName(m_template.FileName), m_template.SourcePath));
                else
                    Image = new Bitmap((int) m_template.BottomRight.X, (int) m_template.BottomRight.Y,
                        PixelFormat.Format24bppRgb);
            }
            else if ((e.PropertyName == "ImageSource") && (m_template.ImageSource != null))
            {
                Image.Dispose();
                Image = m_template.ImageSource;
            }
        }

        /// <summary>
        ///     Draw to the output
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        public override bool DrawTo(Graphics g)
        {
            if ((GetCanvas().Size.Width < Size.Width) &&
                (GetCanvas().Size.Height < Size.Height))
                GetCanvas().Size = new Size((int) Size.Width, (int) Size.Height);

            g.DrawImage(m_image, DrawPosition.X, DrawPosition.Y, DrawSize.Width, DrawSize.Height);
            return true;
        }
    }
}