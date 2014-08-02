﻿/*
Copyright (c) 2013, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met: 

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer. 
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution. 

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies, 
either expressed or implied, of the FreeBSD Project.
*/

using System;

using MatterHackers.Agg.Transform;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Agg.RasterizerScanline;
using MatterHackers.VectorMath;

using Mini;

namespace MatterHackers.Agg.Sample_LionOutline
{
    public class LionFillOutlineExample : ExampleBase
    {
        lion_outline lionFill;
        public override void Init()
        {
            lionFill = new lion_outline();
        }
        public override void Draw(Graphics2D g)
        {
            lionFill.OnDraw(g);
        }
        public override void MouseDrag(int x, int y)
        {
            lionFill.Move(x, y);
        }

        [ExConfig]
        public bool RenderAsScanline
        {
            get
            {
                return this.lionFill.RenderAsScanline;
            }
            set
            {
                this.lionFill.RenderAsScanline = value;
            }
        }

        [ExConfig]
        public bool RenderAccurateJoins
        {
            get
            {
                return this.lionFill.RenderAccurateJoins;
            }
            set
            {
                this.lionFill.RenderAccurateJoins = value;
            }
        }

    }
    //--------------------------------------------------
    public class lion_outline : BasicSprite
    {
        private LionShape lionShape = new LionShape();
        ScanlineRasterizer rasterizer = new ScanlineRasterizer();
        ScanlineCachePacked8 scanlineCache = new ScanlineCachePacked8();

        //special option 
        public lion_outline()
        {
            this.Width = 500;
            this.Height = 500;
        }
        void NeedsRedraw(object sender, EventArgs e)
        {

        }

        public bool RenderAsScanline
        {
            get;
            set;
        }
        public bool RenderAccurateJoins
        {
            get;
            set;
        }
        public override void OnDraw(Graphics2D graphics2D)
        {
            ImageBuffer widgetsSubImage = ImageBuffer.NewSubImageReference(graphics2D.DestImage, graphics2D.GetClippingRect());

            int width = (int)widgetsSubImage.Width;
            int height = (int)widgetsSubImage.Height;

            int strokeWidth = 1;

            ImageBuffer clippedSubImage = new ImageBuffer();
            clippedSubImage.Attach(widgetsSubImage, new BlenderBGRA());
            ImageClippingProxy imageClippingProxy = new ImageClippingProxy(clippedSubImage);
            imageClippingProxy.clear(new RGBA_Floats(1, 1, 1));

            Affine transform = Affine.NewIdentity();
            transform *= Affine.NewTranslation(-lionShape.Center.x, -lionShape.Center.y);
            transform *= Affine.NewScaling(spriteScale, spriteScale);
            transform *= Affine.NewRotation(angle + Math.PI);
            transform *= Affine.NewSkewing(skewX / 1000.0, skewY / 1000.0);
            transform *= Affine.NewTranslation(width / 2, height / 2);

            if (RenderAsScanline)
            {
                rasterizer.SetVectorClipBox(0, 0, width, height);

                Stroke stroke = new Stroke(lionShape.Path);
                stroke.width(strokeWidth);
                stroke.line_join(LineJoin.Round);
                VertexSourceApplyTransform trans = new VertexSourceApplyTransform(stroke, transform);
                ScanlineRenderer scanlineRenderer = new ScanlineRenderer();
                scanlineRenderer.RenderSolidAllPaths(imageClippingProxy, rasterizer, scanlineCache, trans, lionShape.Colors, lionShape.PathIndex, lionShape.NumPaths);
            }
            else
            {
                double w = strokeWidth * transform.GetScale();

                LineProfileAnitAlias lineProfile = new LineProfileAnitAlias(w, new gamma_none());
                OutlineRenderer outlineRenderer = new OutlineRenderer(imageClippingProxy, lineProfile);
                rasterizer_outline_aa rasterizer = new rasterizer_outline_aa(outlineRenderer);

                rasterizer.line_join(RenderAccurateJoins ?
                    rasterizer_outline_aa.outline_aa_join_e.outline_miter_accurate_join
                    : rasterizer_outline_aa.outline_aa_join_e.outline_round_join);
                rasterizer.round_cap(true);

                VertexSourceApplyTransform trans = new VertexSourceApplyTransform(lionShape.Path, transform);

                rasterizer.RenderAllPaths(trans, lionShape.Colors, lionShape.PathIndex, lionShape.NumPaths);
            }

            base.OnDraw(graphics2D);
        }

    }
}