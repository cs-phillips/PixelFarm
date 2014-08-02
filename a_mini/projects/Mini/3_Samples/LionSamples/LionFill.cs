/*
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
namespace MatterHackers.Agg.Sample_LionFill
{
    public class LionFillExample : ExampleBase
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
    }
    //--------------------------------------------------
    public class LionFill : BasicSprite
    {


        LionShape lionShape = new LionShape();
        Affine transform = Affine.NewIdentity();
        VertexSourceApplyTransform transformedPathStorage;

        public LionFill()
        {
            this.Width = 500;
            this.Height = 500;


            //---------------------------------------------
            //change alpha
            byte alpha = 255;// (byte)(alphaSlider.Value * 255); 
            int j = lionShape.NumPaths;
            var colorBuffer = lionShape.Colors;
            for (int i = lionShape.NumPaths - 1; i >= 0; --i)
            {
                colorBuffer[i].Alpha0To255 = alpha;
            }
            //---------------------------------------------
        }
        
        public override bool Move(int mouseX, int mouseY)
        {
            bool result = base.Move(mouseX, mouseY);
            transformedPathStorage = null;        
            return result;
        }
        public override void OnDraw(Graphics2D graphics2D)
        {

            if (transformedPathStorage == null)
            {
                transform = Affine.NewIdentity();
                transform *= Affine.NewTranslation(-lionShape.Center.x, -lionShape.Center.y);
                transform *= Affine.NewScaling(spriteScale, spriteScale);
                transform *= Affine.NewRotation(angle + Math.PI);
                transform *= Affine.NewSkewing(skewX / 1000.0, skewY / 1000.0);
                transform *= Affine.NewTranslation(Width / 2, Height / 2);
                transformedPathStorage = new VertexSourceApplyTransform(lionShape.Path, transform);
            }

            graphics2D.Render(transformedPathStorage, lionShape.Colors, lionShape.PathIndex, lionShape.NumPaths);

            base.OnDraw(graphics2D);
        }




    }


}
