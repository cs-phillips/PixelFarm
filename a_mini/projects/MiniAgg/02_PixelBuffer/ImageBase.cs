//2014 BSD,WinterDev   
//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# Port port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
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
using System.Runtime;

using PixelFarm.Agg;
using PixelFarm.Agg.VertexSource;
using PixelFarm.VectorMath;
using PixelFarm.Agg.Image;
namespace PixelFarm.Agg
{


    public abstract class ImageBase : IImage
    {
        public const int OrderB = 0;
        public const int OrderG = 1;
        public const int OrderR = 2;
        public const int OrderA = 3;
        const int BASE_MASK = 255;

#if DEBUG
        static int dbugTotalId;
        public readonly int dbugId = dbugGetNewDebugId();
#endif

        //--------------------------------------------
        int[] yTableArray;
        int[] xTableArray;
        protected byte[] m_ByteBuffer;
        //--------------------------------------------
        // Pointer to first pixel depending on strideInBytes and image position
        protected int bufferFirstPixel;

        int width;  // in pixels
        int height; // in pixels
        int strideInBytes; // Number of bytes per row. Can be < 0
        int m_DistanceInBytesBetweenPixelsInclusive;
        int bitDepth;

        double originX;
        double originY; 

        IPixelBlender recieveBlender;

        int changedCount = 0;

        public ImageBase()
        {

        }
        public ImageBase(int width, int height, int bitsPerPixel, IPixelBlender recieveBlender)
        {

            int scanWidthInBytes = width * (bitsPerPixel / 8);
            if (width < 1 || height < 1)
            {
                throw new ArgumentOutOfRangeException("You must have a width and height > than 0.");
            }

            if (bitsPerPixel != 32 && bitsPerPixel != 24 && bitsPerPixel != 8)
            {
                throw new Exception("Unsupported bits per pixel.");
            }
            if (scanWidthInBytes < width * (bitsPerPixel / 8))
            {
                throw new Exception("Your scan width is not big enough to hold your width and height.");
            }

            SetDimmensionAndFormat(width, height, scanWidthInBytes, bitsPerPixel, bitsPerPixel / 8);

            if (m_ByteBuffer == null || m_ByteBuffer.Length != strideInBytes * height)
            {
                m_ByteBuffer = new byte[strideInBytes * height];
                SetUpLookupTables();
            }

            if (yTableArray.Length != height
                || xTableArray.Length != width)
            {
                throw new Exception("The yTable and xTable should be allocated correctly at this point. Figure out what happend."); // LBB, don't fix this if you don't understand what it's trying to do.
            }
            //--------------------------
            SetRecieveBlender(recieveBlender);
        }
        
     
        public void MarkImageChanged()
        {
            // mark this unchecked as we don't want to throw an exception if this rolls over.
            unchecked
            {
                changedCount++;
            }
        }

#if DEBUG
        static int dbugGetNewDebugId()
        {

            return dbugTotalId++;
        }
#endif




        //public void CopyFrom(IImage sourceImage)
        //{
        //    CopyFrom(sourceImage, sourceImage.GetBounds(), 0, 0);
        //}

        void CopyFromNoClipping(IImage sourceImage, RectangleInt clippedSourceImageRect, int destXOffset, int destYOffset)
        {
            if (BytesBetweenPixelsInclusive != BitDepth / 8
                || sourceImage.BytesBetweenPixelsInclusive != sourceImage.BitDepth / 8)
            {
                throw new Exception("WIP we only support packed pixel formats at this time.");
            }

            if (BitDepth == sourceImage.BitDepth)
            {
                int lengthInBytes = clippedSourceImageRect.Width * BytesBetweenPixelsInclusive;

                int sourceOffset = sourceImage.GetBufferOffsetXY(clippedSourceImageRect.Left, clippedSourceImageRect.Bottom);
                byte[] sourceBuffer = sourceImage.GetBuffer();
                int destOffset;
                byte[] destBuffer = GetPixelPointerXY(clippedSourceImageRect.Left + destXOffset, clippedSourceImageRect.Bottom + destYOffset, out destOffset);

                for (int i = 0; i < clippedSourceImageRect.Height; i++)
                {
                    AggBasics.memmove(destBuffer, destOffset, sourceBuffer, sourceOffset, lengthInBytes);
                    sourceOffset += sourceImage.Stride;
                    destOffset += Stride;
                }
            }
            else
            {
                bool haveConversion = true;
                switch (sourceImage.BitDepth)
                {
                    case 24:
                        switch (BitDepth)
                        {
                            case 32:
                                {
                                    int numPixelsToCopy = clippedSourceImageRect.Width;
                                    for (int i = clippedSourceImageRect.Bottom; i < clippedSourceImageRect.Top; i++)
                                    {
                                        int sourceOffset = sourceImage.GetBufferOffsetXY(clippedSourceImageRect.Left, clippedSourceImageRect.Bottom + i);
                                        byte[] sourceBuffer = sourceImage.GetBuffer();
                                        int destOffset;
                                        byte[] destBuffer = GetPixelPointerXY(
                                            clippedSourceImageRect.Left + destXOffset,
                                            clippedSourceImageRect.Bottom + i + destYOffset,
                                            out destOffset);
                                        for (int x = 0; x < numPixelsToCopy; x++)
                                        {
                                            destBuffer[destOffset++] = sourceBuffer[sourceOffset++];
                                            destBuffer[destOffset++] = sourceBuffer[sourceOffset++];
                                            destBuffer[destOffset++] = sourceBuffer[sourceOffset++];
                                            destBuffer[destOffset++] = 255;
                                        }
                                    }
                                }
                                break;

                            default:
                                haveConversion = false;
                                break;
                        }
                        break;

                    default:
                        haveConversion = false;
                        break;
                }

                if (!haveConversion)
                {
                    throw new NotImplementedException("You need to write the " + sourceImage.BitDepth.ToString() + " to " + BitDepth.ToString() + " conversion");
                }
            }
        }

        public void CopyFrom(IImage sourceImage, RectangleInt sourceImageRect, int destXOffset, int destYOffset)
        {
            RectangleInt sourceImageBounds = sourceImage.GetBounds();
            RectangleInt clippedSourceImageRect = new RectangleInt();
            if (clippedSourceImageRect.IntersectRectangles(sourceImageRect, sourceImageBounds))
            {
                RectangleInt destImageRect = clippedSourceImageRect;
                destImageRect.Offset(destXOffset, destYOffset);
                RectangleInt destImageBounds = GetBounds();
                RectangleInt clippedDestImageRect = new RectangleInt();
                if (clippedDestImageRect.IntersectRectangles(destImageRect, destImageBounds))
                {
                    // we need to make sure the source is also clipped to the dest. So, we'll copy this back to source and offset it.
                    clippedSourceImageRect = clippedDestImageRect;
                    clippedSourceImageRect.Offset(-destXOffset, -destYOffset);
                    CopyFromNoClipping(sourceImage, clippedSourceImageRect, destXOffset, destYOffset);
                }
            }
        }


        //public void GetOriginOffset(out double x, out double y)
        //{
        //    x = this.originX;
        //    y = this.originY;
        //}
        //public void SetOriginOffset(double x, double y)
        //{
        //    this.originX = x;
        //    this.originY = y;
        //}

        public int Width
        {
            get
            {
                return width;
            }
        }

        public int Height
        {
            get
            {
                return height;
            }
        }

        public int Stride { get { return strideInBytes; } }

        public int BytesBetweenPixelsInclusive 
        {
            get { return m_DistanceInBytesBetweenPixelsInclusive; }
        }
        public int BitDepth
        {
            get { return bitDepth; }
        }

        public RectangleInt GetBounds()
        {
            return new RectangleInt(
                    -(int)this.originX,
                    -(int)this.originY,
                    Width - (int)this.originX,
                    Height - (int)this.originY);
        }

        public IPixelBlender GetRecieveBlender()
        {
            return recieveBlender;
        }

        public void SetRecieveBlender(IPixelBlender value)
        {
            if (BitDepth != 0 && value != null && value.NumPixelBits != BitDepth)
            {
                throw new NotSupportedException("The blender has to support the bit depth of this image.");
            }
            recieveBlender = value;
        }

        protected void SetUpLookupTables()
        {
            yTableArray = new int[height];
            xTableArray = new int[width];
            unsafe
            {
                fixed (int* first = &yTableArray[0])
                {
                    //go last
                    int* cur = first + height;
                    for (int i = height - 1; i >= 0; )
                    {
                        //--------------------
                        *cur = i * strideInBytes;
                        --i;
                        cur--;
                        //--------------------
                    }
                }
                fixed (int* first = &xTableArray[0])
                {
                    //go last
                    int* cur = first + width;
                    //even
                    for (int i = width - 1; i >= 0; )
                    {
                        //--------------------
                        *cur = i * m_DistanceInBytesBetweenPixelsInclusive;
                        --i;
                        cur--;
                        //--------------------
                    }
                }
            }

        }



        //void DeallocateOrClearBuffer(int width, int height, int strideInBytes, int bitDepth, int distanceInBytesBetweenPixelsInclusive)
        //{
        //    if (this.width == width
        //        && this.height == height
        //        && this.strideInBytes == strideInBytes
        //        && this.bitDepth == bitDepth
        //        && m_DistanceInBytesBetweenPixelsInclusive == distanceInBytesBetweenPixelsInclusive
        //        && m_ByteBuffer != null)
        //    {
        //        for (int i = m_ByteBuffer.Length - 1; i >= 0; --i)
        //        {
        //            m_ByteBuffer[i] = 0;
        //        }

        //        return;
        //    }
        //    else
        //    {
        //        if (m_ByteBuffer != null)
        //        {
        //            m_ByteBuffer = null;
        //            SetDimmensionAndFormat(0, 0, 0, 32, 4, true);
        //        }
        //    }
        //}

        protected void SetDimmensionAndFormat(int width, int height, int strideInBytes,
            int bitDepth,
            int distanceInBytesBetweenPixelsInclusive)
        {
            //if (doDeallocateOrClearBuffer)
            //{
            //    DeallocateOrClearBuffer(width, height, strideInBytes, bitDepth, distanceInBytesBetweenPixelsInclusive);
            //}

            this.width = width;
            this.height = height;
            this.strideInBytes = strideInBytes;
            this.bitDepth = bitDepth;
            if (distanceInBytesBetweenPixelsInclusive > 4)
            {
                throw new System.Exception("It looks like you are passing bits per pixel rather than distance in bytes.");
            }
            if (distanceInBytesBetweenPixelsInclusive < (bitDepth / 8))
            {
                throw new Exception("You do not have enough room between pixels to support your bit depth.");
            }
            m_DistanceInBytesBetweenPixelsInclusive = distanceInBytesBetweenPixelsInclusive;
            if (strideInBytes < distanceInBytesBetweenPixelsInclusive * width)
            {
                throw new Exception("You do not have enough strideInBytes to hold the width and pixel distance you have described.");
            }
        }

        public byte[] GetBuffer()
        {
            return m_ByteBuffer;
        }
        public byte[] GetPixelPointerY(int y, out int bufferOffset)
        {
            bufferOffset = bufferFirstPixel + yTableArray[y];
            return m_ByteBuffer;
        }

        public byte[] GetPixelPointerXY(int x, int y, out int bufferOffset)
        {
            bufferOffset = GetBufferOffsetXY(x, y);
            return m_ByteBuffer;
        }

        public static void CopySubBufferToInt32Array(ImageBase buff, int mx, int my, int w, int h, int[] buffer)
        {
            int i = 0;
            byte[] mBuffer = buff.m_ByteBuffer;

            for (int y = my; y < h; ++y)
            {

                int xbufferOffset = buff.GetBufferOffsetXY(0, y);
                for (int x = mx; x < w; ++x)
                {
                    //rgba 
                    byte r = mBuffer[xbufferOffset + 2];
                    byte g = mBuffer[xbufferOffset + 1];
                    byte b = mBuffer[xbufferOffset];

                    xbufferOffset += 4;
                    buffer[i] = b | (g << 8) | (r << 16);
                    i++;
                }
            }
        }

        public ColorRGBA GetPixel(int x, int y)
        {
            return recieveBlender.PixelToColorRGBA_Bytes(m_ByteBuffer, GetBufferOffsetXY(x, y));
        }


        public int GetBufferOffsetXY(int x, int y)
        {
            return bufferFirstPixel + yTableArray[y] + xTableArray[x];
        }

        //public void CopyPixel(int x, int y, byte[] c, int ByteOffset)
        //{
        //    throw new System.NotImplementedException();
        //    //byte* p = GetPixelPointerXY(x, y);
        //    //((int*)p)[0] = ((int*)c)[0];
        //    //p[OrderR] = c.r;
        //    //p[OrderG] = c.g;
        //    //p[OrderB] = c.b;
        //    //p[OrderA] = c.a;
        //}

        //public void BlendPixel(int x, int y, ColorRGBA c, byte cover)
        //{
        //    throw new System.NotImplementedException();
        //    /*
        //    cob_type::copy_or_blend_pix(
        //        (value_type*)m_rbuf->row_ptr(x, y, 1)  + x + x + x, 
        //        c.r, c.g, c.b, c.a, 
        //        cover);*/
        //}

        public void SetPixel(int x, int y, ColorRGBA color)
        {
            x -= (int)this.originX;
            y -= (int)this.originY;
            recieveBlender.CopyPixel(GetBuffer(), GetBufferOffsetXY(x, y), color);
        }

        public void CopyHL(int x, int y, int len, ColorRGBA sourceColor)
        {
            int bufferOffset;
            byte[] buffer = GetPixelPointerXY(x, y, out bufferOffset);
            recieveBlender.CopyPixels(buffer, bufferOffset, sourceColor, len);
        }

        public void CopyVL(int x, int y, int len, ColorRGBA sourceColor)
        {
            throw new NotImplementedException();
#if false
            int scanWidth = StrideInBytes();
            byte* pDestBuffer = GetPixelPointerXY(x, y);
            do
            {
                m_Blender.CopyPixel(pDestBuffer, sourceColor);
                pDestBuffer = &pDestBuffer[scanWidth];
            }
            while (--len != 0);
#endif
        }


        public void BlendHL(int x1, int y, int x2, ColorRGBA sourceColor, byte cover)
        {
            if (sourceColor.alpha != 0)
            {
                int len = x2 - x1 + 1;

                int bufferOffset;
                byte[] buffer = GetPixelPointerXY(x1, y, out bufferOffset);

                int alpha = (((int)(sourceColor.alpha) * (cover + 1)) >> 8);
                if (alpha == BASE_MASK)
                {
                    recieveBlender.CopyPixels(buffer, bufferOffset, sourceColor, len);
                }
                else
                {
                    do
                    {
                        recieveBlender.BlendPixel(buffer, bufferOffset,
                            new ColorRGBA(sourceColor, alpha));

                        bufferOffset += m_DistanceInBytesBetweenPixelsInclusive;
                    }
                    while (--len != 0);
                }
            }
        }

        public void BlendVL(int x, int y1, int y2, ColorRGBA sourceColor, byte cover)
        {
            throw new NotImplementedException();
#if false
            int ScanWidth = StrideInBytes();
            if (sourceColor.m_A != 0)
            {
                unsafe
                {
                    int len = y2 - y1 + 1;
                    byte* p = GetPixelPointerXY(x, y1);
                    sourceColor.m_A = (byte)(((int)(sourceColor.m_A) * (cover + 1)) >> 8);
                    if (sourceColor.m_A == base_mask)
                    {
                        byte cr = sourceColor.m_R;
                        byte cg = sourceColor.m_G;
                        byte cb = sourceColor.m_B;
                        do
                        {
                            m_Blender.CopyPixel(p, sourceColor);
                            p = &p[ScanWidth];
                        }
                        while (--len != 0);
                    }
                    else
                    {
                        if (cover == 255)
                        {
                            do
                            {
                                m_Blender.BlendPixel(p, sourceColor);
                                p = &p[ScanWidth];
                            }
                            while (--len != 0);
                        }
                        else
                        {
                            do
                            {
                                m_Blender.BlendPixel(p, sourceColor);
                                p = &p[ScanWidth];
                            }
                            while (--len != 0);
                        }
                    }
                }
            }
#endif
        }

        public void BlendSolidHSpan(int x, int y, int len, ColorRGBA sourceColor, byte[] covers, int coversIndex)
        {
            int colorAlpha = sourceColor.alpha;
            if (colorAlpha != 0)
            {
                unchecked
                {
                    int bufferOffset;
                    byte[] buffer = GetPixelPointerXY(x, y, out bufferOffset);

                    do
                    {
                        int alpha = ((colorAlpha) * ((covers[coversIndex]) + 1)) >> 8;
                        if (alpha == BASE_MASK)
                        {
                            recieveBlender.CopyPixel(buffer, bufferOffset, sourceColor);
                        }
                        else
                        {
                            recieveBlender.BlendPixel(buffer, bufferOffset, new ColorRGBA(sourceColor, alpha));
                        }
                        bufferOffset += m_DistanceInBytesBetweenPixelsInclusive;
                        coversIndex++;
                    }
                    while (--len != 0);
                }
            }
        }

        public void BlendSolidVSpan(int x, int y, int len, ColorRGBA sourceColor, byte[] covers, int coversIndex)
        {
            if (sourceColor.alpha != 0)
            {
                int scanWidthBytes = Stride;
                unchecked
                {
                    int bufferOffset = GetBufferOffsetXY(x, y);
                    do
                    {
                        byte oldAlpha = sourceColor.alpha;
                        sourceColor.alpha = (byte)(((int)(sourceColor.alpha) * ((int)(covers[coversIndex++]) + 1)) >> 8);
                        if (sourceColor.alpha == BASE_MASK)
                        {
                            recieveBlender.CopyPixel(m_ByteBuffer, bufferOffset, sourceColor);
                        }
                        else
                        {
                            recieveBlender.BlendPixel(m_ByteBuffer, bufferOffset, sourceColor);
                        }
                        bufferOffset += scanWidthBytes;
                        sourceColor.alpha = oldAlpha;
                    }
                    while (--len != 0);
                }
            }
        }

        public void CopyColorHSpan(int x, int y, int len, ColorRGBA[] colors, int colorsIndex)
        {
            int bufferOffset = GetBufferOffsetXY(x, y); 
            do
            {
                recieveBlender.CopyPixel(m_ByteBuffer, bufferOffset, colors[colorsIndex]); 
                ++colorsIndex;
                bufferOffset += m_DistanceInBytesBetweenPixelsInclusive;
            }
            while (--len != 0);
        }

        public void CopyColorVSpan(int x, int y, int len, ColorRGBA[] colors, int colorsIndex)
        {
            int bufferOffset = GetBufferOffsetXY(x, y);

            do
            {
                recieveBlender.CopyPixel(m_ByteBuffer, bufferOffset, colors[colorsIndex]);
                ++colorsIndex;
                bufferOffset += strideInBytes;
            }
            while (--len != 0);
        }

        public void BlendColorHSpan(int x, int y, int len, ColorRGBA[] colors, int colorsIndex, byte[] covers, int coversIndex, bool firstCoverForAll)
        {
            int bufferOffset = GetBufferOffsetXY(x, y);
            recieveBlender.BlendPixels(m_ByteBuffer, bufferOffset, colors, colorsIndex, covers, coversIndex, firstCoverForAll, len);
        }

        public void BlendColorVSpan(int x, int y, int len, ColorRGBA[] colors, int colorsIndex, byte[] covers, int coversIndex, bool firstCoverForAll)
        {
            int bufferOffset = GetBufferOffsetXY(x, y);

            int scanWidthBytes = System.Math.Abs(Stride);
            if (!firstCoverForAll)
            {
                do
                {
                    DoCopyOrBlend.BasedOnAlphaAndCover(recieveBlender, m_ByteBuffer, bufferOffset, colors[colorsIndex], covers[coversIndex++]);
                    bufferOffset += scanWidthBytes;
                    ++colorsIndex;
                }
                while (--len != 0);
            }
            else
            {
                if (covers[coversIndex] == 255)
                {
                    do
                    {
                        DoCopyOrBlend.BasedOnAlpha(recieveBlender, m_ByteBuffer, bufferOffset, colors[colorsIndex]);
                        bufferOffset += scanWidthBytes;
                        ++colorsIndex;
                    }
                    while (--len != 0);
                }
                else
                {
                    do
                    {

                        DoCopyOrBlend.BasedOnAlphaAndCover(recieveBlender, m_ByteBuffer, bufferOffset, colors[colorsIndex], covers[coversIndex]);
                        bufferOffset += scanWidthBytes;
                        ++colorsIndex;
                    }
                    while (--len != 0);
                }
            }
        }

        //public void apply_gamma_inv(GammaLookUpTable g)
        //{
        //    throw new System.NotImplementedException();
        //    //for_each_pixel(apply_gamma_inv_rgba<color_type, order_type, GammaLut>(g));
        //}

        public bool IsPixelVisible(int x, int y)
        {
            ColorRGBA pixelValue = GetRecieveBlender().PixelToColorRGBA_Bytes(m_ByteBuffer, GetBufferOffsetXY(x, y));
            return (pixelValue.Alpha0To255 != 0 || pixelValue.Red0To255 != 0 || pixelValue.Green0To255 != 0 || pixelValue.Blue0To255 != 0);
        }


        //public override int GetHashCode()
        //{
        //    // This might be hard to make fast and usefull.
        //    return m_ByteBuffer.GetHashCode() ^ bufferOffset.GetHashCode() ^ bufferFirstPixel.GetHashCode();
        //}

        public RectangleInt GetBoundingRect()
        {
            return new RectangleInt(0, 0, Width, Height);
        }

        //internal void Initialize(BufferImage sourceImage)
        //{
        //    RectangleInt sourceBoundingRect = sourceImage.GetBoundingRect();

        //    Initialize(sourceImage, sourceBoundingRect);
        //    OriginOffset = sourceImage.OriginOffset;
        //}
        //internal void Initialize(BufferImage sourceImage, RectangleInt boundsToCopyFrom)
        //{
        //    if (sourceImage == this)
        //    {
        //        throw new Exception("We do not create a temp buffer for this to work.  You must have a source distinct from the dest.");
        //    }
        //    Deallocate();
        //    Allocate(boundsToCopyFrom.Width, boundsToCopyFrom.Height, boundsToCopyFrom.Width * sourceImage.BitDepth / 8, sourceImage.BitDepth);
        //    SetRecieveBlender(sourceImage.GetRecieveBlender());

        //    if (width != 0 && height != 0)
        //    {
        //        RectangleInt DestRect = new RectangleInt(0, 0, boundsToCopyFrom.Width, boundsToCopyFrom.Height);
        //        RectangleInt AbsoluteSourceRect = boundsToCopyFrom;
        //        // The first thing we need to do is make sure the frame is cleared. LBB [3/15/2004]
        //        var graphics2D = MatterHackers.Agg.Graphics2D.CreateFromImage(this);
        //        graphics2D.Clear(new RGBA_Bytes(0, 0, 0, 0));

        //        int x = -boundsToCopyFrom.Left - (int)sourceImage.OriginOffset.x;
        //        int y = -boundsToCopyFrom.Bottom - (int)sourceImage.OriginOffset.y;

        //        graphics2D.Render(sourceImage, x, y, 0, 1, 1);
        //    }
        //}


    }




    static class DoCopyOrBlend
    {


        public static void BasedOnAlpha(IPixelBlender recieveBlender, byte[] destBuffer, int bufferOffset, ColorRGBA sourceColor)
        {
            //if (sourceColor.m_A != 0)
            {
#if false // we blend regardless of the alpha so that we can get Light Opacity working (used this way we have addative and faster blending in one blender) LBB
                if (sourceColor.m_A == base_mask)
                {
                    Blender.CopyPixel(pDestBuffer, sourceColor);
                }
                else
#endif
                {
                    recieveBlender.BlendPixel(destBuffer, bufferOffset, sourceColor);
                }
            }
        }

        public static void BasedOnAlphaAndCover(IPixelBlender recieveBlender, byte[] destBuffer, int bufferOffset, ColorRGBA sourceColor, int cover)
        {
            if (cover == 255)
            {
                BasedOnAlpha(recieveBlender, destBuffer, bufferOffset, sourceColor);
            }
            else
            {
                //if (sourceColor.m_A != 0)
                {
                    sourceColor.alpha = (byte)((sourceColor.alpha * (cover + 1)) >> 8);
#if false // we blend regardless of the alpha so that we can get Light Opacity working (used this way we have addative and faster blending in one blender) LBB
                    if (sourceColor.m_A == base_mask)
                    {
                        Blender.CopyPixel(pDestBuffer, sourceColor);
                    }
                    else
#endif
                    {
                        recieveBlender.BlendPixel(destBuffer, bufferOffset, sourceColor);
                    }
                }
            }
        }
    }
}