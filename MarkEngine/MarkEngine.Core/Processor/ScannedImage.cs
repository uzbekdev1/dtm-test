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
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using ZXing;
using ZXing.Common;
using Image = System.Drawing.Image;
using Point = AForge.Point;

namespace OmrMarkEngine.Core.Processor
{
    /// <summary>
    ///     Tool for processing images
    /// </summary>
    public class ScannedImage : IDisposable
    {
        private Result[] m_barcodeResults;

        // Backing field for image
        private Bitmap m_bitmap;
        private Point m_bottomLeft;
        private Point m_bottomRight;
        private bool m_disposed;
        private Point m_topLeft;
        private Point m_topRight;

        /// <summary>
        ///     Constructs the image processor from a file
        /// </summary>
        public ScannedImage(string fileName)
        {
            using (var img = Image.FromFile(fileName))
            {
                m_bitmap = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(m_bitmap))
                {
                    g.DrawImage(img, 0, 0, img.Width, img.Height);
                }
            }
        }

        /// <summary>
        ///     Constructs an image process from an existing bitmap
        /// </summary>
        public ScannedImage(Image img)
        {
            m_bitmap = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(m_bitmap))
            {
                g.DrawImage(img, 0, 0, img.Width, img.Height);
            }
        }

        /// <summary>
        ///     Get the indicator that the form has necessary information
        /// </summary>
        public bool IsScannable { get; private set; }

        /// <summary>
        ///     Is ready for scanning?
        /// </summary>
        public bool IsReadyForScan { get; private set; }

        /// <summary>
        ///     Template name
        /// </summary>
        public string TemplateName { get; private set; }

        /// <summary>
        ///     Image
        /// </summary>
        public Image Image
        {
            get { return m_bitmap; }
        }

        /// <summary>
        ///     Marker codes
        /// </summary>
        public Result[] MarkerCodes { get; private set; }

        /// <summary>
        ///     Template parameters
        /// </summary>
        public string[] Parameters { get; private set; }

        /// <summary>
        ///     Get the form area from the raw image
        /// </summary>
        /// <returns></returns>
        public List<PointF> FormArea
        {
            get
            {
                if (m_disposed)
                    throw new ObjectDisposedException("ImageProcessor");
                return new List<PointF>
                {
                    new PointF(m_topLeft.X, m_topLeft.Y),
                    new PointF(m_topRight.X, m_topRight.Y),
                    new PointF(m_bottomRight.X, m_bottomRight.Y),
                    new PointF(m_bottomLeft.X, m_bottomLeft.Y)
                };
            }
        }

        /// <summary>
        ///     Dispose the image
        /// </summary>
        public void Dispose()
        {
            m_bitmap.Dispose();
            m_disposed = true;
        }

        /// <summary>
        ///     Performs the analysis on the image
        /// </summary>
        public void Analyze(bool thorough = true)
        {
            if (m_disposed)
                throw new ObjectDisposedException("ImageProcessor");

            if (IsScannable)
                return;

            LuminanceSource source = new BitmapLuminanceSource(m_bitmap);
            var binarizer = new HybridBinarizer(source);
            var binBitmap = new BinaryBitmap(binarizer);

            // Try to extract the form data
            var barReader = new BarcodeReader();
            barReader.AutoRotate = true;
            barReader.Options.TryHarder = thorough;
            barReader.Options.PossibleFormats = new List<BarcodeFormat> {BarcodeFormat.CODE_128};

            m_barcodeResults = barReader.DecodeMultiple(source);

            // Look for barcode markers if possible
            if (m_barcodeResults != null)
                MarkerCodes = m_barcodeResults.Where(o => o.Text.StartsWith("OMR:")).ToArray();
            IsScannable = true;
            // Get the template data
            var markerCode = MarkerCodes == null
                ? null
                : MarkerCodes.FirstOrDefault(o => o.Text.StartsWith("OMR:TL") || o.Text.StartsWith("OMR:ID"));

            // Get the guiding points by circles
            var grayFilter = new GrayscaleY();
            var thresholdFilter = new Threshold(127);
            var invertFilter = new Invert();
            using (var searchImage = invertFilter.Apply(thresholdFilter.Apply(grayFilter.Apply(m_bitmap))))
            {
                // Blobs
                var blobCounter = new BlobCounter();
                blobCounter.FilterBlobs = true;
                blobCounter.MinHeight = 30;
                blobCounter.MinWidth = 30;

                // Check for circles
                blobCounter.ProcessImage(searchImage);
                var blobs = blobCounter.GetObjectsInformation();
                var shapeChecker = new SimpleShapeChecker();
                var controlPoints = new List<Point>();
                var currentCheck = 45;
                while ((currentCheck-- > 20) && (controlPoints.Count != 4))
                {
                    controlPoints.Clear();
                    // Get the positions
                    foreach (var blob in blobs)
                    {
                        var center = new Point();
                        float radius = 0;

                        if (shapeChecker.IsCircle(blobCounter.GetBlobsEdgePoints(blob), out center, out radius) &&
                            (radius > currentCheck))
                            controlPoints.Add(center);
                    }
                }

                // Control points
                IsScannable &= controlPoints.Count == 4;
                if (!IsScannable)
                    return;

                // Now set markers
                m_topLeft = controlPoints[0]; //new AForge.Point(this.m_bitmap.Width + 10, this.m_bitmap.Height + 10);
                m_topRight = controlPoints[1];
                m_bottomLeft = controlPoints[2];
                m_bottomRight = controlPoints[3];

                // Find the right most bubble
                float rightMost = controlPoints.Select(o => o.X).Max(),
                    leftMost = controlPoints.Select(o => o.X).Min();
                // Organize those that are left/right
                Point[] lefties = controlPoints.Where(o => o.X < leftMost + (rightMost - leftMost)/2).ToArray(),
                    righties = controlPoints.Where(o => o.X > leftMost + (rightMost - leftMost)/2).ToArray();

                // HACK:
                if (lefties[0].Y < lefties[1].Y)
                {
                    m_topLeft = lefties[0];
                    m_bottomLeft = lefties[1];
                }
                else
                {
                    m_topLeft = lefties[1];
                    m_bottomLeft = lefties[0];
                }

                // HACK:
                if (righties[0].Y < righties[1].Y)
                {
                    m_topRight = righties[0];
                    m_bottomRight = righties[1];
                }
                else
                {
                    m_topRight = righties[1];
                    m_bottomRight = righties[0];
                }
            }

            if (!IsScannable)
                return;

            // Get the template data
            if ((MarkerCodes != null) && (markerCode != null))
            {
                var templateData = markerCode.Text.Split(':');
                if (templateData.Length > 2)
                {
                    TemplateName = templateData[2];
                    if (templateData.Length > 3)
                        Parameters = templateData.Skip(3).ToArray();
                }
            }
        }

        /// <summary>
        ///     Get binarized image
        /// </summary>
        public Image GetBinarizedImage()
        {
            if (m_disposed)
                throw new ObjectDisposedException("ImageProcessor");
            if (!IsScannable)
                throw new InvalidOperationException(
                    "Form doesn't have sufficient control information to perform this operation");

            var grayFilter = new Grayscale(255, 255, 255);
            return grayFilter.Apply(m_bitmap);
        }

        /// <summary>
        ///     Gets the corrected image.
        /// </summary>
        /// <remarks>Done by looking for the barcodes with OMR::TL, etc.</remarks>
        public Image GetCorrectedImage()
        {
            if (m_disposed)
                throw new ObjectDisposedException("ImageProcessor");
            if (!IsScannable)
                throw new InvalidOperationException(
                    "Form doesn't have sufficient control information to perform this operation");

            float dy = m_topRight.Y - m_topLeft.Y,
                dx = m_topRight.X - m_topLeft.X;
            var offsetAngle = Math.Tan(dy/dx);
            var rotate = new RotateBicubic(offsetAngle*(180/Math.PI), true);
            return rotate.Apply(m_bitmap);
        }

        /// <summary>
        ///     Get the cropped image
        /// </summary>
        public Image GetCroppedImage()
        {
            var crop =
                new Crop(new Rectangle((int) m_topLeft.X, (int) m_topLeft.Y, (int) (m_topRight.X - m_topLeft.X),
                    (int) (m_bottomLeft.Y - m_topLeft.Y)));
            var bmp = new Bitmap(m_bitmap.Width, m_bitmap.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(Brushes.White, 0, 0, bmp.Width, bmp.Height);
                g.DrawImage(crop.Apply(m_bitmap), m_topLeft.X, m_topLeft.Y);
            }
            return bmp;
        }

        /// <summary>
        ///     Prepare this image for processing
        /// </summary>
        public void PrepareProcessing()
        {
            if (m_disposed)
                throw new ObjectDisposedException("ImageProcessor");
            if (!IsScannable)
                throw new InvalidOperationException(
                    "Form doesn't have sufficient control information to perform this operation");

            // Rotate and re-analyze
            float dy = m_topRight.Y - m_topLeft.Y,
                dx = m_topRight.X - m_topLeft.X;

            var offsetAngle = Math.Tan(dy/dx);
            var rotate = new RotateBilinear(offsetAngle*(180/Math.PI), true);
            var bmp = rotate.Apply(m_bitmap);
            m_bitmap.Dispose();
            m_bitmap = bmp;
            IsScannable = false;
            Analyze(true);

            // crop
            var crop =
                new Crop(new Rectangle((int) m_topLeft.X, (int) m_topLeft.Y, (int) (m_topRight.X - m_topLeft.X),
                    (int) (m_bottomLeft.Y - m_topLeft.Y)));
            bmp = new Bitmap((int) (m_topRight.X - m_topLeft.X), (int) (m_bottomLeft.Y - m_topLeft.Y),
                PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(Brushes.White, 0, 0, m_bitmap.Width, m_bitmap.Height);
                g.DrawImage(crop.Apply(m_bitmap), 0, 0);
            }

            m_bitmap.Dispose();
            m_bitmap = bmp;
            IsReadyForScan = true;
        }
    }
}