﻿using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using OpenTK.Graphics.OpenGL;
using Tesselate;


namespace OpenTkEssTest
{
    public class CanvasGL2d
    {
        LayoutFarm.Drawing.Color fillColor = LayoutFarm.Drawing.Color.Black;

        public CanvasGL2d()
        {
        }
        public void Clear(LayoutFarm.Drawing.Color c)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.AccumBufferBit | ClearBufferMask.StencilBufferBit);
            GL.ClearColor(c);
        }

        static unsafe void CreateRectCoords(float* arr, byte* indices,
                float x, float y, float w, float h)
        {
            arr[0] = x; arr[1] = y;
            arr[2] = x + w; arr[3] = y;
            arr[4] = x + w; arr[5] = y + h;
            arr[6] = x; arr[7] = y + h;

            indices[0] = 0; indices[1] = 1; indices[2] = 2;
            indices[3] = 2; indices[4] = 3; indices[5] = 0;
        }
        public void FillRect(float x, float y, float w, float h)
        {

            //2d
            unsafe
            {
                float* arr = stackalloc float[8];
                byte* indices = stackalloc byte[6];
                CreateRectCoords(arr, indices, x, y, w, h);

                GL.EnableClientState(ArrayCap.VertexArray); //***
                //vertex
                GL.VertexPointer(2, VertexPointerType.Float, 0, (IntPtr)arr);
                GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedByte, (IntPtr)indices);
                GL.DisableClientState(ArrayCap.VertexArray);
            }
        }
        public void DrawLine(float x1, float y1, float x2, float y2)
        {
            unsafe
            {
                float* arr = stackalloc float[4];
                arr[0] = x1; arr[1] = y1;
                arr[2] = x2; arr[3] = y2;

                byte* indices = stackalloc byte[2];
                indices[0] = 0; indices[1] = 1;


                GL.EnableClientState(ArrayCap.VertexArray); //***
                //vertex
                GL.VertexPointer(2, VertexPointerType.Float, 0, (IntPtr)arr);
                GL.DrawElements(BeginMode.Lines, 2, DrawElementsType.UnsignedByte, (IntPtr)indices);
                GL.DisableClientState(ArrayCap.VertexArray);
            }
        }
        public void DrawImage(GLBitmapTexture bmp, float x, float y)
        {
            unsafe
            {

                GL.Enable(EnableCap.Texture2D);
                {
                    GL.BindTexture(TextureTarget.Texture2D, bmp.TextureId);
                    GL.EnableClientState(ArrayCap.TextureCoordArray); //***

                    float* arr = stackalloc float[8];
                    arr[0] = 0; arr[1] = 1;
                    arr[2] = 1; arr[3] = 1;
                    arr[4] = 1; arr[5] = 0;
                    arr[6] = 0; arr[7] = 0;

                    byte* indices = stackalloc byte[6];
                    indices[0] = 0; indices[1] = 1; indices[2] = 2;
                    indices[3] = 2; indices[4] = 3; indices[5] = 0;

                    GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, (IntPtr)arr);
                    //------------------------------------------ 
                    //fill rect with texture
                    FillRect(x, y, bmp.Width, bmp.Height);
                    GL.DisableClientState(ArrayCap.TextureCoordArray);

                } GL.Disable(EnableCap.Texture2D);
            }
        }
        public void DrawImage(GLBitmapTexture bmp, float x, float y, float w, float h)
        {
            unsafe
            {

                GL.Enable(EnableCap.Texture2D);
                {
                    GL.BindTexture(TextureTarget.Texture2D, bmp.TextureId);
                    GL.EnableClientState(ArrayCap.TextureCoordArray); //***

                    float* arr = stackalloc float[8];
                    arr[0] = 0; arr[1] = 1;
                    arr[2] = 1; arr[3] = 1;
                    arr[4] = 1; arr[5] = 0;
                    arr[6] = 0; arr[7] = 0;

                    byte* indices = stackalloc byte[6];
                    indices[0] = 0; indices[1] = 1; indices[2] = 2;
                    indices[3] = 2; indices[4] = 3; indices[5] = 0;

                    GL.TexCoordPointer(2, TexCoordPointerType.Float, 0, (IntPtr)arr);
                    //------------------------------------------ 
                    //fill rect with texture
                    FillRect(x, y, w, h);
                    GL.DisableClientState(ArrayCap.TextureCoordArray);

                } GL.Disable(EnableCap.Texture2D);
            }
        }
        public void DrawPolygon(float[] polygon2dVertices)
        {
            //closed polyline
            //draw polyline
            unsafe
            {

                int npoints = polygon2dVertices.Length / 2;
                //crete indices
                int* indices = stackalloc int[npoints * 2];
                int nn = 0;
                for (int i = 1; i < npoints; ++i)
                {
                    indices[nn++] = i - 1;
                    indices[nn++] = i;
                }
                //------------------
                //close polygon
                indices[nn++] = npoints - 1;
                indices[nn++] = 0;

                fixed (float* arr = &polygon2dVertices[0])
                {

                    GL.EnableClientState(ArrayCap.VertexArray); //***
                    //vertex 2d
                    GL.VertexPointer(2, VertexPointerType.Float, 0, (IntPtr)arr);
                    GL.DrawElements(BeginMode.Lines, npoints * 2, DrawElementsType.UnsignedInt, (IntPtr)indices);
                    GL.DisableClientState(ArrayCap.VertexArray);
                }
            }
        }
        public void FillPolygon(float[] vertex2dCoords)
        {
            //-------------
            //Tesselate
            //2d coods lis
            //n point 
            var vertextList = TessealatePolygon(vertex2dCoords);
            //-----------------------------
            FillTriangularStrip(vertextList);
            //-----------------------------
        }
        static List<Vertex> TessealatePolygon(float[] vertex2dCoords)
        {
            TessListener t01 = new TessListener();
            Tesselator tess = new Tesselator();
            int ncoords = vertex2dCoords.Length / 2;
            List<Vertex> vertexts = new List<Vertex>(ncoords);
            int nn = 0;
            for (int i = 0; i < ncoords; ++i)
            {
                vertexts.Add(new Vertex(vertex2dCoords[nn++], vertex2dCoords[nn++]));
            }
            t01.Connect(vertexts, tess, Tesselate.Tesselator.WindingRuleType.Odd, true);
            tess.BeginPolygon();
            tess.BeginContour();
            int j = vertexts.Count;
            for (int i = 0; i < j; ++i)
            {
                Vertex v = vertexts[i];
                tess.AddVertex(v.m_X, v.m_Y, 0, i);
            }
            tess.EndContour();
            tess.EndPolygon();
            return t01.resultVertexList;
        }
        static void FillTriangularStrip(List<Vertex> m_VertexList)
        {
            //convert vertex to float array
            {
                unsafe
                {
                    int j = m_VertexList.Count;
                    int j2 = j * 2;
                    float* vertices = stackalloc float[j2];
                    int nn = 0;
                    for (int i = 0; i < j; ++i)
                    {
                        var v = m_VertexList[i];
                        vertices[nn++] = (float)v.m_X;
                        vertices[nn++] = (float)v.m_Y;
                    }
                    //--------------------------------------
                    int num_indices = j - 2;

                    //int[] indx2 = new int[j ];
                    int* indx = stackalloc int[j];

                    nn = 0;//reset
                    for (int i = 0; i < num_indices; )
                    {
                        //indx2[nn++] = i;
                        //indx2[nn++] = i + 1;
                        //indx2[nn++] = i + 2;
                        indx[nn++] = i;
                        indx[nn++] = i + 1;
                        indx[nn++] = i + 2;
                        i += 3;
                    }
                    //--------------------------------------
                    GL.EnableClientState(ArrayCap.VertexArray); //***
                    //vertex 2d
                    GL.VertexPointer(2, VertexPointerType.Float, 0, (IntPtr)vertices);
                    GL.DrawElements(BeginMode.TriangleStrip, j, DrawElementsType.UnsignedInt, (IntPtr)indx);
                    GL.DisableClientState(ArrayCap.VertexArray);

                }
            }

            //wire frame
            //{

            //    int j = m_VertexList.Count;
            //    int lim = j - 2;
            //    for (int i = 0; i < lim; )
            //    {
            //        var v0 = m_VertexList[i];
            //        var v1 = m_VertexList[i + 1];
            //        var v2 = m_VertexList[i + 2];


            //        DrawLine((float)v0.m_X, (float)v0.m_Y,
            //                (float)v1.m_X, (float)v1.m_Y);
            //        DrawLine((float)v1.m_X, (float)v1.m_Y,
            //              (float)v2.m_X, (float)v2.m_Y);
            //        DrawLine((float)v2.m_X, (float)v2.m_Y,
            //             (float)v0.m_X, (float)v0.m_Y);

            //        i += 3;
            //    }
            //}
        }
        public LayoutFarm.Drawing.Color FillColor
        {
            get
            {
                return this.fillColor;
            }
            set
            {
                this.fillColor = value;
                GL.Color3(value);
            }
        }
    }



}