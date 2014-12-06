﻿//
// Copyright (c) 2014 The ANGLE Project Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.
//

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics;
//using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.ES20;
using Examples.Tutorial;
using Mini;



namespace OpenTkEssTest
{
    public static class ES2Utils
    {


        public static int CompileShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            int compileResult;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out compileResult);

            if (compileResult == 0)
            {
                int infoLogLength;
                GL.GetShader(shader, ShaderParameter.InfoLogLength, out infoLogLength);

                string infolog;
                GL.GetShaderInfoLog(shader, out infolog);
                GL.DeleteShader(shader);

                //std::vector<GLchar> infoLog(infoLogLength);
                //glGetShaderInfoLog(shader, infoLog.size(), NULL, &infoLog[0]);

                //std::cerr << "shader compilation failed: " << &infoLog[0];

                //glDeleteShader(shader);
                shader = 0;
            }

            return shader;
        }
        public static int CompileProgram(string vs_source, string fs_source)
        {
            int program = GL.CreateProgram();
            int vs = CompileShader(ShaderType.VertexShader, vs_source);
            int fs = CompileShader(ShaderType.FragmentShader, fs_source);

            //GLuint program = glCreateProgram();

            //GLuint vs = CompileShader(GL_VERTEX_SHADER, vsSource);
            //GLuint fs = CompileShader(GL_FRAGMENT_SHADER, fsSource);

            if (vs == 0 || fs == 0)
            {
                GL.DeleteShader(vs);
                GL.DeleteShader(fs);
                GL.DeleteProgram(program);

                return 0;
            }
            GL.AttachShader(program, vs);
            GL.DeleteShader(vs);
            //glAttachShader(program, vs);
            //glDeleteShader(vs);

            GL.AttachShader(program, fs);
            GL.DeleteShader(fs);
            //glAttachShader(program, fs);
            //glDeleteShader(fs);
            GL.LinkProgram(program);
            //glLinkProgram(program);

            int linkStatus;
            GL.GetProgram(program, ProgramParameter.LinkStatus, out linkStatus);

            //GLint linkStatus;
            //glGetProgramiv(program, GL_LINK_STATUS, &linkStatus);

            if (linkStatus == 0)
            {
                //GLint infoLogLength;
                //glGetProgramiv(program, GL_INFO_LOG_LENGTH, &infoLogLength);
                int infoLogLength;
                GL.GetProgram(program, ProgramParameter.InfoLogLength, out infoLogLength);

                string infoLog;
                GL.GetProgramInfoLog(program, out infoLog);
                //std::vector<GLchar> infoLog(infoLogLength);
                //glGetProgramInfoLog(program, infoLog.size(), NULL, &infoLog[0]);

                //std::cerr << "program link failed: " << &infoLog[0];
                GL.DeleteProgram(program);
                //glDeleteProgram(program);
                return 0;
            }

            return program;
        }
        public static int CreateSimpleTexture2D()
        {
            // Use tightly packed data
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            //glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

            // Generate a texture object
            //GLuint texture;
            int texture = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, texture);

            // Bind the texture object
            // glBindTexture(GL_TEXTURE_2D, texture);

            // Load the texture: 2x2 Image, 3 bytes per pixel (R, G, B)
            const int width = 2;
            const int height = 2;
            //        GLubyte pixels[width * height * 3] =
            //{
            //    255,   0,   0, // Red
            //      0, 255,   0, // Green
            //      0,   0, 255, // Blue
            //    255, 255,   0, // Yellow
            //};
            byte[] pixels = new byte[width * height * 3]
            {
                  255,   0,   0, // Red
                  0, 255,   0, // Green
                  0,   0, 255, // Blue
                255, 255,   0, // Yellow
            };

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);
            //glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, pixels);

            // Set the filtering mode

            //glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
            //glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            return texture;
        }
    }

}