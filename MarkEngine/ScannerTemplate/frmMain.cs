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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using FyfeSoftware.Sketchy.Core;
using FyfeSoftware.Sketchy.Core.Collections;
using FyfeSoftware.Sketchy.Core.Shapes;
using FyfeSoftware.Sketchy.Design;
using OmrMarkEngine.Core;
using OmrMarkEngine.Core.Processor;
using OmrMarkEngine.Output;
using OmrMarkEngine.Output.Transforms;
using OmrMarkEngine.Template;
using OmrMarkEngine.Template.Design;
using OmrMarkEngine.Template.Scripting;
using OmrMarkEngine.Wia;
using TemplateDesigner;
using TemplateDesigner.Design;

namespace ScannerTemplate
{
    public partial class frmMain : Form
    {
        // Canvas
        private readonly DesignerCanvas m_canvas = new DesignerCanvas();
        private OmrTemplate m_currentTemplate;

        // Dirty flag
        private readonly bool m_isDirty = false;
        private readonly Queue<string> m_processQueue = new Queue<string>();
        private readonly ScanEngine m_scanEngine = new ScanEngine();

        private readonly object m_syncObject = new object();

        // Transforms
        private readonly List<IOutputTransform> m_transforms = new List<IOutputTransform>();

        // Zooms
        private readonly float[] zooms =
        {
            0.1f,
            0.2f,
            0.4f,
            0.5f,
            0.75f,
            1.0f,
            1.25f,
            1.5f,
            1.75f,
            2.0f,
            3.0f
        };

        public frmMain()
        {
            InitializeComponent();
            skHost1.Canvas = m_canvas;
            m_canvas.SelectedShapes.CollectionModified += SelectedShapes_CollectionModified;
            foreach (var scan in m_scanEngine.GetWiaDevices())
            {
                ToolStripItem tsi = new ToolStripMenuItem(scan.Name);
                tsi.Tag = scan;
                tsi.Click += tsi_Click;
                mnuScanner.DropDownItems.Add(tsi);
                ToolStripItem newTsi = new ToolStripMenuItem(scan.Name);
                newTsi.Tag = scan;
                newTsi.Click += newTsi_Click;

                mnuNewFromScanner.DropDownItems.Add(newTsi);
            }

            // Scan completed
            m_scanEngine.ScanCompleted += m_scanEngine_ScanCompleted;
            if (m_scanEngine.GetWiaDevices().Count == 0)
            {
                mnuScanner.DropDownItems.Add("No Scanners Available");
                mnuNewFromScanner.DropDownItems.Add("No Scanners Available");
            }

            // Output types
            foreach (
                var itm in
                typeof(Engine).Assembly.GetTypes().Where(o => o.GetInterface(typeof(IOutputTransform).FullName) != null)
            )
            {
                var tx = Activator.CreateInstance(itm) as IOutputTransform;
                m_transforms.Add(tx);
            }
        }

        /// <summary>
        ///     New scan click
        /// </summary>
        private void newTsi_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_isDirty &&
                    (MessageBox.Show("Discard unsaved changes?", "Confirm New", MessageBoxButtons.YesNo) ==
                     DialogResult.No))
                    mnuSave_Click(sender, e);

                lblStatus.Text = "Scanning...";
                var imgData = m_scanEngine.ScanSingle((sender as ToolStripMenuItem).Tag as ScannerInfo);
                using (var ms = new MemoryStream(imgData))
                {
                    var img = Image.FromStream(ms);
                    img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    using (var sci = new ScannedImage(img))
                    {
                        var tFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                        sci.Analyze();
                        using (var correctedImage = sci.GetCorrectedImage())
                        {
                            correctedImage.Save(tFile);
                        }
                        lsvImages.Clear();
                        m_currentTemplate = OmrTemplate.FromFile(tFile);
                    }
                }
                UpdateTemplateDiagram();
                testToolStripMenuItem.Enabled = true;

                lblStatus.Text = "Idle...";

                SelectRootImageProperties();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not create template : " + ex.Message);
            }
        }

        /// <summary>
        ///     Scan is completed
        /// </summary>
        private void m_scanEngine_ScanCompleted(object sender, ScanCompletedEventArgs e)
        {
            string tFile = null;
            using (var ms = new MemoryStream(e.Image))
            {
                tFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                using (var fs = File.Create(tFile))
                {
                    ms.WriteTo(fs);
                }
            }
            lock (m_syncObject)
            {
                m_processQueue.Enqueue(tFile);
            }
            if (!bwImageProcess.IsBusy)
                bwImageProcess.RunWorkerAsync();
        }

        /// <summary>
        ///     Click on a scanner
        /// </summary>
        private void tsi_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Scanning...";
            m_scanEngine.ScanAsync((sender as ToolStripMenuItem).Tag as ScannerInfo);
        }

        /// <summary>
        ///     Selected shapes have changes
        /// </summary>
        private void SelectedShapes_CollectionModified(object sender, CollectionModifiedEventArgs e)
        {
            if (e.ModificationType == CollectionModificationType.ItemsAdded)
            {
                var selectedItems = new List<object>();
                foreach (var itm in e.Items)
                    selectedItems.Add(itm.Tag);
                if (selectedItems.Count() == 0)
                    pgMain.SelectedObject = m_currentTemplate;
                else
                    pgMain.SelectedObjects = selectedItems.ToArray();
            }
            else if (e.ModificationType == CollectionModificationType.Cleared)
                SelectRootImageProperties();
            skHost1.Focus();
        }

        /// <summary>
        ///     Select the image
        /// </summary>
        private void mnuNewFromImage_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_isDirty &&
                    (MessageBox.Show("Discard unsaved changes?", "Confirm New", MessageBoxButtons.YesNo) ==
                     DialogResult.No))
                    mnuSave_Click(sender, e);

                // Open the image
                var dlgOpen = new OpenFileDialog
                {
                    Title = "New From Image",
                    Filter =
                        "All Images (*.jpg;*.jpeg;*.bmp;*.png)|*.jpg;*.jpeg;*.bmp;*.png|JPEG Images (*.jpg;*.jpeg)|*.jpg;*.jpeg|Bitmaps (*.bmp)|*.bmp|PNG Images (*.png)|*.png"
                };

                // Open
                if (dlgOpen.ShowDialog() == DialogResult.OK)
                {
                    //skHost1.Canvas.Clear();
                    lsvImages.Clear();
                    m_currentTemplate = OmrTemplate.FromFile(dlgOpen.FileName);
                    UpdateTemplateDiagram();
                    testToolStripMenuItem.Enabled = true;


                    SelectRootImageProperties();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can not create template: " + ex.Message);
            }
        }

        /// <summary>
        ///     Update template diagram
        /// </summary>
        private void UpdateTemplateDiagram()
        {
            mnuSave.Enabled = true;
            m_canvas.Clear();
            m_canvas.Add(new BackgroundImageShape(m_currentTemplate), "img");
            // Add indicators for the indicators
            m_canvas.Add(new CornerAnchorShape(m_currentTemplate.TopLeft, "TL"), "TL");
            m_canvas.Add(new CornerAnchorShape(m_currentTemplate.TopRight, "TR"), "TR");
            m_canvas.Add(new CornerAnchorShape(m_currentTemplate.BottomLeft, "BL"), "BL");
            m_canvas.Add(new CornerAnchorShape(m_currentTemplate.BottomRight, "BR"), "BR");

            // Join the canvas stuff
            m_canvas.Add(new ConnectionLineShape
            {
                Source = m_canvas.FindShape("TL"),
                Target = m_canvas.FindShape("TR"),
                OutlineWidth = 4,
                OutlineColor = Color.OrangeRed,
                OutlineStyle = DashStyle.Dot
            });
            m_canvas.Add(new ConnectionLineShape
            {
                Source = m_canvas.FindShape("TR"),
                Target = m_canvas.FindShape("BR"),
                OutlineWidth = 4,
                OutlineColor = Color.OrangeRed,
                OutlineStyle = DashStyle.Dot
            });
            m_canvas.Add(new ConnectionLineShape
            {
                Source = m_canvas.FindShape("BL"),
                Target = m_canvas.FindShape("BR"),
                OutlineWidth = 4,
                OutlineColor = Color.OrangeRed,
                OutlineStyle = DashStyle.Dot
            });
            m_canvas.Add(new ConnectionLineShape
            {
                Source = m_canvas.FindShape("TL"),
                Target = m_canvas.FindShape("BL"),
                OutlineWidth = 4,
                OutlineColor = Color.OrangeRed,
                OutlineStyle = DashStyle.Dot
            });
        }

        /// <summary>
        ///     Select root image properties
        /// </summary>
        private void SelectRootImageProperties()
        {
            if (m_canvas.FindShape("img") == null) return;
            pgMain.SelectedObject = m_canvas.FindShape("img").Tag;
        }

        /// <summary>
        ///     Save the template
        /// </summary>
        private void mnuSave_Click(object sender, EventArgs e)
        {
            if (m_currentTemplate == null)
                return;

            if (string.IsNullOrEmpty(m_currentTemplate.FileName))
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Save Template",
                    Filter = "Marker Engine XML (*.mxml)|*.mxml",
                    AddExtension = true
                };
                if (saveDialog.ShowDialog() == DialogResult.OK)
                    m_currentTemplate.FileName = saveDialog.FileName;
                else
                    return;
            }
            m_currentTemplate.Save();
        }

        private void tbZoom_Scroll(object sender, EventArgs e)
        {
            lblZm.Text = string.Format("{0:##}%", zooms[tbZoom.Value]*100);
            m_canvas.Zoom = zooms[tbZoom.Value];
        }

        /// <summary>
        ///     Add a barcode field
        /// </summary>
        private void btnAddBarcodeField_Click(object sender, EventArgs e)
        {
            var field = new OmrBarcodeField
            {
                Id =
                    string.Format("OmrBarcode{0}",
                        m_currentTemplate.Fields.Count(o => o.Id.StartsWith("OmrBarcode")) + 1)
            };
            m_currentTemplate.Fields.Add(field);
            m_canvas.Add(new BarcodeFormFieldStencil(field)
            {
                Position = new PointF(skHost1.HorizontalScroll.Value, skHost1.VerticalScroll.Value)
            }, field.Id);
        }

        /// <summary>
        ///     Bubble answer field
        /// </summary>
        private void btnAddBubble_Click(object sender, EventArgs e)
        {
            var field = new OmrBubbleField
            {
                Id =
                    string.Format("OmrBubble{0}", m_currentTemplate.Fields.Count(o => o.Id.StartsWith("OmrBubble")) + 1)
            };
            m_currentTemplate.Fields.Add(field);
            m_canvas.Add(new BubbleFormFieldStencil(field)
            {
                Position = new PointF(skHost1.HorizontalScroll.Value, skHost1.VerticalScroll.Value)
            }, field.Id);
        }

        /// <summary>
        ///     Open
        /// </summary>
        private void mnuOpen_Click(object sender, EventArgs e)
        {
            try
            {
                if (m_currentTemplate != null)
                    switch (
                        MessageBox.Show("Do you want to save your changes before opening another template?",
                            "Confirm Open", MessageBoxButtons.YesNoCancel))
                    {
                        case DialogResult.Cancel:
                            return;
                        case DialogResult.Yes:
                            mnuSave_Click(sender, e);
                            break;
                    }

                var openDialog = new OpenFileDialog
                {
                    Title = "Open Template",
                    Filter = "Marker Engine XML (*.mxml)|*.mxml",
                    AddExtension = true
                };
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    m_currentTemplate = OmrTemplate.Load(openDialog.FileName);
                    lsvImages.Items.Clear();
                    mnuSave.Enabled = true;
                    UpdateTemplateDiagram();
                    // Add field data
                    foreach (var itm in m_currentTemplate.Fields)
                    {
                        var pos = itm.TopLeft;
                        var size = new SizeF(itm.TopRight.X - itm.TopLeft.X, itm.BottomLeft.Y - itm.TopLeft.Y);
                        if (itm is OmrBarcodeField)
                            m_canvas.Add(
                                new BarcodeFormFieldStencil(itm as OmrBarcodeField) {Position = pos, Size = size},
                                itm.Id);
                        else if (itm is OmrBubbleField)
                            m_canvas.Add(
                                new BubbleFormFieldStencil(itm as OmrBubbleField) {Position = pos, Size = size}, itm.Id);
                        testToolStripMenuItem.Enabled = true;
                    }
                }
                else
                    return;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open template: " + ex.Message);
            }
        }

        /// <summary>
        ///     Copy
        /// </summary>
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pgMain.SelectedObjects.Count(o => o is OmrQuestionField) == 0) return;

            var template = new OmrTemplate();
            template.Fields.AddRange(
                pgMain.SelectedObjects.Where(o => o is OmrQuestionField).Select(o => o as OmrQuestionField).ToArray());

            using (var sw = new StringWriter())
            {
                new XmlSerializer(typeof(OmrTemplate)).Serialize(sw, template);
                sw.Flush();
                Clipboard.SetText(sw.ToString(), TextDataFormat.Text);
            }
        }

        /// <summary>
        ///     Paste
        /// </summary>
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsText(TextDataFormat.Text)) return;

            // get text
            var tdata = Clipboard.GetText();
            using (var sr = new StringReader(tdata))
            {
                var copiedData = new XmlSerializer(typeof(OmrTemplate)).Deserialize(sr) as OmrTemplate;
                var newSelection = new ShapeCollection();
                foreach (var copyData in copiedData.Fields)
                {
                    // Copy the template data
                    var tData = copyData.Clone() as OmrQuestionField;
                    tData.Id = Guid.NewGuid().ToString();

                    // Constructor
                    var shp = m_canvas.FindShape(copyData.Id);

                    ConstructorInfo ci = null;
                    if (shp != null)
                        ci = shp.GetType().GetConstructor(new[] {tData.GetType()});
                    else
                    {
                        if (tData is OmrBarcodeField)
                            ci = typeof(BarcodeFormFieldStencil).GetConstructor(new[] {tData.GetType()});
                        else if (tData is OmrBubbleField)
                            ci = typeof(BubbleFormFieldStencil).GetConstructor(new[] {tData.GetType()});
                    }
                    if (ci == null)
                    {
                        MessageBox.Show(string.Format("Could not paste object {0}", tData.Id));
                        continue;
                    }
                    var clone = ci.Invoke(new object[] {tData}) as IShape;

                    if (shp != null)
                    {
                        clone.Position = new PointF(shp.Position.X + 10, shp.Position.Y + 10);
                        clone.Size = new SizeF(shp.Size.Width, shp.Size.Height);
                    }
                    else
                    {
                        clone.Position = tData.TopLeft;
                        clone.Size = new SizeF(tData.BottomRight.X - tData.TopLeft.X,
                            tData.BottomRight.Y - tData.TopLeft.Y);
                    }

                    clone.Tag = tData;

                    m_currentTemplate.Fields.Add(tData);
                    newSelection.Add(clone);
                    m_canvas.Add(clone);
                }

                m_canvas.ClearSelection();
                m_canvas.SelectedShapes.AddRange(newSelection);
            }
        }

        private void fromImagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lsvImages.Items.Clear();

            // Open the image
            var dlgOpen = new OpenFileDialog
            {
                Title = "Open Sample Images",
                Multiselect = true,
                Filter =
                    "All Images (*.jpg;*.jpeg;*.bmp;*.png)|*.jpg;*.jpeg;*.bmp;*.png|JPEG Images (*.jpg;*.jpeg)|*.jpg;*.jpeg|Bitmaps (*.bmp)|*.bmp|PNG Images (*.png)|*.png"
            };

            if (dlgOpen.ShowDialog() == DialogResult.OK)
                foreach (var img in dlgOpen.FileNames)
                {
                    lblStatus.Text = "Processing Images...";
                    lock (m_syncObject)
                    {
                        m_processQueue.Enqueue(img);
                    }
                    if (!bwImageProcess.IsBusy)
                        bwImageProcess.RunWorkerAsync();
                }
        }


        private void lsvImages_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_canvas.Remove(m_canvas.FindShape("previewTest"));
            if (lsvImages.SelectedItems.Count == 0)
            {
                m_currentTemplate.SourcePath = m_currentTemplate.SourcePath;

                saveSelectedOutputToolStripMenuItem.Enabled = enableTemplateScriptsToolStripMenuItem.Enabled = false;
                return;
            }
            saveSelectedOutputToolStripMenuItem.Enabled = enableTemplateScriptsToolStripMenuItem.Enabled = true;
            // First we want to apply the template
            var engineProcessor = new Engine();
            var output = lsvImages.SelectedItems[0].Tag as OmrPageOutput;
            m_canvas.Add(new OutputVisualizationStencil(output), "previewTest");
            m_currentTemplate.ImageSource = Image.FromFile(output.AnalyzedImage);
            lstErrors.Items.Clear();
            lstErrors.Items.AddRange(output.Validate(m_currentTemplate).Issues.ToArray<object>());
            // Output the template
            DumpPageOutputTree(output);
        }

        /// <summary>
        ///     Dump the page output tree
        /// </summary>
        private void DumpPageOutputTree(OmrPageOutput output)
        {
            lblCurrentPage.Text = string.Format("Page {0}", output.Id);
            trvDocument.Nodes.Clear();
            foreach (var itm in output.Details)
                trvDocument.Nodes.Add(CreateTreeNode(itm));
        }

        /// <summary>
        ///     Create a tree node
        /// </summary>
        public TreeNode CreateTreeNode(OmrOutputData data)
        {
            var retVal = new TreeNode();

            if (data is OmrBubbleData)
            {
                var bubble = data as OmrBubbleData;
                retVal.Text = string.Format("{0} = {1}", bubble.Key, bubble.Value);
                retVal.ImageIndex = retVal.StateImageIndex = 1;
            }
            else if (data is OmrBarcodeData)
            {
                var barcode = data as OmrBarcodeData;
                retVal.Text = string.Format("{0}", barcode.BarcodeData);
                retVal.ImageIndex = retVal.StateImageIndex = 2;
            }
            else if (data is OmrOutputDataCollection)
            {
                var collection = data as OmrOutputDataCollection;
                retVal.Text = string.Format("{0}", collection.Id);
                if (data is OmrAggregateDataOutput)
                    retVal.Text += string.Format(" ({0} = {1})", (data as OmrAggregateDataOutput).Function,
                        (data as OmrAggregateDataOutput).AggregateValue);
                retVal.ImageIndex = retVal.StateImageIndex = 3;
                foreach (var itm in collection.Details)
                    retVal.Nodes.Add(CreateTreeNode(itm));
            }
            return retVal;
        }

        /// <summary>
        ///     Background process to do work
        /// </summary>
        private void bwImageProcess_DoWork(object sender, DoWorkEventArgs e)
        {
            if (m_processQueue.Count > 0)
            {
                Image image = null;
                lock (m_syncObject)
                {
                    image = Image.FromFile(m_processQueue.Dequeue());
                }
                image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                var engineProcessor = new Engine();
                var scannedImage = new ScannedImage(image);
                var output = engineProcessor.ApplyTemplate(m_currentTemplate, scannedImage);
                e.Result = output;
                image.Dispose();
                scannedImage.Dispose();
            }
        }

        /// <summary>
        ///     Run work is completed
        /// </summary>
        private void bwImageProcess_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lblStatus.Text = "Processing Images...";
            spReplay.Panel1Collapsed = false;

            var validationResult = (e.Result as OmrPageOutput).Validate(m_currentTemplate);
            var isValid = validationResult.IsValid;
            var key = lsvImages.Items.Add(isValid ? "Pass" : "Fail", isValid ? 0 : 1);
            key.SubItems.Add((e.Result as OmrPageOutput).Id);
            key.Tag = e.Result;


            if (m_processQueue.Count > 0)
            {
                lblStatus.Text = "Processing Images...";
                Application.DoEvents();

                bwImageProcess.RunWorkerAsync();
            }
            else
                lblStatus.Text = "Idle";
            if (!isValid)
                validationResult.Issues.ForEach(o => key.ToolTipText = key.ToolTipText + o + "\r\n");
        }

        /// <summary>
        ///     Save the current view to a file
        /// </summary>
        private void saveViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dlgSave = new SaveFileDialog
            {
                Title = "Save View",
                Filter =
                    "All Images (*.jpg;*.jpeg;*.bmp;*.png)|*.jpg;*.jpeg;*.bmp;*.png|JPEG Images (*.jpg;*.jpeg)|*.jpg;*.jpeg|Bitmaps (*.bmp)|*.bmp|PNG Images (*.png)|*.png",
                AddExtension = true
            };
            if (dlgSave.ShowDialog() == DialogResult.OK)
                using (var bmp = new Bitmap(m_canvas.Size.Width, m_canvas.Size.Height, PixelFormat.Format24bppRgb))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        m_canvas.DrawTo(g);
                    }
                    bmp.Save(dlgSave.FileName);
                }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_isDirty)
                switch (MessageBox.Show("Discard unsaved changes?", "Confirm Close", MessageBoxButtons.YesNoCancel))
                {
                    case DialogResult.Yes:
                        mnuSave_Click(sender, e);
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
        }

        /// <summary>
        ///     Delete selected
        /// </summary>
        private void deleteSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_canvas.SelectedShapes == null) return;

            var garbagePile = new List<IShape>(m_canvas.SelectedShapes);
            m_canvas.ClearSelection();
            foreach (var shp in garbagePile)
            {
                m_canvas.Remove(shp);
                m_currentTemplate.Fields.Remove(shp.Tag as OmrQuestionField);
            }
        }

        /// <summary>
        ///     Save output to XML file
        /// </summary>
        private void saveSelectedOutputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lsvImages.SelectedItems.Count == 0) return;

            // Filters
            var filters = new StringBuilder();
            foreach (var tx in m_transforms)
                filters.AppendFormat("{0} (*.{1})|*.{1}|", tx.Name, tx.Extension);
            filters.Remove(filters.Length - 1, 1);

            var saveDialog = new SaveFileDialog
            {
                Title = "Save Output",
                Filter = filters.ToString(),
                AddExtension = true
            };
            if (saveDialog.ShowDialog() == DialogResult.OK)
                try
                {
                    var transformer = m_transforms.Find(o => saveDialog.FileName.EndsWith(o.Extension));
                    using (var fs = File.Create(saveDialog.FileName))
                    {
                        var pages = new OmrPageOutputCollection();
                        foreach (ListViewItem sel in lsvImages.SelectedItems)
                            pages.Pages.Add(sel.Tag as OmrPageOutput);

                        var data = transformer.Transform(m_currentTemplate, pages);
                        fs.Write(data, 0, data.Length);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error Saving");
                }
        }

        private void enableTemplateScriptsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lsvImages.SelectedItems.Count == 0) return;

            lblStatus.Text = "Running Script...";
            Application.DoEvents();
            foreach (ListViewItem sel in lsvImages.SelectedItems)
                try
                {
                    while (sel.SubItems.Count < 3)
                        sel.SubItems.Add(new ListViewItem.ListViewSubItem());
                    sel.SubItems[2].Text = "Running Script";
                    Application.DoEvents();
                    new TemplateScriptUtil().Run(m_currentTemplate, sel.Tag as OmrPageOutput);
                    sel.SubItems[2].Text = "";
                }
                catch (ScriptingErrorException ex)
                {
                    sel.SubItems[2].Text = "Error: " + ex.Message;
                }
                catch (Exception ex)
                {
                    sel.SubItems[2].Text = "Error: " + ex.Message;
                    MessageBox.Show(ex.ToString(), "Script Error");
                }
                finally
                {
                    lblStatus.Text = "Idle";
                }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new frmAbout().ShowDialog();
        }
    }
}