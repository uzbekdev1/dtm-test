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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AForge.Imaging;
using AForge.Imaging.Filters;
using OmrMarkEngine.Core.Processor;
using OmrMarkEngine.Output;
using OmrMarkEngine.Template;
using ZXing;

namespace OmrMarkEngine.Core
{
    /// <summary>
    ///     OMR Engine Root
    /// </summary>
    public class Engine
    {
        /// <summary>
        ///     When true indicates that the process should save intermediate images
        /// </summary>
        public bool SaveIntermediaryImages { get; set; }

        /// <summary>
        ///     Apply template
        /// </summary>
        /// <param name="template"></param>
        /// <param name="image"></param>
        public OmrPageOutput ApplyTemplate(OmrTemplate template, ScannedImage image)
        {
            // Image ready for scan
            if (!image.IsReadyForScan)
            {
                if (!image.IsScannable)
                    image.Analyze();
                image.PrepareProcessing();
            }

            // Page output
            var retVal = new OmrPageOutput
            {
                Id = image.TemplateName + DateTime.Now.ToString("yyyyMMddHHmmss"),
                TemplateId = image.TemplateName,
                Parameters = image.Parameters,
                StartTime = DateTime.Now,
                Template = template
            };

            // Save directory for output images 
            var saveDirectory = string.Empty;
            var parmStr = new StringBuilder();
            if (SaveIntermediaryImages)
            {
                if (image.Parameters != null)
                    foreach (var pv in image.Parameters)
                        parmStr.AppendFormat("{0}.", pv);
                retVal.RefImages = new List<string>
                {
                    string.Format("{0}-{1}-init.bmp", retVal.Id, parmStr),
                    string.Format("{0}-{1}-tx.bmp", retVal.Id, parmStr),
                    string.Format("{0}-{1}-fields.bmp", retVal.Id, parmStr),
                    string.Format("{0}-{1}-gs.bmp", retVal.Id, parmStr),
                    string.Format("{0}-{1}-bw.bmp", retVal.Id, parmStr),
                    string.Format("{0}-{1}-inv.bmp", retVal.Id, parmStr)
                };

                saveDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "imgproc");

                if (!Directory.Exists(saveDirectory))
                    Directory.CreateDirectory(saveDirectory);
                image.Image.Save(Path.Combine(saveDirectory,
                    string.Format("{0}-{1}-init.bmp", DateTime.Now.ToString("yyyyMMddHHmmss"), parmStr)));
            }

            // First, we want to get the image from the scanned image and translate it to the original
            // position in the template
            Bitmap bmp = null;
            try
            {
                bmp = new Bitmap((int) template.BottomRight.X, (int) template.BottomRight.Y, PixelFormat.Format24bppRgb);

                // Scale
                float width = template.TopRight.X - template.TopLeft.X,
                    height = template.BottomLeft.Y - template.TopLeft.Y;


                // Translate to original
                using (var g = Graphics.FromImage(bmp))
                {
                    var bc = new ResizeBicubic((int) width, (int) height);
                    g.DrawImage(bc.Apply((Bitmap) image.Image), template.TopLeft.X, template.TopLeft.Y);
                }

                if (SaveIntermediaryImages)
                    bmp.Save(Path.Combine(saveDirectory,
                        string.Format("{0}-{1}-tx.bmp", DateTime.Now.ToString("yyyyMMddHHmmss"), parmStr)));


                // Now try to do hit from the template
                if (SaveIntermediaryImages)
                    using (var tbmp = bmp.Clone() as Bitmap)
                    {
                        using (var g = Graphics.FromImage(tbmp))
                        {
                            foreach (var field in template.Fields)
                            {
                                g.DrawRectangle(Pens.Black, field.TopLeft.X, field.TopLeft.Y,
                                    field.TopRight.X - field.TopLeft.X, field.BottomLeft.Y - field.TopLeft.Y);
                                g.DrawString(field.Id, SystemFonts.CaptionFont, Brushes.Black, field.TopLeft);
                            }
                        }

                        tbmp.Save(Path.Combine(saveDirectory,
                            string.Format("{0}-{1}-fields.bmp", DateTime.Now.ToString("yyyyMMddHHmmss"), parmStr)));
                    }


                // Now convert to Grayscale
                var grayFilter = new GrayscaleY();
                var gray = grayFilter.Apply(bmp);
                bmp.Dispose();
                bmp = gray;

                if (SaveIntermediaryImages)
                    bmp.Save(Path.Combine(saveDirectory,
                        string.Format("{0}-{1}-gs.bmp", DateTime.Now.ToString("yyyyMMddHHmmss"), parmStr)));

                // Prepare answers
                var hitFields = new Dictionary<OmrQuestionField, OmrOutputData>();
                var barScan = new BarcodeReader();
                barScan.Options.UseCode39ExtendedMode = true;
                barScan.Options.UseCode39RelaxedExtendedMode = true;
                barScan.Options.TryHarder = true;
                barScan.TryInverted = true;
                barScan.Options.PureBarcode = false;
                barScan.AutoRotate = true;

                foreach (var itm in template.Fields.Where(o => o is OmrBarcodeField))
                {
                    var position = itm.TopLeft;
                    var size = new SizeF(itm.TopRight.X - itm.TopLeft.X, itm.BottomLeft.Y - itm.TopLeft.Y);
                    using (
                        var areaOfInterest =
                            new Crop(new Rectangle((int) position.X, (int) position.Y, (int) size.Width,
                                (int) size.Height)).Apply(bmp))
                    {
                        // Scan the barcode
                        var result = barScan.Decode(areaOfInterest);


                        if (result != null)
                            hitFields.Add(itm, new OmrBarcodeData
                            {
                                BarcodeData = result.Text,
                                Format = result.BarcodeFormat,
                                Id = itm.Id,
                                TopLeft =
                                    new PointF(result.ResultPoints[0].X + position.X,
                                        result.ResultPoints[0].Y + position.Y),
                                BottomRight =
                                    new PointF(result.ResultPoints[1].X + position.X,
                                        result.ResultPoints[0].Y + position.Y + 10)
                            });
                    }
                }

                // Now binarize
                var binaryThreshold = new Threshold(template.ScanThreshold);
                binaryThreshold.ApplyInPlace(bmp);

                if (SaveIntermediaryImages)
                    bmp.Save(Path.Combine(saveDirectory,
                        string.Format("{0}-{1}-bw.bmp", DateTime.Now.ToString("yyyyMMddHHmmss"), parmStr)));

                // Set return parameters
                var tAnalyzeFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                bmp.Save(tAnalyzeFile, ImageFormat.Jpeg);
                retVal.AnalyzedImage = tAnalyzeFile;
                retVal.BottomRight = new PointF(bmp.Width, bmp.Height);

                // Now Invert 
                var invertFiter = new Invert();
                invertFiter.ApplyInPlace(bmp);

                if (SaveIntermediaryImages)
                    bmp.Save(Path.Combine(saveDirectory,
                        string.Format("{0}-{1}-inv.bmp", DateTime.Now.ToString("yyyyMMddHHmmss"), parmStr)));


                // Crop out areas of interest
                var areasOfInterest = new List<KeyValuePair<OmrQuestionField, Bitmap>>();
                foreach (var itm in template.Fields.Where(o => o is OmrBubbleField))
                {
                    var position = itm.TopLeft;
                    var size = new SizeF(itm.TopRight.X - itm.TopLeft.X, itm.BottomLeft.Y - itm.TopLeft.Y);
                    areasOfInterest.Add(new KeyValuePair<OmrQuestionField, Bitmap>(
                            itm,
                            new Crop(new Rectangle((int) position.X, (int) position.Y, (int) size.Width,
                                (int) size.Height)).Apply(bmp))
                    );
                }

                // Queue analysis
                var wtp = new WaitThreadPool();
                var syncLock = new object();

                foreach (var itm in areasOfInterest)
                    wtp.QueueUserWorkItem(img =>
                    {
                        var parm = itm;

                        try
                        {
                            var areaOfInterest = parm.Value;
                            var field = parm.Key;

                            var blobCounter = new BlobCounter();
                            blobCounter.FilterBlobs = true;

                            // Check for circles
                            blobCounter.ProcessImage(areaOfInterest);
                            var blobs = blobCounter.GetObjectsInformation();
                            var blob = blobs.FirstOrDefault(o => o.Area == blobs.Max(b => b.Area));
                            if (blob != null)
                            {
                                //var area = new AForge.Imaging.ImageStatistics(blob).PixelsCountWithoutBlack;
                                if (blob.Area < 30)
                                    return;
                                var bubbleField = field as OmrBubbleField;
                                lock (syncLock)
                                {
                                    hitFields.Add(field, new OmrBubbleData
                                    {
                                        Id = field.Id,
                                        Key = bubbleField.Question,
                                        Value = bubbleField.Value,
                                        TopLeft =
                                            new PointF(blob.Rectangle.X + field.TopLeft.X,
                                                blob.Rectangle.Y + field.TopLeft.Y),
                                        BottomRight =
                                            new PointF(blob.Rectangle.X + blob.Rectangle.Width + field.TopLeft.X,
                                                blob.Rectangle.Y + blob.Rectangle.Height + field.TopLeft.Y),
                                        BlobArea = blob.Area
                                    });
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError(e.ToString());
                        }
                        finally
                        {
                            parm.Value.Dispose();
                        }
                    }, itm);

                wtp.WaitOne();

                // Organize the response 
                foreach (var res in hitFields)
                    if (string.IsNullOrEmpty(res.Key.AnswerRowGroup))
                    {
                        AddAnswerToOutputCollection(retVal, res);
                    }
                    else
                    {
                        // Rows of data
                        var rowGroup = retVal.Details.Find(o => o.Id == res.Key.AnswerRowGroup) as OmrRowData;
                        if (rowGroup == null)
                        {
                            rowGroup = new OmrRowData
                            {
                                Id = res.Key.AnswerRowGroup
                            };
                            retVal.Details.Add(rowGroup);
                        }

                        AddAnswerToOutputCollection(rowGroup, res);
                    }

                // Remove temporary images
                //foreach (var f in retVal.RefImages)
                //    File.Delete(Path.Combine(saveDirectory, f));

                // Outcome is success
                retVal.Outcome = OmrScanOutcome.Success;
            }
            catch (Exception e)
            {
                retVal.Outcome = OmrScanOutcome.Failure;
                retVal.ErrorMessage = e.Message;
                Trace.TraceError(e.ToString());
            }
            finally
            {
                retVal.StopTime = DateTime.Now;
                bmp.Dispose();
            }

            return retVal;
        }

        /// <summary>
        ///     Add an answer to an output collection
        /// </summary>
        /// <param name="retVal"></param>
        /// <param name="answerPair"></param>
        private void AddAnswerToOutputCollection(OmrOutputDataCollection collection,
            KeyValuePair<OmrQuestionField, OmrOutputData> answerPair)
        {
            // Now add answer
            if (answerPair.Key is OmrBubbleField)
            {
                var bubbleField = answerPair.Key as OmrBubbleField;
                switch (bubbleField.Behavior)
                {
                    case BubbleBehaviorType.One:
                        if (!collection.AlreadyAnswered(answerPair.Value))
                            collection.Details.Add(answerPair.Value);
                        break;
                    case BubbleBehaviorType.Multi:
                        collection.Details.Add(answerPair.Value);
                        break;
                    case BubbleBehaviorType.Count:
                        var aggregate =
                            collection.Details.Find(o => o.Id == bubbleField.Question) as OmrAggregateDataOutput;
                        if (aggregate == null)
                        {
                            aggregate = new OmrAggregateDataOutput {Id = bubbleField.Question};
                            aggregate.Function = AggregationFunction.Count;
                            collection.Details.Add(aggregate);
                        }
                        if (collection is OmrRowData)
                            aggregate.RowId = collection.Id;
                        // Add this answer to the aggregate
                        aggregate.Details.Add(answerPair.Value);
                        break;
                }
            }
            else if (!collection.AlreadyAnswered(answerPair.Value))
                collection.Details.Add(answerPair.Value);
        }
    }
}