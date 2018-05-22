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
using System.Drawing.Text;
using FyfeSoftware.Sketchy.Core;
using FyfeSoftware.Sketchy.Core.Primitives.Stencils;
using FyfeSoftware.Sketchy.Core.Shapes;

namespace OmrMarkEngine.Template.Design
{
    /// <summary>
    ///     Barcode form field stencil
    /// </summary>
    [Serializable]
    public class BarcodeFormFieldStencil : AbstractInteractiveStencil
    {
        [NonSerialized] private readonly RectangleShape m_containerShape;

        // The field to which this belongs
        [NonSerialized] private readonly OmrBarcodeField m_field;

        [NonSerialized] private readonly ImageShape m_iconShape;

        [NonSerialized] private readonly TextShape m_textShape;

        /// <summary>
        ///     Barcode form field stencil
        /// </summary>
        public BarcodeFormFieldStencil(OmrBarcodeField field)
        {
            Tag = m_field = field;
            Size = new SizeF(200, 96);

            // Container shape
            m_containerShape = new RectangleShape
            {
                FillBrush = new SolidBrush(Color.FromArgb(127, Color.Gainsboro)),
                Position = new PointF(0, 0),
                ShadowBrush = new SolidBrush(Color.FromArgb(127, 0, 0, 0)),
                Size = new SizeF(Size.Width, Size.Height)
            };
            m_textShape = new TextShape
            {
                Alignment = StringAlignment.Center,
                FillBrush = Brushes.Black,
                Text = field.Id,
                Font = new Font(new FontFamily(GenericFontFamilies.Serif), 16)
            };
            m_textShape.Position = new PointF((Size.Width - m_textShape.Size.Width)/2, (Size.Height - 32)/2 + 16);

            m_iconShape = new ImageShape
            {
                Position = new PointF((Size.Width - 32)/2, 5),
                Size = new SizeF(32, 32),
                AutoScale = false,
                Image =
                    Image.FromStream(
                        GetType()
                            .Assembly.GetManifestResourceStream("OmrMarkEngine.Core.Template.Resources.view-barcode.png"))
            };

            Add(m_containerShape, "container");
            Add(m_iconShape, "image");
            Add(m_textShape);

            m_field.PropertyChanged += m_field_PropertyChanged;
            SizeChanged += BarcodeFormFieldStencil_SizeChanged;
            PositionChanged += BarcodeFormFieldStencil_PositionChanged;
        }

        /// <summary>
        ///     Gets the editor
        /// </summary>
        public override IShapeEditor Editor
        {
            get { return null; }
        }

        /// <summary>
        ///     Allowed sizing
        /// </summary>
        public override SizeModeType AllowedSizing
        {
            get { return SizeModeType.Horizontal | SizeModeType.Vertical; }
        }


        /// <summary>
        ///     Set the size
        /// </summary>
        public override SizeF Size
        {
            get { return base.Size; }
            set
            {
                if ((value.Width < 200) || (value.Height < 96))
                    return;
                base.Size = value;
            }
        }

        /// <summary>
        ///     Handle edit
        /// </summary>
        /// <param name="e"></param>
        public override void HandleEndEdit(ShapeEditEventArgs e)
        {
        }

        /// <summary>
        ///     Position has changed
        /// </summary>
        private void BarcodeFormFieldStencil_PositionChanged(object sender, EventArgs e)
        {
            UpdateTemplate();
        }

        /// <summary>
        ///     Size has changed
        /// </summary>
        private void BarcodeFormFieldStencil_SizeChanged(object sender, EventArgs e)
        {
            m_textShape.Position = new PointF((Size.Width - m_textShape.Size.Width)/2, (Size.Height - 32)/2 + 16);
            UpdateTemplate();
        }

        /// <summary>
        ///     Update the position
        /// </summary>
        private void UpdateTemplate()
        {
            m_field.TopLeft = Position;
            m_field.TopRight = new PointF(Position.X + Size.Width, Position.Y);
            m_field.BottomRight = new PointF(Position.X + Size.Width, Position.Y + Size.Height);
            m_field.BottomLeft = new PointF(Position.X, Position.Y + Size.Height);
        }

        /// <summary>
        ///     Property has change
        /// </summary>
        private void m_field_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            m_textShape.Text = m_field.Id;
            if (Site != null)
                Site.Name = m_field.Id;
        }
    }
}