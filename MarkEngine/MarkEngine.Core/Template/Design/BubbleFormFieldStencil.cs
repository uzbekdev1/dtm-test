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
using System.Drawing.Drawing2D;
using FyfeSoftware.Sketchy.Core;
using FyfeSoftware.Sketchy.Core.Primitives.Stencils;
using FyfeSoftware.Sketchy.Core.Shapes;

namespace OmrMarkEngine.Template.Design
{
    /// <summary>
    ///     bubble field stencil
    /// </summary>
    [Serializable]
    public class BubbleFormFieldStencil : AbstractInteractiveStencil
    {
        private readonly TextShape m_answerShape = new TextShape
        {
            FillBrush = Brushes.Black,
            Position = new PointF(4, 4),
            Font = new Font(FontFamily.GenericSansSerif, 16.0f)
        };

        // True/false shape
        private readonly RectangleShape m_bubbleShape = new RectangleShape
        {
            FillBrush = new SolidBrush(Color.FromArgb(127, 0, 0, 255)),
            OutlineColor = Color.Blue,
            OutlineWidth = 4,
            OutlineStyle = DashStyle.Solid,
            Position = new PointF(2, 2),
            Size = new SizeF(40, 40)
        };

        /// <summary>
        ///     Field for bubble
        /// </summary>
        private readonly OmrBubbleField m_field;

        /// <summary>
        ///     True/false form field stencil
        /// </summary>
        public BubbleFormFieldStencil(OmrBubbleField field)
        {
            Tag = m_field = field;
            Size = new SizeF(48, 48);
            m_answerShape.Text = field.Value;

            Add(m_bubbleShape);
            Add(m_answerShape);

            PositionChanged += BubbleFormFieldStencil_PositionChanged;
            SizeChanged += BubbleFormFieldStencil_SizeChanged;
            m_field.PropertyChanged += m_field_PropertyChanged;
        }

        /// <summary>
        ///     Move only
        /// </summary>
        public override SizeModeType AllowedSizing
        {
            get { return SizeModeType.Horizontal | SizeModeType.Vertical; }
        }

        /// <summary>
        ///     Editor
        /// </summary>
        public override IShapeEditor Editor
        {
            get
            {
                return null;
                ;
            }
        }

        /// <summary>
        ///     Property changed
        /// </summary>
        private void m_field_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            m_answerShape.Text = m_field.Value;
            if (Site != null)
                Site.Name = m_field.Id;
        }

        /// <summary>
        ///     Size changed
        /// </summary>
        private void BubbleFormFieldStencil_SizeChanged(object sender, EventArgs e)
        {
            UpdateTemplate();
        }

        /// <summary>
        ///     Position changed
        /// </summary>
        private void BubbleFormFieldStencil_PositionChanged(object sender, EventArgs e)
        {
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
        ///     End of edit
        /// </summary>
        public override void HandleEndEdit(ShapeEditEventArgs e)
        {
        }
    }
}