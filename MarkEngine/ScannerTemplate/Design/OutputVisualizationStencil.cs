/* 
 * Optical Mark Recognition Engine
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

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using FyfeSoftware.Sketchy.Core;
using FyfeSoftware.Sketchy.Core.Primitives;
using FyfeSoftware.Sketchy.Core.Shapes;
using OmrMarkEngine.Output;

namespace TemplateDesigner.Design
{
    /// <summary>
    ///     A stencil which is responsible for visualizing an output
    /// </summary>
    public class OutputVisualizationStencil : AbstractStencil
    {
        // The data which is being visualized
        private OmrPageOutput m_data;

        /// <summary>
        ///     The output visualizer
        /// </summary>
        public OutputVisualizationStencil(OmrPageOutput pageOutput)
        {
            m_data = pageOutput;
            // Find the bounds and that is our size
            Size = new SizeF(pageOutput.BottomRight.X, pageOutput.BottomRight.Y);
            Position = new PointF(0, 0);
            DrawItems(pageOutput.Details);

            Add(new TextShape
            {
                FillBrush = Brushes.White,
                Font = new Font(FontFamily.GenericSansSerif, 16f, FontStyle.Bold),
                Position = new PointF(0, 0),
                Text = string.Format("Scan ID: {0}", pageOutput.Id)
            });
        }

        /// <summary>
        ///     Get the allowed sizing modes
        /// </summary>
        public override SizeModeType AllowedSizing
        {
            get { return SizeModeType.None; }
        }

        /// <summary>
        ///     Draw items
        /// </summary>
        private void DrawItems(List<OmrOutputData> details)
        {
            foreach (var dtl in details)
                if (dtl is OmrOutputDataCollection)
                    DrawItems((dtl as OmrOutputDataCollection).Details);
                else
                {
                    var blotch = new RectangleShape
                    {
                        Position = dtl.TopLeft,
                        Size = new SizeF(dtl.BottomRight.X - dtl.TopLeft.X, dtl.BottomRight.Y - dtl.TopLeft.Y),
                        FillBrush = new SolidBrush(Color.FromArgb(127, Color.Green)),
                        OutlineColor = Color.DarkGreen,
                        OutlineStyle = DashStyle.Solid,
                        OutlineWidth = 2
                    };
                    Add(blotch);
                }
        }
    }
}