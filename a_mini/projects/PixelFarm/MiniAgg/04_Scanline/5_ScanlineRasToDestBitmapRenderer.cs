//BSD, 2014-2017, WinterDev

//MatterHackers
//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// Permission to copy, use, modify, sell and distribute this software 
// is granted provided this copyright notice appears in all copies. 
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------

using System;
using PixelFarm.Drawing;
using PixelFarm.Agg.Imaging;
namespace PixelFarm.Agg
{
    public enum ScanlineRenderMode
    {
        Default,
        Custom,
        SubPixelRendering
    }


    public partial class ScanlineSubPixelRasterizer
    {
        //this class design for render 32 bits RGBA  

        ForwardTemporaryBuffer _forwardTempBuff = new ForwardTemporaryBuffer();
        /// grey scale 4, 1/9 lcd lookup table
        /// </summary> 
        static readonly LcdDistributionLut s_g4_1_3LcdLut = new LcdDistributionLut(LcdDistributionLut.GrayLevels.Gray4, 1f / 3f, 2f / 9f, 1f / 9f);
        /// <summary>
        /// grey scale 4, 1/8 lcd lookup table
        /// </summary>
        static readonly LcdDistributionLut s_g4_1_2LcdLut = new LcdDistributionLut(LcdDistributionLut.GrayLevels.Gray4, 1f / 2f, 1f / 4f, 1f / 8f);
        Color _color;

        const int BASE_MASK = 255;
        //in this case EXISTING_A (existing alpha always 0) 
        //const int EXISTING_A = 0; 
        /// <summary>
        /// forward grayscale buffer
        /// </summary>
        ForwardTemporaryBuffer _forwardBuffer = new ForwardTemporaryBuffer();
        /// <summary>
        /// single line gray-scale buffer(8 bits) 
        /// </summary>
        SingleLineBuffer _grayScaleLine = new SingleLineBuffer();
        LcdDistributionLut _currentLcdLut = null;

        internal ScanlineSubPixelRasterizer()
        {
            //default
            _currentLcdLut = s_g4_1_2LcdLut;
        }
        public void RenderScanline(
            IImageReaderWriter dest,
            ScanlineRasterizer sclineRas,
            Scanline scline,
            Color color)
        {

#if DEBUG
            int dbugMinScanlineCount = 0;
#endif

            //1. ensure single line buffer width
            _grayScaleLine.EnsureLineStride(dest.Width + 4);
            //2. setup vars
            byte[] dest_buffer = dest.GetBuffer();
            int dest_w = dest.Width;
            int dest_h = dest.Height;
            int dest_stride = dest.Stride;
            int src_w = dest_w;
            int src_stride = dest_stride;
            //*** set color before call Blend()
            this._color = color;

            byte color_alpha = color.alpha;
            //---------------------------
            //3. loop, render single scanline with subpixel rendering 

            byte[] lineBuff = _grayScaleLine.GetInternalBuffer();

            while (sclineRas.SweepScanline(scline))
            {
                //3.1. clear 
                _grayScaleLine.Clear();
                //3.2. write grayscale span to temp buffer
                //3.3 convert to subpixel value and write to dest buffer 
                //render solid single scanline 
                int num_spans = scline.SpanCount;
                byte[] covers = scline.GetCovers();
                //render each span in the scanline
                for (int i = 1; i <= num_spans; ++i)
                {
                    ScanlineSpan span = scline.GetSpan(i);
                    if (span.len > 0)
                    {
                        //positive len  
                        _grayScaleLine.SubPixBlendSolidHSpan(span.x, span.len, color_alpha, covers, span.cover_index);
                    }
                    else
                    {
                        //fill the line, same coverage area
                        int x = span.x;
                        int x2 = (x - span.len - 1);
                        _grayScaleLine.SubPixBlendHL(x, x2, color_alpha, covers[span.cover_index]);
                    }
                }
                BlendScanline(dest_buffer, dest_stride, scline.Y, src_w, src_stride, lineBuff);
#if DEBUG
                dbugMinScanlineCount++;
#endif
            }
        }

        public LcdDistributionLut LcdLut
        {
            get { return _currentLcdLut; }
            set
            {
                _currentLcdLut = value;
            }
        }


        void BlendScanline(byte[] destImgBuffer, int destStride, int y, int srcW, int srcStride, byte[] grayScaleLineBuffer)
        {
            LcdDistributionLut lcdLut = _currentLcdLut;
            _forwardBuffer.Reset();
            int srcIndex = 0;
            //start pixel
            int destImgIndex = 0;
            int destX = 0;
            //-----------------
            byte color_alpha = _color.alpha;
            byte color_c0 = _color.red;
            byte color_c1 = _color.green;
            byte color_c2 = _color.blue;

            //-----------------
            //single line 
            srcIndex = 0;
            destImgIndex = (destStride * y) + (destX * 4); //4 color component


            _forwardBuffer.Reset();
            int nwidth = srcW;
            while (nwidth > 3)
            {
                //------------
                //TODO: add release mode code (optimized version)
                //1. convert from original grayscale value from lineBuff to lcd level
                //and 
                //2.
                //from single grey scale value,
                //it is expanded*** into 5 color-components 

                byte e_0, e_1, e_2; //energy 0,1,2 
                {
                    byte write0 = lcdLut.Convert255ToLevel(grayScaleLineBuffer[srcIndex]);
                    byte write1 = lcdLut.Convert255ToLevel(grayScaleLineBuffer[srcIndex + 1]);
                    byte write2 = lcdLut.Convert255ToLevel(grayScaleLineBuffer[srcIndex + 2]);

                    //0
                    _forwardBuffer.WriteAccumAndReadBack(
                        lcdLut.Tertiary(write0),
                        lcdLut.Secondary(write0),
                        lcdLut.Primary(write0),
                        out e_0);
                    //1
                    _forwardBuffer.WriteAccumAndReadBack(
                        lcdLut.Tertiary(write1),
                        lcdLut.Secondary(write1),
                        lcdLut.Primary(write1),
                        out e_1);
                    //2
                    _forwardBuffer.WriteAccumAndReadBack(
                        lcdLut.Tertiary(write2),
                        lcdLut.Secondary(write2),
                        lcdLut.Primary(write2),
                        out e_2);
                }

                //4. blend 3 pixels 
                byte exc0 = destImgBuffer[destImgIndex];//existing color
                byte exc1 = destImgBuffer[destImgIndex + 1];//existing color
                byte exc2 = destImgBuffer[destImgIndex + 2];//existing color  

                //--------------------------------------------------------
                //note: that we swap e_2 and e_0 on the fly***
                //-------------------------------------------------------- 
                destImgBuffer[destImgIndex] = (byte)((((color_c0 - exc0) * (e_2 * color_alpha)) + (exc0 << 16)) >> 16); //swap on the fly
                destImgBuffer[destImgIndex + 1] = (byte)((((color_c1 - exc1) * (e_1 * color_alpha)) + (exc1 << 16)) >> 16);
                destImgBuffer[destImgIndex + 2] = (byte)((((color_c2 - exc2) * (e_0 * color_alpha)) + (exc2 << 16)) >> 16);//swap on the fly
                //---------------------------------------------------------
                destImgIndex += 4;
                 
                srcIndex += 3;
                nwidth -= 3;
            }
            //---------
            //when finish each line
            //we must draw extened 4 pixels
            //---------
            {
                //get remaining energy from _forward buffer
                byte ec_r1, ec_r2, ec_r3, ec_r4;
                _forwardBuffer.ReadRemaining4(out ec_r1, out ec_r2, out ec_r3, out ec_r4);

                //we need 2 pixels,  
                int remaining_dest = Math.Min((srcStride - (destImgIndex + 4)), 5);
                if (remaining_dest < 1)
                {
                    return;
                }

                switch (remaining_dest)
                {
                    default: throw new NotSupportedException();
                    case 5:
                        {
                            //1st round
                            byte exc0 = destImgBuffer[destImgIndex];//existing color
                            byte exc1 = destImgBuffer[destImgIndex + 1];//existing color
                            byte exc2 = destImgBuffer[destImgIndex + 2];//existing color 

                            //--------------------------------------------------------
                            //note: that we swap ec_r3 and ec_r1 on the fly***

                            destImgBuffer[destImgIndex] = (byte)((((color_c0 - exc0) * (ec_r3 * color_alpha)) + (exc0 << 16)) >> 16); //swap on the fly
                            destImgBuffer[destImgIndex + 1] = (byte)((((color_c1 - exc1) * (ec_r2 * color_alpha)) + (exc1 << 16)) >> 16);
                            destImgBuffer[destImgIndex + 2] = (byte)((((color_c2 - exc2) * (ec_r1 * color_alpha)) + (exc2 << 16)) >> 16);//swap on the fly
                            destImgIndex += 4;


                            srcIndex += 3;
                            //--------------------------------------------------------
                            //2nd round
                            exc0 = destImgBuffer[destImgIndex];//existing color 
                            destImgBuffer[destImgIndex] = (byte)((((color_c0 - exc0) * (ec_r4 * color_alpha)) + (exc0 << 16)) >> 16);
                        }
                        break;
                    case 4:
                        {
                            //1st round
                            byte ec0 = destImgBuffer[destImgIndex];//existing color
                            byte ec1 = destImgBuffer[destImgIndex + 1];//existing color
                            byte ec2 = destImgBuffer[destImgIndex + 2];//existing color 

                            //--------------------------------------------------------
                            //note: that we swap e_2 and e_0 on the fly 

                            destImgBuffer[destImgIndex] = (byte)((((color_c0 - ec0) * (ec_r3 * color_alpha)) + (ec0 << 16)) >> 16); //swap on the fly
                            destImgBuffer[destImgIndex + 1] = (byte)((((color_c1 - ec1) * (ec_r2 * color_alpha)) + (ec1 << 16)) >> 16);
                            destImgBuffer[destImgIndex + 2] = (byte)((((color_c2 - ec2) * (ec_r1 * color_alpha)) + (ec2 << 16)) >> 16);//swap on the fly

                            destImgIndex += 4;                             
                            srcIndex += 3;
                        }
                        break;
                    case 3:
                    case 2:
                    case 1:
                    case 0:
                        //just return  
                        break;
                }
            }
        }


        //void SubPixRender(IImageReaderWriter dest, Scanline scanline, Color color)
        //{
        //    byte[] covers = scanline.GetCovers();
        //    int num_spans = scanline.SpanCount;
        //    int y = scanline.Y;
        //    byte[] buffer = dest.GetBuffer();
        //    IPixelBlender blender = dest.GetRecieveBlender();
        //    int last_x = int.MinValue;
        //    int bufferOffset = 0;
        //    //------------------------------------------
        //    Color bgColor = Color.White;
        //    float cb_R = bgColor.R / 255f;
        //    float cb_G = bgColor.G / 255f;
        //    float cb_B = bgColor.B / 255f;
        //    float cf_R = color.R / 255f;
        //    float cf_G = color.G / 255f;
        //    float cf_B = color.B / 255f;
        //    //------------------------------------------
        //    int prevCover = -1;
        //    for (int i = 1; i <= num_spans; ++i)
        //    {
        //        //render span by span  
        //        ScanlineSpan span = scanline.GetSpan(i);
        //        if (span.x != last_x + 1)
        //        {
        //            bufferOffset = dest.GetBufferOffsetXY(span.x, y);
        //        }

        //        last_x = span.x;
        //        int num_pix = span.len;
        //        if (num_pix < 0)
        //        {
        //            //special encode***
        //            num_pix = -num_pix; //make it positive value
        //            last_x += (num_pix - 1);
        //            //long span with coverage
        //            int coverageValue = covers[span.cover_index];
        //            //------------------------------------------- 
        //            if (coverageValue >= 255)
        //            {
        //                //100% cover
        //                int a = ((coverageValue + 1) * color.Alpha0To255) >> 8;
        //                Color todrawColor = Color.FromArgb(a, Color.FromArgb(color.R, color.G, color.B));
        //                while (num_pix > 0)
        //                {
        //                    blender.BlendPixel(buffer, bufferOffset, todrawColor);
        //                    bufferOffset += 4; //1 pixel 4 bytes
        //                    --num_pix;
        //                }
        //            }
        //            else
        //            {
        //                int a = ((coverageValue + 1) * color.Alpha0To255) >> 8;
        //                Color newc = Color.FromArgb(color.R, color.G, color.B);
        //                Color todrawColor = Color.FromArgb(a, newc);
        //                while (num_pix > 0)
        //                {
        //                    blender.BlendPixel(buffer, bufferOffset, todrawColor);
        //                    bufferOffset += 4; //1 pixel 4 bytes
        //                    --num_pix;
        //                }
        //            }
        //            prevCover = coverageValue;
        //        }
        //        else
        //        {
        //            int coverIndex = span.cover_index;
        //            last_x += (num_pix - 1);
        //            while (num_pix > 0)
        //            {
        //                int coverageValue = covers[coverIndex++];
        //                if (coverageValue >= 255)
        //                {
        //                    //100% cover
        //                    Color newc = Color.FromArgb(color.R, color.G, color.B);
        //                    int a = ((coverageValue + 1) * color.Alpha0To255) >> 8;
        //                    blender.BlendPixel(buffer, bufferOffset, Color.FromArgb(a, newc));
        //                    prevCover = coverageValue;
        //                }
        //                else
        //                {
        //                    //check direction : 

        //                    bool isUpHill = coverageValue >= prevCover;
        //                    //if (isUpHill != ((coverageValue % 2) > 0))
        //                    //{
        //                    //}
        //                    //---------------------------- 
        //                    byte c_r = 0, c_g = 0, c_b = 0;
        //                    //----------------------------
        //                    //assume lcd screen is RGB
        //                    float subpix_percent = ((float)(coverageValue) / 256f);
        //                    if (coverageValue < cover_1_3)
        //                    {
        //                        //assume LCD color arrangement is BGR                            
        //                        if (isUpHill)
        //                        {
        //                            c_r = bgColor.R;
        //                            c_g = bgColor.G;
        //                            c_b = (byte)(mix(cb_B, cf_B, subpix_percent) * 255);
        //                        }
        //                        else
        //                        {
        //                            c_r = (byte)(mix(cb_R, cf_R, subpix_percent) * 255);
        //                            c_g = bgColor.G;
        //                            c_b = bgColor.B;
        //                        }

        //                        int a = ((coverageValue + 1) * color.Alpha0To255) >> 8;
        //                        blender.BlendPixel(buffer, bufferOffset, Color.FromArgb(a, Color.FromArgb(c_r, c_g, c_b)));
        //                    }
        //                    else if (coverageValue < cover_2_3)
        //                    {
        //                        if (isUpHill)
        //                        {
        //                            c_r = bgColor.R;
        //                            c_g = (byte)(mix(cb_G, cf_G, subpix_percent) * 255);
        //                            c_b = (byte)(mix(cb_B, cf_B, 1) * 255);
        //                        }
        //                        else
        //                        {
        //                            c_r = (byte)(mix(cb_R, cf_R, 1) * 255);
        //                            c_g = (byte)(mix(cb_G, cf_G, subpix_percent) * 255);
        //                            c_b = bgColor.B;
        //                        }

        //                        int a = ((coverageValue + 1) * color.Alpha0To255) >> 8;
        //                        blender.BlendPixel(buffer, bufferOffset, Color.FromArgb(a, Color.FromArgb(c_r, c_g, c_b)));
        //                    }
        //                    else
        //                    {
        //                        //cover > 2/3 but not full 
        //                        if (isUpHill)
        //                        {
        //                            c_r = (byte)(mix(cb_R, cf_R, subpix_percent) * 255);
        //                            c_g = (byte)(mix(cb_G, cf_G, 1) * 255);
        //                            c_b = (byte)(mix(cb_B, cf_B, 1) * 255);
        //                        }
        //                        else
        //                        {
        //                            c_r = (byte)(mix(cb_R, cf_R, 1) * 255);
        //                            c_g = (byte)(mix(cb_G, cf_G, 1) * 255);
        //                            c_b = (byte)(mix(cb_B, cf_B, subpix_percent) * 255);
        //                        }

        //                        int a = ((coverageValue + 1) * color.Alpha0To255) >> 8;
        //                        blender.BlendPixel(buffer, bufferOffset, Color.FromArgb(a, Color.FromArgb(c_r, c_g, c_b)));
        //                    }
        //                }
        //                bufferOffset += 4; //1 pixel 4 bits 
        //                --num_pix;
        //                prevCover = coverageValue;
        //            }
        //        }
        //    }
        //} 
    }


    public partial class ScanlineSubPixelRasterizer
    {

        class SingleLineBuffer
        {
            //temporary buffer for grey scale buffer

            int stride;
            byte[] line_buffer; //buffer for 8 bits grey scale byte buffer
            public SingleLineBuffer()
            {
                //default
                EnsureLineStride(4);
            }
            public void EnsureLineStride(int stride8Bits)
            {
                this.stride = stride8Bits;
                if (line_buffer == null)
                {
                    line_buffer = new byte[stride8Bits];
                }
                else if (line_buffer.Length != stride8Bits)
                {
                    line_buffer = new byte[stride8Bits];
                }
            }
            public void Clear()
            {
                Array.Clear(line_buffer, 0, line_buffer.Length);
            }
            public byte[] GetInternalBuffer()
            {
                return this.line_buffer;
            }

            static float mix(float farColor, float nearColor, float weight)
            {
                //from ...
                //opengl es2 mix function              
                return farColor * (1f - weight) + (nearColor * weight);
            }

            public void SubPixBlendSolidHSpan(int x, int len, byte src_alpha, byte[] covers, int coversIndex)
            {
                //-------------------------------------
                //reference code:
                //int colorAlpha = sourceColor.alpha;
                //if (colorAlpha != 0)
                //{
                //    byte[] buffer = GetBuffer();
                //    int bufferOffset = GetBufferOffsetXY(x, y);
                //    do
                //    {
                //        int alpha = ((colorAlpha) * ((covers[coversIndex]) + 1)) >> 8;
                //        if (alpha == BASE_MASK)
                //        {
                //            recieveBlender.CopyPixel(buffer, bufferOffset, sourceColor);
                //        }
                //        else
                //        {
                //            recieveBlender.BlendPixel(buffer, bufferOffset, Color.FromArgb(alpha, sourceColor));
                //        }
                //        bufferOffset += m_DistanceInBytesBetweenPixelsInclusive;
                //        coversIndex++;
                //    }
                //    while (--len != 0);
                //}
                //-------------------------------------
                byte[] buffer = this.line_buffer;
                if (src_alpha != 0)
                {
                    int bufferOffset = x;
                    do
                    {
                        int alpha = ((src_alpha) * ((covers[coversIndex]) + 1)) >> 8;
                        if (alpha == BASE_MASK)
                        {
                            buffer[bufferOffset] = src_alpha;
                        }
                        else
                        {
                            //original
                            //buffer[bufferOffset] = (byte)((alpha + EXISTING_A) - ((alpha * EXISTING_A + BASE_MASK) >> (int)Color.BASE_SHIFT));
                            //since in this case EXISTING_A is always 0, so we remove it
                            buffer[bufferOffset] = (byte)((alpha) - ((BASE_MASK) >> (int)Color.BASE_SHIFT));
                        }

                        bufferOffset++;
                        coversIndex++;
                    } while (--len != 0);
                }
            }

            public void SubPixBlendHL(int x1, int x2, byte src_alpha, byte cover)
            {
                ////------------------------------------------------- 
                //reference code:
                //if (sourceColor.A == 0) { return; }
                ////------------------------------------------------- 
                //int len = x2 - x1 + 1;
                //byte[] buffer = GetBuffer();
                //int bufferOffset = GetBufferOffsetXY(x1, y);
                //int alpha = (((int)(sourceColor.A) * (cover + 1)) >> 8);
                //if (alpha == BASE_MASK)
                //{
                //    //full
                //    recieveBlender.CopyPixels(buffer, bufferOffset, sourceColor, len);
                //}
                //else
                //{
                //    Color c2 = Color.FromArgb(alpha, sourceColor);
                //    do
                //    {
                //        //copy pixel-by-pixel
                //        recieveBlender.BlendPixel(buffer, bufferOffset, c2);
                //        bufferOffset += m_DistanceInBytesBetweenPixelsInclusive;
                //    }
                //    while (--len != 0);
                //}
                ////-------------------------------------------------  



                if (src_alpha == 0) { return; }
                //------------------------------------------------- 
                int len = x2 - x1 + 1;
                int bufferOffset = x1;
                byte alpha = (byte)(((int)(src_alpha) * (cover + 1)) >> 8);
                byte[] buffer = this.line_buffer;

                if (alpha == BASE_MASK)
                {
                    //full
                    do
                    {
                        buffer[bufferOffset] = src_alpha;
                        bufferOffset++;

                    } while (--len != 0);
                }
                else
                {
                    do
                    {
                        //original
                        //buffer[bufferOffset] = (byte)((alpha + EXISTING_A) - ((alpha * EXISTING_A + BASE_MASK) >> (int)Color.BASE_SHIFT));
                        //since in this case EXISTING_A is always 0, so we remove it
                        buffer[bufferOffset] = (byte)((alpha) - ((BASE_MASK) >> (int)Color.BASE_SHIFT));
                        bufferOffset++;

                    } while (--len != 0);
                }
            }
        }

        public class ForwardTemporaryBuffer
        {
            //reuseable for subpixel grey scale buffer

            byte[] byteBuffer = new byte[5];
            int writeIndex = 0;
            public ForwardTemporaryBuffer()
            {
            }

            /// <summary>
            /// expand accumulaton data to 5 bytes
            /// </summary>
            /// <param name="v0">tertiary</param>
            /// <param name="v1">secondary</param>
            /// <param name="v2">primary</param>
            public void WriteAccumAndReadBack(byte v0, byte v1, byte v2, out byte readBack)
            {
                //-----------------------------------------------
                //0       -     1   -   2   -   1     -0
                //tertiary-secondary-primary-secondary-tertiary
                //-----------------------------------------------
                //indeed we can use loop for this,
                //but in this case we just switch it
                switch (writeIndex)
                {
                    default: throw new NotSupportedException();
                    case 0:
                        readBack = (byte)(byteBuffer[0] + v0); byteBuffer[0] = 0;//accum-read-reset
                        byteBuffer[1] += v1; byteBuffer[2] += v2;
                        byteBuffer[3] += v1; byteBuffer[4] += v0;
                        writeIndex = 1;

                        break;
                    case 1:
                        readBack = (byte)(byteBuffer[1] + v0); byteBuffer[1] = 0;//accum-read-reset
                        byteBuffer[2] += v1; byteBuffer[3] += v2;
                        byteBuffer[4] += v1; byteBuffer[0] += v0;
                        writeIndex = 2;
                        break;
                    case 2:
                        readBack = (byte)(byteBuffer[2] + v0); byteBuffer[2] = 0;//accum-read-reset
                        byteBuffer[3] += v1; byteBuffer[4] += v2;
                        byteBuffer[0] += v1; byteBuffer[1] += v0;
                        writeIndex = 3;
                        break;
                    case 3:
                        readBack = (byte)(byteBuffer[3] + v0); byteBuffer[3] = 0; //accum-read-reset
                        byteBuffer[4] += v1; byteBuffer[0] += v2;
                        byteBuffer[1] += v1; byteBuffer[2] += v0;
                        writeIndex = 4;
                        break;
                    case 4:
                        readBack = (byte)(byteBuffer[4] + v0); byteBuffer[4] = 0; //accum-read-reset
                        byteBuffer[0] += v1; byteBuffer[1] += v2;
                        byteBuffer[2] += v1; byteBuffer[3] += v0;
                        writeIndex = 0;
                        break;
                }
            }
            public void Reset()
            {
                writeIndex = 0;
                byteBuffer[0] = byteBuffer[1] = byteBuffer[2] = byteBuffer[3] = byteBuffer[4] = 0;
            }

            public void ReadRemaining4(out byte v0, out byte v1, out byte v2, out byte v3)
            {
                //not clear byte,
                //not move read index
                switch (writeIndex)
                {
                    default: throw new NotSupportedException();
                    case 0:
                        v0 = byteBuffer[0]; v1 = byteBuffer[1]; v2 = byteBuffer[2];
                        v3 = byteBuffer[3];
                        break;
                    case 1:
                        v0 = byteBuffer[1]; v1 = byteBuffer[2]; v2 = byteBuffer[3];
                        v3 = byteBuffer[4];
                        break;
                    case 2:
                        v0 = byteBuffer[2]; v1 = byteBuffer[3]; v2 = byteBuffer[4];
                        v3 = byteBuffer[0];
                        break;
                    case 3:
                        v0 = byteBuffer[3]; v1 = byteBuffer[4]; v2 = byteBuffer[0];
                        v3 = byteBuffer[1];
                        break;
                    case 4:
                        v0 = byteBuffer[4]; v1 = byteBuffer[0]; v2 = byteBuffer[1];
                        v3 = byteBuffer[2];
                        break;
                }
            }
        }


    }
    /// <summary>
    /// scanline rasterizer TO DESTINATION bitmap
    /// </summary>  
    public class ScanlineRasToDestBitmapRenderer
    {

        ScanlineSubPixelRasterizer scSubPixRas = new ScanlineSubPixelRasterizer();
        ArrayList<Color> tempSpanColors = new ArrayList<Color>();
        public ScanlineRasToDestBitmapRenderer()
        {
        }
        public ScanlineRenderMode ScanlineRenderMode
        {
            get;
            set;
        }

        public void RenderWithColor(IImageReaderWriter dest,
                ScanlineRasterizer sclineRas,
                Scanline scline,
                Color color)
        {
            if (!sclineRas.RewindScanlines()) { return; } //early exit
            //----------------------------------------------- 
            scline.ResetSpans(sclineRas.MinX, sclineRas.MaxX);
            switch (this.ScanlineRenderMode)
            {
                default:
                    {

                        while (sclineRas.SweepScanline(scline))
                        {
                            //render solid single scanline
                            int y = scline.Y;
                            int num_spans = scline.SpanCount;
                            byte[] covers = scline.GetCovers();
                            //render each span in the scanline
                            for (int i = 1; i <= num_spans; ++i)
                            {
                                ScanlineSpan span = scline.GetSpan(i);
                                if (span.len > 0)
                                {
                                    //positive len 
                                    dest.BlendSolidHSpan(span.x, y, span.len, color, covers, span.cover_index);
                                }
                                else
                                {
                                    //fill the line, same coverage area
                                    int x = span.x;
                                    int x2 = (x - span.len - 1);
                                    dest.BlendHL(x, y, x2, color, covers[span.cover_index]);
                                }
                            }
                        }
                    }
                    break;
                case Agg.ScanlineRenderMode.SubPixelRendering:
                    {
                        scSubPixRas.RenderScanline(dest, sclineRas, scline, color);
                    }
                    break;
                case Agg.ScanlineRenderMode.Custom:
                    {
                        while (sclineRas.SweepScanline(scline))
                        {
                            CustomRenderSingleScanLine(dest, scline, color);
                        }
                    }
                    break;
            }
        }

        public void RenderWithSpan(IImageReaderWriter dest,
                ScanlineRasterizer sclineRas,
                Scanline scline,
                ISpanGenerator spanGenerator)
        {
            if (!sclineRas.RewindScanlines()) { return; } //early exit
            //-----------------------------------------------

            scline.ResetSpans(sclineRas.MinX, sclineRas.MaxX);
            spanGenerator.Prepare();
            if (dest.Stride / 4 > (tempSpanColors.AllocatedSize))
            {
                //if not enough -> alloc more
                tempSpanColors.Clear(dest.Stride / 4);
            }

            Color[] colorArray = tempSpanColors.Array;
            while (sclineRas.SweepScanline(scline))
            {
                //render single scanline 
                int y = scline.Y;
                int num_spans = scline.SpanCount;
                byte[] covers = scline.GetCovers();
                for (int i = 1; i <= num_spans; ++i)
                {
                    ScanlineSpan span = scline.GetSpan(i);
                    int x = span.x;
                    int span_len = span.len;
                    bool firstCoverForAll = false;
                    if (span_len < 0)
                    {
                        span_len = -span_len;
                        firstCoverForAll = true;
                    } //make absolute value

                    //1. generate colors -> store in colorArray
                    spanGenerator.GenerateColors(colorArray, 0, x, y, span_len);
                    //2. blend color in colorArray to destination image
                    dest.BlendColorHSpan(x, y, span_len,
                        colorArray, 0,
                        covers, span.cover_index,
                        firstCoverForAll);
                }
            }
        }
        protected virtual void CustomRenderSingleScanLine(
            IImageReaderWriter dest,
            Scanline scline,
            Color color)
        {
            //implement
        }
    }


    //----------------------------
    public class CustomScanlineRasToDestBitmapRenderer : ScanlineRasToDestBitmapRenderer
    {
    }



    public class LcdDistributionLut
    {

        // Sub-pixel energy distribution lookup table.
        // See description by Steve Gibson: http://grc.com/cttech.htm
        // The class automatically normalizes the coefficients
        // in such a way that primary + 2*secondary + 3*tertiary = 1.0
        // Also, the input values are in range of 0...NumLevels, output ones
        // are 0...255
        //--------------------------------- 
        //template<class GgoFormat> class lcd_distribution_lut
        //{
        //public:
        //    lcd_distribution_lut(double prim, double second, double tert)
        //    {
        //        double norm = (255.0 / (GgoFormat::num_levels - 1)) / (prim + second*2 + tert*2);
        //        prim   *= norm;
        //        second *= norm;
        //        tert   *= norm;
        //        for(unsigned i = 0; i < GgoFormat::num_levels; i++)
        //        {
        //            m_primary[i]   = (unsigned char)floor(prim   * i);
        //            m_secondary[i] = (unsigned char)floor(second * i);
        //            m_tertiary[i]  = (unsigned char)floor(tert   * i);
        //        }
        //    }

        //    unsigned primary(unsigned v)   const { return m_primary[v];   }
        //    unsigned secondary(unsigned v) const { return m_secondary[v]; }
        //    unsigned tertiary(unsigned v)  const { return m_tertiary[v];  }

        //    static unsigned ggo_format()
        //    {
        //        return GgoFormat::format;
        //    }

        //private:
        //    unsigned char m_primary[GgoFormat::num_levels];
        //    unsigned char m_secondary[GgoFormat::num_levels];
        //    unsigned char m_tertiary[GgoFormat::num_levels];
        //};

        //    // Possible formats for GetGlyphOutline() and corresponding 
        //// numbers of levels of gray.
        ////---------------------------------
        //struct ggo_gray2 { enum { num_levels = 5,  format = GGO_GRAY2_BITMAP }; };
        //struct ggo_gray4 { enum { num_levels = 17, format = GGO_GRAY4_BITMAP }; };
        //struct ggo_gray8 { enum { num_levels = 65, format = GGO_GRAY8_BITMAP }; };


        public enum GrayLevels
        {
            /// <summary>
            /// 4 level grey scale (0-3)
            /// </summary>
            Gray2,
            /// <summary>
            /// 16 levels grey scale (0-15)
            /// </summary>
            Gray4,
            /// <summary>
            /// 65 levels grey scale (0-64)
            /// </summary>
            Gray8
        }

        GrayLevels grayLevel;
        byte[] m_primary;
        byte[] m_secondary;
        byte[] m_tertiary;

        //--------------------------------
        //coverage to primary,seconday,tertiary
        //this is my extension
        byte[] coverage_primary;
        byte[] coverage_secondary;
        byte[] coverage_tertiary;
        //--------------------------------
        int numLevel;

        public LcdDistributionLut(LcdDistributionLut.GrayLevels grayLevel, double prim, double second, double tert)
        {
            this.grayLevel = grayLevel;

            switch (grayLevel)
            {
                default: throw new System.NotSupportedException();
                case GrayLevels.Gray2: numLevel = 5; break;
                case GrayLevels.Gray4: numLevel = 17; break;
                case GrayLevels.Gray8: numLevel = 65; break;
            }
            m_primary = new byte[numLevel];
            m_secondary = new byte[numLevel];
            m_tertiary = new byte[numLevel];

            double norm = (255.0 / (numLevel - 1)) / (prim + second * 2 + tert * 2);
            prim *= norm;
            second *= norm;
            tert *= norm;
            for (int i = 0; i < numLevel; ++i)
            {
                m_primary[i] = (byte)Math.Floor(prim * i);
                m_secondary[i] = (byte)Math.Floor(second * i);
                m_tertiary[i] = (byte)Math.Floor(tert * i);
            }

            coverage_primary = new byte[256];
            coverage_secondary = new byte[256];
            coverage_tertiary = new byte[256];
            for (int i = 0; i < 256; ++i)
            {
                int toGreyScaleLevel = (byte)(((float)(i + 1) / 256f) * ((float)numLevel - 1));
                coverage_primary[i] = m_primary[toGreyScaleLevel];
                coverage_secondary[i] = m_secondary[toGreyScaleLevel];
                coverage_tertiary[i] = m_tertiary[toGreyScaleLevel];
            }
        }
        /// <summary>
        /// convert from original 0-255 to level for this lut
        /// </summary>
        /// <param name="orgLevel"></param>
        /// <returns></returns>
        public byte Convert255ToLevel(byte orgLevel)
        {
            return (byte)((((float)orgLevel + 1) / 256f) * (float)(numLevel - 1));
        }
        public byte Primary(int greyLevelIndex)
        {
            return m_primary[greyLevelIndex];
        }
        public byte Secondary(int greyLevelIndex)
        {
            return m_secondary[greyLevelIndex];
        }
        public byte Tertiary(int greyLevelIndex)
        {
            return m_tertiary[greyLevelIndex];
        }

        //
        public byte PrimaryFromCoverage(int coverage)
        {
            return coverage_primary[coverage];
        }

        public byte SecondayFromCoverage(int coverage)
        {
            return coverage_secondary[coverage];
        }
        public byte TertiaryFromCoverage(int coverage)
        {
            return coverage_tertiary[coverage];
        }
    }



    // This function prepares the alpha-channel information 
    //// for the glyph averaging the values in accordance with 
    //// the method suggested by Steve Gibson. The function
    //// extends the width by 4 extra pixels, 2 at the beginning 
    //// and 2 at the end. Also, it doesn't align the new width 
    //// to 4 bytes, that is, the output gm.gmBlackBoxX is the 
    //// actual width of the array.
    ////---------------------------------
    //template<class LutType>
    //void prepare_lcd_glyph(const LutType& lut, 
    //                       const unsigned char* gbuf1, 
    //                       const GLYPHMETRICS& gm, 
    //                       unsigned char* gbuf2, 
    //                       GLYPHMETRICS* gm2)
    //{
    //    unsigned src_stride = (gm.gmBlackBoxX + 3) / 4 * 4;
    //    unsigned dst_width  = src_stride + 4;
    //    memset(gbuf2, 0, dst_width * gm.gmBlackBoxY);

    //    for(unsigned y = 0; y < gm.gmBlackBoxY; ++y)
    //    {
    //        const unsigned char* src_ptr = gbuf1 + src_stride * y;
    //        unsigned char* dst_ptr = gbuf2 + dst_width * y;
    //        unsigned x;
    //        for(x = 0; x < gm.gmBlackBoxX; ++x)
    //        {
    //            unsigned v = *src_ptr++;
    //            dst_ptr[0] += lut.tertiary(v);
    //            dst_ptr[1] += lut.secondary(v);
    //            dst_ptr[2] += lut.primary(v);
    //            dst_ptr[3] += lut.secondary(v);
    //            dst_ptr[4] += lut.tertiary(v);
    //            ++dst_ptr;
    //        }
    //    }
    //    gm2->gmBlackBoxX = dst_width;
    //}

    // Blend one span into the R-G-B 24 bit frame buffer
    // For the B-G-R byte order or for 32-bit buffers modify
    // this function accordingly. The general idea is 'span' 
    // contains alpha values for individual color channels in the 
    // R-G-B order, so, for the B-G-R order you will have to 
    // choose values from the 'span' array differently
    //---------------------------------
    //void blend_lcd_span(int x, 
    //                    int y, 
    //                    const unsigned char* span, 
    //                    int width, 
    //                    const rgba& color, 
    //                    unsigned char* rgb24_buf, 
    //                    unsigned rgb24_stride)
    //{
    //    unsigned char* p = rgb24_buf + rgb24_stride * y + x;
    //    unsigned char rgb[3] = { color.r, color.g, color.b };
    //    int i = x % 3;
    //    do
    //    {
    //        int a0 = int(*span++) * color.a;
    //        *p++ = (unsigned char)((((rgb[i++] - *p) * a0) + (*p << 16)) >> 16);
    //        if(i > 2) i = 0;
    //    }
    //    while(--width);
    //}

}

