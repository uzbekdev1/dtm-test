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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using AForge.Imaging.Filters;
using FyfeSoftware.Sketchy.Core;
using FyfeSoftware.Sketchy.Design;
using OmrMarkEngine.Core;
using OmrMarkEngine.Core.Processor;
using OmrMarkEngine.Output;
using OmrMarkEngine.Output.Design;
using OmrMarkEngine.Template;
using OmrMarkEngine.Template.Scripting;
using OmrMarkEngine.Wia;

namespace OmrScannerApplication
{
    public partial class frmAutoScan : Form
    {
        private readonly Queue<KeyValuePair<OmrTemplate, OmrPageOutput>> m_executionQueue =
            new Queue<KeyValuePair<OmrTemplate, OmrPageOutput>>();

        private readonly object m_lockObject = new object();
        private readonly ScanEngine m_scanEngine = new ScanEngine();

        /// <summary>
        ///     Scanned pages
        /// </summary>
        private OmrPageOutputCollection m_scannedPages;

        private readonly WaitThreadPool m_threadPool = new WaitThreadPool();
        private Dictionary<ListViewItem, Image> m_uiResults = new Dictionary<ListViewItem, Image>();


        // Auto scan
        public frmAutoScan()
        {
            InitializeComponent();
            ReloadDevices();
            m_scanEngine.ScanCompleted += m_scanEngine_ScanCompleted;
        }

        /// <summary>
        ///     Scan engine has completed
        /// </summary>
        private void m_scanEngine_ScanCompleted(object sender, ScanCompletedEventArgs e)
        {
            // Enqueue the data
            m_threadPool.QueueUserWorkItem(ProcessImageWorker, e.Image);
            if (!bwUpdate.IsBusy)
                bwUpdate.RunWorkerAsync();
        }

        /// <summary>
        ///     Reload devices
        /// </summary>
        private void ReloadDevices()
        {
            cboScanners.Items.Clear();
            cboScanners.Items.AddRange(m_scanEngine.GetWiaDevices().ToArray());
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            ReloadDevices();
        }

        /// <summary>
        ///     Start scan
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, EventArgs e)
        {
            m_scannedPages = new OmrPageOutputCollection();
            foreach (var itm in m_uiResults)
            {
                itm.Value.Dispose();
                itm.Key.Tag = null;
            }
            m_uiResults = new Dictionary<ListViewItem, Image>();
            lsvView.Items.Clear();

            if (cboScanners.SelectedItem == null)
                MessageBox.Show("Please select a valid scanner");
            else
            {
                stsMain.Visible = true;
                stsMain.Style = ProgressBarStyle.Marquee;
                lblStatus.Text = "Acquiring Images...";
                groupBox1.Enabled = false;
                m_scanEngine.ScanAsync(cboScanners.SelectedItem as ScannerInfo);
                lblStatus.Text = "Waiting for processing to complete...";
            }
        }

        private void lsvView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lsvView.SelectedItems.Count == 0) return;
            pictureBox1.Image = lsvView.SelectedItems[0].Tag as Image;
        }

        /// <summary>
        ///     Do work
        /// </summary>
        private void ProcessImageWorker(object state)
        {
            ScannedImage scannedImage = null;
            Image original = null;
            try
            {
                lock (m_lockObject)
                {
                    using (var ms = new MemoryStream((byte[]) state))
                    {
                        var img = Image.FromStream(ms);
                        img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        scannedImage = new ScannedImage(img);
                    }
                }

                scannedImage.Analyze();

                original =
                    new ResizeNearestNeighbor(scannedImage.Image.Width/4, scannedImage.Image.Height/4).Apply(
                        (Bitmap) scannedImage.Image);
            }
            catch
            {
                if (scannedImage != null)
                    scannedImage.Dispose();
                return; // Abort
            }

            try
            {
                // Add an Error entry
                if (!scannedImage.IsScannable)
                {
                    var lsv = new ListViewItem();
                    lsv.Tag = 2;
                    lsv.SubItems.Add(new ListViewItem.ListViewSubItem(lsv,
                        "Scanned image doesn't appear to be a scannable form."));
                    lock (m_lockObject)
                    {
                        m_uiResults.Add(lsv, original);
                    }
                }
                else
                {
                    var engine = new Engine();
                    var templateFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
                        scannedImage.TemplateName + ".mxml");
                    if (!File.Exists(templateFile))
                    {
                        var lsv = new ListViewItem();
                        lsv.Tag = 0;
                        lsv.SubItems.Add(new ListViewItem.ListViewSubItem(lsv,
                            "Template file " + templateFile + " is missing"));
                        lock (m_lockObject)
                        {
                            m_uiResults.Add(lsv, original);
                        }
                        return;
                    }

                    // Apply template
                    var template = OmrTemplate.Load(templateFile);
                    var pageData = engine.ApplyTemplate(
                        template,
                        scannedImage);

                    // Draw the page data
                    ICanvas canvas = new DesignerCanvas();
                    canvas.Add(new OutputVisualizationStencil(pageData));
                    original.Dispose();
                    original = new Bitmap((int) template.BottomRight.X, (int) template.BottomLeft.Y,
                        PixelFormat.Format24bppRgb);
                    using (var g = Graphics.FromImage(original))
                    {
                        float width = template.TopRight.X - template.TopLeft.X,
                            height = template.BottomLeft.Y - template.TopLeft.Y;
                        g.DrawImage(scannedImage.Image, template.TopLeft.X, template.TopLeft.Y, width, height);
                        canvas.DrawTo(g);
                    }
                    // Save original analyzed image
                    try
                    {
                        var tPath = Path.GetTempFileName();
                        original.Save(tPath);
                        pageData.AnalyzedImage = tPath;
                    }
                    catch
                    {
                    }

                    var oldOriginal = original;
                    original =
                        new ResizeNearestNeighbor(scannedImage.Image.Width/2, scannedImage.Image.Height/2).Apply(
                            (Bitmap) original);
                    oldOriginal.Dispose();


                    lock (m_lockObject)
                    {
                        if (pageData.Outcome == OmrScanOutcome.Failure)
                        {
                            var lsv = new ListViewItem();
                            lsv.Tag = 0;
                            lsv.SubItems.Add(new ListViewItem.ListViewSubItem(lsv, pageData.ErrorMessage));
                            lock (m_lockObject)
                            {
                                m_uiResults.Add(lsv, original);
                            }
                        }
                        else
                        {
                            var validation = pageData.Validate(template);
                            var lsv = new ListViewItem();
                            lsv.Tag = validation.IsValid ? 2 : 1;
                            if (!validation.IsValid)
                                lsv.SubItems.Add(validation.Issues[0]);
                            lsv.Tag = pageData;
                            lock (m_lockObject)
                            {
                                m_executionQueue.Enqueue(new KeyValuePair<OmrTemplate, OmrPageOutput>(template, pageData));
                            }
                            lock (m_lockObject)
                            {
                                m_uiResults.Add(lsv, original);
                            }
                        }
                        m_scannedPages.Pages.Add(pageData);
                    }
                }
            }
            catch (Exception e)
            {
                var lsv = new ListViewItem();
                lsv.Tag = 0;
                lsv.SubItems.Add(e.Message);
                lock (m_lockObject)
                {
                    m_uiResults.Add(lsv, original);
                }
            }
        }

        /// <summary>
        ///     Make a summary
        /// </summary>
        private string MakeSummary(OmrPageOutput pageData)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Following Data was Scanned for {0}:\r\nParms:", pageData.TemplateId);
            if (pageData.Parameters != null)
                foreach (var parm in pageData.Parameters)
                    sb.AppendFormat("{0}; ", parm);

            foreach (var itm in pageData.Details)
                if (itm is OmrOutputDataCollection)
                {
                    sb.AppendFormat("(Row {0}: ", itm.Id);
                    foreach (var subItm in (itm as OmrOutputDataCollection).Details)
                        if (subItm is OmrBubbleData)
                        {
                            var bubItm = subItm as OmrBubbleData;
                            sb.AppendFormat("{0} = {1}; ", bubItm.Key, bubItm.Value);
                        }
                        else if (subItm is OmrBarcodeData)
                            sb.AppendFormat("Barcode {0}; ", (subItm as OmrBarcodeData).BarcodeData);
                    sb.Append(")\r\n");
                }
                else if (itm is OmrBubbleData)
                {
                    var bubItm = itm as OmrBubbleData;
                    sb.AppendFormat("{0} = {1} \r\n ", bubItm.Key, bubItm.Value);
                }
                else if (itm is OmrBarcodeData)
                    sb.AppendFormat("Barcode {0} \r\n ", (itm as OmrBarcodeData).BarcodeData);
            return sb.ToString();
        }


        /// <summary>
        ///     Completed, add the data
        /// </summary>
        private void bwUpdate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Execute
            while (m_executionQueue.Count > 0)
            {
                var data = m_executionQueue.Dequeue();
                var lsv = m_uiResults.Keys.FirstOrDefault(o => o.Tag == data.Value);

                // Run script
                try
                {
                    new TemplateScriptUtil().Run(data.Key, data.Value);
                    lsv.SubItems.Add(new ListViewItem.ListViewSubItem(lsv, MakeSummary(data.Value)));
                    lsv.Tag = 2;
                }
                catch (Exception ex)
                {
                    lsv.Tag = 1;
                    var sb = new StringBuilder(ex.Message);
                    while (ex.InnerException != null)
                    {
                        ex = ex.InnerException;
                        sb.AppendFormat(": {0}", ex.Message);
                    }
                    if (lsv.SubItems.Count < 2)
                        lsv.SubItems.Add("");

                    lsv.SubItems[1].Text = ex.Message;
                }
            }

            foreach (var itm in m_uiResults)
            {
                if (!(itm.Key.Tag is int))
                    continue;
                //imlScan.Images.Add(itm.Value);
                var lsv = lsvView.Items.Add(string.Empty, (int) itm.Key.Tag);
                lsv.SubItems.Add(itm.Key.SubItems[1].Text);
                lsv.Tag = itm.Value;
            }

            groupBox1.Enabled = true;
            lblStatus.Text = "Complete";
            stsMain.Visible = false;
        }

        /// <summary>
        ///     Do work
        /// </summary>
        private void bwUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            m_threadPool.WaitOne();
        }
    }
}