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
using MatterHackers.VectorMath;

using Mini;
using LayoutFarm.MiniCinema;
namespace MatterHackers.Agg.Sample_LionFill_Test
{
    [Info(OrderCode = "03")]
    [Info("Affine transformer, and basic renderers. You can rotate and scale the “Lion” with the"
      + " left mouse button. Right mouse button adds “skewing” transformations, proportional to the “X” "
      + "coordinate. The image is drawn over the old one with a cetrain opacity value. Change “Alpha” "
      + "to draw funny looking “lions”. Change window size to clear the window.")]
    public class LionFillExampleTest : DemoBase
    {
        LionFill lionFill;


        public override void Init()
        {
            lionFill = new LionFill();
        }
        public override void Draw(Graphics2D g)
        {
            lionFill.OnDraw(g);
        }
        public override void MouseDrag(int x, int y)
        {
            lionFill.Move(x, y);
        }

        [DemoConfig(MaxValue = 255)]
        public int AlphaValue
        {
            get { return lionFill.AlphaValue; }
            set
            {
                lionFill.AlphaValue = (byte)value;
            }
        }
    }

    //--------------------------------------------------
    public class LionFill : SimpleSprite
    {
        VertexStore vxStorage;
        LionShape lionShape;
        bool needUpdate;
        byte alpha;
        public LionFill()
        {
            lionShape = new LionShape();
            TestLoadLionFromBinaryFile();
            this.Width = 500;
            this.Height = 500;
            AlphaValue = 255;

        }
        void TestLoadLionFromBinaryFile()
        {
            System.Diagnostics.Debugger.Break();
            //test load raw buffer 
            using (var fs = new System.IO.FileStream("..\\lion_stream.bin", System.IO.FileMode.Open))
            {
                var reader = new System.IO.BinaryReader(fs);
                var lionShape2 = new MatterHackers.Agg.LionShape();

                MatterHackers.Agg.VertexSource.PathStorage path;
                MatterHackers.Agg.ColorRGBA[] colors;
                int[] pathIndexList;
                //1. path and command
                MatterHackers.Agg.VertexSource.VertexSourceIO.ReadPathDataFromStream(
                  reader, out path
                  );
                //2. colors
                MatterHackers.Agg.VertexSource.VertexSourceIO.ReadColorDataFromStream(
                  reader, out colors
                  );
                //3. path indice
                int npaths;
                MatterHackers.Agg.VertexSource.VertexSourceIO.ReadPathIndexListFromStream(
                  reader, out npaths, out pathIndexList
                 );

                //------------------------------ 
                LionShape.UnsafeDirectSetData(lionShape2,
                    npaths,
                    path, colors, pathIndexList);
                //---------------------------------

                fs.Close();

                this.lionShape = lionShape2;
            }

        }
        public byte AlphaValue
        {
            get { return this.alpha; }
            set
            {

                this.alpha = value;

                //change alpha value
                int j = lionShape.NumPaths;
                var colorBuffer = lionShape.Colors;
                for (int i = lionShape.NumPaths - 1; i >= 0; --i)
                {
                    colorBuffer[i].alpha = alpha;
                }
            }
        }

        public override bool Move(int mouseX, int mouseY)
        {
            bool result = base.Move(mouseX, mouseY);
            this.needUpdate = true;
            return result;
        }
        public override void OnDraw(Graphics2D graphics2D)
        {

            //freeze to bitmap ?

            if (vxStorage == null)
            {
                var transform = Affine.NewMatix(
                        AffinePlan.Translate(-lionShape.Center.x, -lionShape.Center.y),
                        AffinePlan.Scale(spriteScale, spriteScale),
                        AffinePlan.Rotate(angle + Math.PI),
                        AffinePlan.Skew(skewX / 1000.0, skewY / 1000.0),
                        AffinePlan.Translate(Width / 2, Height / 2)
                );

                //convert
                //System.Collections.Generic.List<VertexData> list = new System.Collections.Generic.List<VertexData>();
                vxStorage = transform.TransformToVxs(lionShape.Path);
                //transformedPathStorage = new VertexSourceApplyTransform(lionShape.Path, transform);
                //transformedPathStorage.DoTransform(list);

                //vxStorage = new VertexStorage(list);
            }

            //graphics2D.Render(transformedPathStorage, lionShape.Colors, lionShape.PathIndexList, lionShape.NumPaths);
            graphics2D.Render(vxStorage, lionShape.Colors, lionShape.PathIndexList, lionShape.NumPaths);

            base.OnDraw(graphics2D);
            if (!IsFreezed)
            {
                //var destImage = graphics2D.DestImage;
                //var buffer = destImage.GetBuffer();
                //var w = destImage.Width;
                //var h = destImage.Height;

                ////snap to bmp
                //System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(w, h);
                //BitmapHelper.CopyToWindowsBitmap(buffer, 0,
                //    destImage.StrideInBytes(),
                //    destImage.Height,
                //    destImage.BitDepth,
                //    bmp, new RectangleInt(0, 0, w, h));

                //bmp.Save("d:\\WImageTest\\01.bmp");

                //this.Freeze();
                //var bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, w, h),
                //     System.Drawing.Imaging.ImageLockMode.ReadWrite,
                //     System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            }
            base.OnDraw(graphics2D);
        }




    }


}

