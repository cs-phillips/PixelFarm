﻿//2014 BSD,WinterDev   
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
namespace PixelFarm.Agg
{

    public interface ISpanInterpolator
    {
        //------------------------------------------------
        void Begin(double x, double y, int len);
        void GetCoord(out int x, out int y);
        void Next();
        //------------------------------------------------
        Transform.ITransform GetTransformer();
        void SetTransformer(Transform.ITransform trans);
        //------------------------------------------------
        void Resync(double xe, double ye, int len);
        void GetLocalScale(out int x, out int y);
    }

}