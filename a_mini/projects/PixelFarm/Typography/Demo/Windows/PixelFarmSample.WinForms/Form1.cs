﻿//MIT, 2016-2017, WinterDev
using System;
using System.Collections.Generic;

using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Typography.OpenFont;
using Typography.Rendering;

using PixelFarm.Agg;
using Typography.TextLayout;
using PixelFarm.Drawing.Fonts;

namespace SampleWinForms
{
    public partial class Form1 : Form
    {
        Graphics g;
        AggCanvasPainter p;
        ImageGraphics2D imgGfx2d;
        ActualImage destImg;
        Bitmap winBmp;

        string _currentSelectedFontFile = "";
        float fontSizeInPoint = 14; //default

        public Form1()
        {
            InitializeComponent();
            this.Load += new EventHandler(Form1_Load);
            this.txtGridSize.KeyDown += TxtGridSize_KeyDown;
            //----------
            txtInputChar.TextChanged += (s, e) => UpdateRenderOutput();
            //----------
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithMiniAgg);
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithGdiPlusPath);
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithTextPrinterAndMiniAgg);
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithMsdfGen);
            cmbRenderChoices.SelectedIndex = 2;
            cmbRenderChoices.SelectedIndexChanged += (s, e) => UpdateRenderOutput();
            //----------
            cmbPositionTech.Items.Add(PositionTecnhique.OpenFont);
            cmbPositionTech.Items.Add(PositionTecnhique.Kerning);
            cmbPositionTech.Items.Add(PositionTecnhique.None);
            cmbPositionTech.SelectedIndex = 0;
            cmbPositionTech.SelectedIndexChanged += (s, e) => UpdateRenderOutput();
            //----------
            cmbHintTechnique.Items.Add(HintTechnique.None);
            cmbHintTechnique.Items.Add(HintTechnique.TrueTypeInstruction);
            cmbHintTechnique.Items.Add(HintTechnique.TrueTypeInstruction_VerticalOnly);
            cmbHintTechnique.Items.Add(HintTechnique.CustomAutoFit);
            cmbHintTechnique.SelectedIndex = 0;
            cmbHintTechnique.SelectedIndexChanged += (s, e) => UpdateRenderOutput();
            //---------- 

            button1.Click += (s, e) => UpdateRenderOutput();
            chkShowGrid.CheckedChanged += (s, e) => UpdateRenderOutput();
            chkShowTess.CheckedChanged += (s, e) => UpdateRenderOutput();
            chkXGridFitting.CheckedChanged += (s, e) => UpdateRenderOutput();
            chkYGridFitting.CheckedChanged += (s, e) => UpdateRenderOutput();
            chkFillBackground.CheckedChanged += (s, e) => UpdateRenderOutput();
            chkLcdTechnique.CheckedChanged += (s, e) => UpdateRenderOutput();
            chkDrawBone.CheckedChanged += (s, e) => UpdateRenderOutput();
            chkGsubEnableLigature.CheckedChanged += (s, e) => UpdateRenderOutput();
            chkShowTess.CheckedChanged += (s, e) => UpdateRenderOutput();
            //----------
            lstFontSizes.Items.AddRange(
                new object[]{
                    8, 9,
                    10,11,
                    12,
                    14,
                    16,
                    18,20,22,24,26,28,36,48,72,240,300,360
                });
            lstFontSizes.SelectedIndexChanged += (s, e) =>
            {
                //new font size
                fontSizeInPoint = (int)lstFontSizes.SelectedItem;
                UpdateRenderOutput();
            };

            //----------
            //simple load test fonts from local test dir
            //and send it into test list

            int selectedFileIndex = -1;
            //string selectedFontFileName = "pala.ttf";
            string selectedFontFileName = "tahoma.ttf";
            //string selectedFontFileName="cambriaz.ttf";
            //string selectedFontFileName="CompositeMS2.ttf"; 
            int fileIndexCount = 0;
            foreach (string file in Directory.GetFiles("..\\..\\..\\TestFonts", "*.ttf"))
            {
                var tmpLocalFile = new TempLocalFontFile(file);
                lstFontList.Items.Add(tmpLocalFile);
                if (selectedFileIndex < 0 && tmpLocalFile.OnlyFileName == selectedFontFileName)
                {
                    selectedFileIndex = fileIndexCount;
                    _currentSelectedFontFile = file;
                }
                fileIndexCount++;
            }
            if (selectedFileIndex < 0) { selectedFileIndex = 0; }
            lstFontList.SelectedIndex = selectedFileIndex;
            lstFontList.SelectedIndexChanged += (s, e) =>
            {
                _currentSelectedFontFile = ((TempLocalFontFile)lstFontList.SelectedItem).actualFileName;
                UpdateRenderOutput();
            };
            //----------------
            //string inputstr = "ก้า";
            string inputstr = "น้ำน้ำ";
            //string inputstr = "fi";
            //string inputstr = "ก่นกิ่น";
            //string inputstr = "ญญู";
            //string inputstr = "ป่า"; //for gpos test 
            //string inputstr = "快速上手";
            //----------------
            this.txtInputChar.Text = inputstr;
            this.chkFillBackground.Checked = true;
        }


        enum RenderChoice
        {
            RenderWithMiniAgg,
            RenderWithGdiPlusPath,
            RenderWithTextPrinterAndMiniAgg,
            RenderWithMsdfGen, //rendering with multi-channel signed distance field img
            RenderWithSdfGen//not support sdfgen
        }

        void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Render with PixelFarm";
            //this.lstFontSizes.SelectedIndex = lstFontSizes.Items.Count - 1;//select last one  
            this.lstFontSizes.SelectedIndex = 0;//select last one  
        }



        void UpdateRenderOutput()
        {
            if (g == null)
            {
                destImg = new ActualImage(400, 300, PixelFormat.ARGB32);
                imgGfx2d = new ImageGraphics2D(destImg); //no platform
                p = new AggCanvasPainter(imgGfx2d);
                winBmp = new Bitmap(400, 300, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                g = this.CreateGraphics();
            }
            ReadAndRender(_currentSelectedFontFile);
        }



        void ReadAndRender(string fontfile)
        {

            if (string.IsNullOrEmpty(this.txtInputChar.Text))
            {
                p.Clear(PixelFarm.Drawing.Color.White);
                return;
            }
            var reader = new OpenFontReader();
            char testChar = txtInputChar.Text[0];//only 1 char 
            int resolution = 96;
            //1. read typeface from font file

            RenderChoice renderChoice = (RenderChoice)this.cmbRenderChoices.SelectedItem;
            switch (renderChoice)
            {
                case RenderChoice.RenderWithMiniAgg:
                    {
                        using (var fs = new FileStream(fontfile, FileMode.Open))
                        {
                            Typeface typeFace = reader.Read(fs);
                            RenderWithMiniAgg(typeFace, testChar, fontSizeInPoint);
                        }
                    }
                    break;
                case RenderChoice.RenderWithGdiPlusPath:
                    {
                        using (var fs = new FileStream(fontfile, FileMode.Open))
                        {
                            Typeface typeFace = reader.Read(fs);
                            RenderWithGdiPlusPath(typeFace, testChar, fontSizeInPoint, resolution);
                        }
                    }
                    break;
                case RenderChoice.RenderWithTextPrinterAndMiniAgg:
                    {

                        RenderWithTextPrinterAndMiniAgg(fontfile, this.txtInputChar.Text, fontSizeInPoint, resolution);
                    }
                    break;
                case RenderChoice.RenderWithMsdfGen:
                case RenderChoice.RenderWithSdfGen:
                    {
                        using (var fs = new FileStream(fontfile, FileMode.Open))
                        {
                            Typeface typeFace = reader.Read(fs);
                            RenderWithMsdfImg(typeFace, testChar, fontSizeInPoint);
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }

        }


        void RenderWithMiniAgg(Typeface typeface, char testChar, float sizeInPoint)
        {
            //----------------------------------------------------
            var builder = new MyGlyphPathBuilder(typeface);
            var hintTech = (HintTechnique)cmbHintTechnique.SelectedItem;
            builder.UseTrueTypeInstructions = false;//reset
            builder.UseVerticalHinting = false;//reset
            switch (hintTech)
            {
                case HintTechnique.TrueTypeInstruction:
                    builder.UseTrueTypeInstructions = true;
                    break;
                case HintTechnique.TrueTypeInstruction_VerticalOnly:
                    builder.UseTrueTypeInstructions = true;
                    builder.UseVerticalHinting = true;
                    break;
                case HintTechnique.CustomAutoFit:
                    //custom agg autofit 
                    break;
            }
            //----------------------------------------------------

            builder.Build(testChar, sizeInPoint);

            var vxsShapeBuilder = new GlyphPathBuilderVxs();
            builder.ReadShapes(vxsShapeBuilder);
            VertexStore vxs = vxsShapeBuilder.GetVxs();

            p.UseSubPixelRendering = chkLcdTechnique.Checked;

            //5. use PixelFarm's Agg to render to bitmap...
            //5.1 clear background
            p.Clear(PixelFarm.Drawing.Color.White);

            if (chkFillBackground.Checked)
            {
                //5.2 
                p.FillColor = PixelFarm.Drawing.Color.Black;
                //5.3
                if (!chkYGridFitting.Checked)
                {
                    p.Fill(vxs);
                }
            }
            if (chkBorder.Checked)
            {
                //5.4 
                // p.StrokeWidth = 3;
                p.StrokeColor = PixelFarm.Drawing.Color.Green;
                //user can specific border width here...
                //p.StrokeWidth = 2;
                //5.5 
                p.Draw(vxs);
            }


            float pxScale = builder.GetPixelScale();
            //1. autofit
            var autoFit = new GlyphAutoFit();
            autoFit.Hint(
                builder.GetOutputPoints(),
                builder.GetOutputContours(), pxScale);
            var vxsShapeBuilder2 = new GlyphPathBuilderVxs();
            autoFit.ReadOutput(vxsShapeBuilder2);
            VertexStore vxs2 = vxsShapeBuilder2.GetVxs();
            //
            p.FillColor = PixelFarm.Drawing.Color.Black;
            p.Fill(vxs2);

            if (chkShowTess.Checked)
            {
#if DEBUG
                debugDrawTriangulatedGlyph(autoFit.FitOutput, pxScale);
#endif
            }

            if (chkShowGrid.Checked)
            {
                //render grid
                RenderGrid(800, 600, _gridSize, p);
            }



            //6. use this util to copy image from Agg actual image to System.Drawing.Bitmap
            PixelFarm.Agg.Imaging.BitmapHelper.CopyToGdiPlusBitmapSameSize(destImg, winBmp);
            //--------------- 
            //7. just render our bitmap
            g.Clear(Color.White);
            g.DrawImage(winBmp, new Point(30, 20));
        }

        void RenderWithMsdfImg(Typeface typeface, char testChar, float sizeInPoint)
        {
            p.FillColor = PixelFarm.Drawing.Color.Black;
            //p.UseSubPixelRendering = chkLcdTechnique.Checked;
            p.Clear(PixelFarm.Drawing.Color.White);
            //----------------------------------------------------
            var builder = new MyGlyphPathBuilder(typeface);
            var hintTech = (HintTechnique)cmbHintTechnique.SelectedItem;
            builder.UseTrueTypeInstructions = false;//reset
            builder.UseVerticalHinting = false;//reset 
            switch (hintTech)
            {
                case HintTechnique.TrueTypeInstruction:
                    builder.UseTrueTypeInstructions = true;
                    break;
                case HintTechnique.TrueTypeInstruction_VerticalOnly:
                    builder.UseTrueTypeInstructions = true;
                    builder.UseVerticalHinting = true;
                    break;
                case HintTechnique.CustomAutoFit:
                    //custom agg autofit 
                    break;
            }
            //----------------------------------------------------
            builder.Build(testChar, sizeInPoint);
            //----------------------------------------------------
            var msdfGlyphGen = new MsdfGlyphGen();
            GlyphImage2 glyphImg = msdfGlyphGen.CreateMsdfImage(
                builder.GetOutputPoints(),
                builder.GetOutputContours(),
                builder.GetPixelScale());
            var actualImg = ActualImage.CreateFromBuffer(glyphImg.Width, glyphImg.Height, PixelFormat.ARGB32, glyphImg.GetImageBuffer());
            p.DrawImage(actualImg, 0, 0);

            //using (Bitmap bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            //{
            //    var bmpdata = bmp.LockBits(new Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
            //    System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpdata.Scan0, buffer.Length);
            //    bmp.UnlockBits(bmpdata);
            //    bmp.Save("d:\\WImageTest\\a001_xn2_" + n + ".png");
            //}

            if (chkShowGrid.Checked)
            {
                //render grid
                RenderGrid(800, 600, _gridSize, p);
            }

            //6. use this util to copy image from Agg actual image to System.Drawing.Bitmap
            PixelFarm.Agg.Imaging.BitmapHelper.CopyToGdiPlusBitmapSameSize(destImg, winBmp);
            //--------------- 
            //7. just render our bitmap
            g.Clear(Color.White);
            g.DrawImage(winBmp, new Point(30, 20));
        }



        void RenderGrid(int width, int height, int sqSize, AggCanvasPainter p)
        {
            //render grid 
            p.FillColor = PixelFarm.Drawing.Color.Gray;
            for (int y = 0; y < height;)
            {
                for (int x = 0; x < width;)
                {
                    p.FillRectLBWH(x, y, 1, 1);
                    x += sqSize;
                }
                y += sqSize;
            }
        }
        static void DrawEdge(AggCanvasPainter p, EdgeLine edge, float scale)
        {
            if (edge.IsOutside)
            {
                //free side                
                switch (edge.SlopKind)
                {
                    default:
                        p.StrokeColor = PixelFarm.Drawing.Color.Green;
                        break;
                    case LineSlopeKind.Vertical:
                        if (edge.IsLeftSide)
                        {
                            p.StrokeColor = PixelFarm.Drawing.Color.Blue;
                        }
                        else
                        {
                            p.StrokeColor = PixelFarm.Drawing.Color.LightGray;
                        }
                        break;
                    case LineSlopeKind.Horizontal:
                        if (edge.IsUpper)
                        {
                            p.StrokeColor = PixelFarm.Drawing.Color.Red;
                        }
                        else
                        {
                            p.StrokeColor = PixelFarm.Drawing.Color.LightGray;
                        }
                        break;
                }
            }
            else
            {
                switch (edge.SlopKind)
                {
                    default:
                        p.StrokeColor = PixelFarm.Drawing.Color.LightGray;
                        break;
                    case LineSlopeKind.Vertical:
                        p.StrokeColor = PixelFarm.Drawing.Color.Blue;
                        break;
                    case LineSlopeKind.Horizontal:
                        p.StrokeColor = PixelFarm.Drawing.Color.Yellow;
                        break;
                }
            }
            p.Line(edge.x0 * scale, edge.y0 * scale, edge.x1 * scale, edge.y1 * scale);
        }

#if DEBUG
        void debugDrawTriangulatedGlyph(GlyphFitOutline glyphFitOutline, float pixelScale)
        {
            p.StrokeColor = PixelFarm.Drawing.Color.Magenta;
            List<GlyphTriangle> triAngles = glyphFitOutline.dbugGetTriangles();
            int j = triAngles.Count;
            //
            double prev_cx = 0, prev_cy = 0;
            // 
            bool drawBone = this.chkDrawBone.Checked;

            for (int i = 0; i < j; ++i)
            {
                //---------------
                GlyphTriangle tri = triAngles[i];
                EdgeLine e0 = tri.e0;
                EdgeLine e1 = tri.e1;
                EdgeLine e2 = tri.e2;
                //---------------
                //draw each triangles
                DrawEdge(p, e0, pixelScale);
                DrawEdge(p, e1, pixelScale);
                DrawEdge(p, e2, pixelScale);
                //---------------
                //draw centroid
                double cen_x = tri.CentroidX;
                double cen_y = tri.CentroidY;
                //---------------
                p.FillColor = PixelFarm.Drawing.Color.Yellow;
                p.FillRectLBWH(cen_x * pixelScale, cen_y * pixelScale, 2, 2);
                if (!drawBone)
                {
                    //if not draw bone then draw connected lines
                    if (i == 0)
                    {
                        //start mark
                        p.FillColor = PixelFarm.Drawing.Color.Yellow;
                        p.FillRectLBWH(cen_x * pixelScale, cen_y * pixelScale, 7, 7);
                    }
                    else
                    {
                        p.StrokeColor = PixelFarm.Drawing.Color.Red;
                        p.Line(
                            prev_cx * pixelScale, prev_cy * pixelScale,
                           cen_x * pixelScale, cen_y * pixelScale);
                    }
                    prev_cx = cen_x;
                    prev_cy = cen_y;
                }
            }
            //---------------
            //draw bone 
            if (drawBone)
            {
                List<GlyphBone> bones = glyphFitOutline.dbugGetBones();
                j = bones.Count;
                for (int i = 0; i < j; ++i)
                {
                    GlyphBone b = bones[i];
                    if (i == 0)
                    {
                        //start mark
                        p.FillColor = PixelFarm.Drawing.Color.Yellow;
                        p.FillRectLBWH(b.p.CentroidX * pixelScale, b.p.CentroidY * pixelScale, 7, 7);
                    }
                    //draw each bone
                    p.StrokeColor = PixelFarm.Drawing.Color.Red;
                    p.Line(
                        b.p.CentroidX * pixelScale, b.p.CentroidY * pixelScale,
                        b.q.CentroidX * pixelScale, b.q.CentroidY * pixelScale);
                }
            }
            //---------------
        }

#endif

        void DrawGlyphContour(GlyphContour cnt, AggCanvasPainter p)
        {
            //for debug
            List<GlyphPart> parts = cnt.parts;
            int j = parts.Count;
            for (int i = 0; i < j; ++i)
            {
                GlyphPart part = parts[i];
                switch (part.Kind)
                {
                    default: throw new NotSupportedException();
                    case GlyphPartKind.Line:
                        {
                            GlyphLine line = (GlyphLine)part;
                            p.FillColor = PixelFarm.Drawing.Color.Red;
                            p.FillRectLBWH(line.x0, line.y0, 2, 2);
                            p.FillRectLBWH(line.x1, line.y1, 2, 2);
                        }
                        break;
                    case GlyphPartKind.Curve3:
                        {
                            GlyphCurve3 c = (GlyphCurve3)part;
                            p.FillColor = PixelFarm.Drawing.Color.Red;
                            p.FillRectLBWH(c.x0, c.y0, 2, 2);
                            p.FillColor = PixelFarm.Drawing.Color.Blue;
                            p.FillRectLBWH(c.p2x, c.p2y, 2, 2);
                            p.FillColor = PixelFarm.Drawing.Color.Red;
                            p.FillRectLBWH(c.x, c.y, 2, 2);
                        }
                        break;
                    case GlyphPartKind.Curve4:
                        {
                            GlyphCurve4 c = (GlyphCurve4)part;
                            p.FillColor = PixelFarm.Drawing.Color.Red;
                            p.FillRectLBWH(c.x0, c.y0, 2, 2);
                            p.FillColor = PixelFarm.Drawing.Color.Blue;
                            p.FillRectLBWH(c.p2x, c.p2y, 2, 2);
                            p.FillRectLBWH(c.p3x, c.p3y, 2, 2);
                            p.FillColor = PixelFarm.Drawing.Color.Red;
                            p.FillRectLBWH(c.x, c.y, 2, 2);
                        }
                        break;
                }
            }
        }

        void RenderWithGdiPlusPath(Typeface typeface, char testChar, float sizeInPoint, int resolution)
        {

            //render glyph path with Gdi+ path 
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.Clear(Color.White);
            //////credit:
            //////http://stackoverflow.com/questions/1485745/flip-coordinates-when-drawing-to-control
            g.ScaleTransform(1.0F, -1.0F);// Flip the Y-Axis 
            g.TranslateTransform(0.0F, -(float)300);// Translate the drawing area accordingly  


            //----------------------------------------------------
            var builder = new MyGlyphPathBuilder(typeface);
            var hintTech = (HintTechnique)cmbHintTechnique.SelectedItem;
            builder.UseTrueTypeInstructions = false;//reset
            builder.UseVerticalHinting = false;//reset
            switch (hintTech)
            {
                case HintTechnique.TrueTypeInstruction:
                    builder.UseTrueTypeInstructions = true;
                    break;
                case HintTechnique.TrueTypeInstruction_VerticalOnly:
                    builder.UseTrueTypeInstructions = true;
                    builder.UseVerticalHinting = true;
                    break;
                case HintTechnique.CustomAutoFit:
                    //custom agg autofit 
                    break;
            }
            //---------------------------------------------------- 
            builder.Build(testChar, sizeInPoint);
            var gdiPathBuilder = new GlyphPathBuilderGdi();
            builder.ReadShapes(gdiPathBuilder);
            float pxScale = builder.GetPixelScale();

            System.Drawing.Drawing2D.GraphicsPath path = gdiPathBuilder.ResultGraphicPath;
            path.Transform(
                new System.Drawing.Drawing2D.Matrix(
                    pxScale, 0,
                    0, pxScale,
                    0, 0
                ));

            if (chkFillBackground.Checked)
            {
                g.FillPath(Brushes.Black, path);
            }
            if (chkBorder.Checked)
            {
                g.DrawPath(Pens.Green, path);
            }
            //transform back
            g.ScaleTransform(1.0F, -1.0F);// Flip the Y-Axis 
            g.TranslateTransform(0.0F, -(float)300);// Translate the drawing area accordingly            
        }


        TextPrinter printer2;
        void RenderWithTextPrinterAndMiniAgg(string fontfile, string str, float sizeInPoint, int resolution)
        {
            //1. 
            if (printer2 == null)
            {
                printer2 = new TextPrinter();
                printer2.ScriptLang = ScriptLangs.Thai;
            }


            printer2.FontFile = fontfile;
            //
            printer2.EnableLigature = this.chkGsubEnableLigature.Checked;
            printer2.PositionTechnique = (PositionTecnhique)cmbPositionTech.SelectedItem;
            //printer.EnableTrueTypeHint = this.chkTrueTypeHint.Checked;
            //printer.UseAggVerticalHinting = this.chkVerticalHinting.Checked;
            //
            int len = str.Length;
            //
            List<GlyphPlan> glyphPlanList = new List<GlyphPlan>(len);
            printer2.Print(sizeInPoint, str, glyphPlanList);
            //--------------------------

            //5. use PixelFarm's Agg to render to bitmap...
            //5.1 clear background
            p.Clear(PixelFarm.Drawing.Color.White);
            //---------------------------
            //TODO: review here
            //fake subpixel rendering 
            //not correct

            p.UseSubPixelRendering = chkLcdTechnique.Checked;
            //---------------------------
            if (chkFillBackground.Checked)
            {
                //5.2 
                p.FillColor = PixelFarm.Drawing.Color.Black;
                //5.3 
                int glyphListLen = glyphPlanList.Count;

                float ox = p.OriginX;
                float oy = p.OriginY;
                float cx = 0;
                float cy = 10;
                for (int i = 0; i < glyphListLen; ++i)
                {
                    GlyphPlan glyphPlan = glyphPlanList[i];
                    cx = glyphPlan.x;
                    cy = glyphPlan.y;
                    p.SetOrigin(cx, cy);
                    p.Fill((VertexStore)glyphPlan.vxs);
                }
                p.SetOrigin(ox, oy);

            }
            if (chkBorder.Checked)
            {
                //5.4 
                p.StrokeColor = PixelFarm.Drawing.Color.Green;
                //user can specific border width here...
                //p.StrokeWidth = 2;
                //5.5 
                int glyphListLen = glyphPlanList.Count;
                float ox = p.OriginX;
                float oy = p.OriginY;
                float cx = 0;
                float cy = 10;
                for (int i = 0; i < glyphListLen; ++i)
                {
                    GlyphPlan glyphPlan = glyphPlanList[i];
                    cx = glyphPlan.x;
                    p.SetOrigin(cx, cy);
                    p.Draw((VertexStore)glyphPlan.vxs);
                }
                p.SetOrigin(ox, oy);
            }
            //6. use this util to copy image from Agg actual image to System.Drawing.Bitmap
            PixelFarm.Agg.Imaging.BitmapHelper.CopyToGdiPlusBitmapSameSize(destImg, winBmp);
            //--------------- 
            //7. just render our bitmap
            g.Clear(Color.White);
            g.DrawImage(winBmp, new Point(10, 0));
            //--------------------------

        }


        int _gridSize = 5;//default 
        private void TxtGridSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                int result = this._gridSize;
                if (int.TryParse(this.txtGridSize.Text, out result))
                {
                    if (result < 5)
                    {
                        _gridSize = 5;
                    }
                    else if (result > 200)
                    {
                        _gridSize = 200;
                    }
                }
                this._gridSize = result;
                this.txtGridSize.Text = _gridSize.ToString();
                UpdateRenderOutput();
            }

        }
        private void cmdBuildMsdfTexture_Click(object sender, EventArgs e)
        {

            string sampleFontFile = @"..\..\..\TestFonts\tahoma.ttf";
            CreateSampleMsdfTextureFont(
                sampleFontFile,
                18,
                0,
                255,
                "d:\\WImageTest\\sample_msdf.png");

        }
        static void CreateSampleMsdfTextureFont(string fontfile, float sizeInPoint, ushort startGlyphIndex, ushort endGlyphIndex, string outputFile)
        {
            //sample
            var reader = new OpenFontReader();

            using (var fs = new FileStream(fontfile, FileMode.Open))
            {
                //1. read typeface from font file
                Typeface typeface = reader.Read(fs);
                //sample: create sample msdf texture 
                //-------------------------------------------------------------
                var builder = new MyGlyphPathBuilder(typeface);
                //builder.UseTrueTypeInterpreter = this.chkTrueTypeHint.Checked;
                //builder.UseVerticalHinting = this.chkVerticalHinting.Checked;
                //-------------------------------------------------------------
                var atlasBuilder = new SimpleFontAtlasBuilder2();
                var msdfBuilder = new MsdfGlyphGen();

                for (ushort n = startGlyphIndex; n <= endGlyphIndex; ++n)
                {
                    //build glyph
                    builder.BuildFromGlyphIndex(n, sizeInPoint);

                    var msdfGlyphGen = new MsdfGlyphGen();
                    var actualImg = msdfGlyphGen.CreateMsdfImage(
                        builder.GetOutputPoints(),
                        builder.GetOutputContours(),
                        builder.GetPixelScale());
                    atlasBuilder.AddGlyph((int)n, actualImg);

                    //using (Bitmap bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    //{
                    //    var bmpdata = bmp.LockBits(new Rectangle(0, 0, w, h), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                    //    System.Runtime.InteropServices.Marshal.Copy(buffer, 0, bmpdata.Scan0, buffer.Length);
                    //    bmp.UnlockBits(bmpdata);
                    //    bmp.Save("d:\\WImageTest\\a001_xn2_" + n + ".png");
                    //}
                }

                var glyphImg2 = atlasBuilder.BuildSingleImage();
                using (Bitmap bmp = new Bitmap(glyphImg2.Width, glyphImg2.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    var bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, glyphImg2.Width, glyphImg2.Height),
                        System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                    int[] intBuffer = glyphImg2.GetImageBuffer();

                    System.Runtime.InteropServices.Marshal.Copy(intBuffer, 0, bmpdata.Scan0, intBuffer.Length);
                    bmp.UnlockBits(bmpdata);
                    bmp.Save("d:\\WImageTest\\a_total.png");
                }
                atlasBuilder.SaveFontInfo("d:\\WImageTest\\a_info.xml");
            }
        }


    }
}
