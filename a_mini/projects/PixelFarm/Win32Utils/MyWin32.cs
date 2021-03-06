//MIT, 2014-2017, WinterDev
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Win32
{
    [StructLayout(LayoutKind.Sequential)]
    struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
        public BlendFunction(byte alpha)
        {
            BlendOp = 0;
            BlendFlags = 0;
            AlphaFormat = 0;
            SourceConstantAlpha = alpha;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct BitMapInfo
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
        public byte bmiColors_rgbBlue;
        public byte bmiColors_rgbGreen;
        public byte bmiColors_rgbRed;
        public byte bmiColors_rgbReserved;
    }


    /* Bitmap Header Definition */
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct BITMAP
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public short bmPlanes;
        public short bmBitsPixel;
        public void* bmBits;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct RGBQUAD
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public short bmPlanes;
        public short bmBitsPixel;
        public IntPtr bmBits;
    }


    static class MyWin32
    {
        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GlobalAlloc(int flags, int size);
        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GlobalLock(IntPtr handle);
        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern bool GlobalUnlock(IntPtr handle);
        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GlobalFree(IntPtr handle);
        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern int GetMessagePos();
        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern int GetMessageTime();
        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr GetActiveWindow();
        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, object dwProcessId);
        [DllImport("gdi32.dll")]
        public static extern int SetDCBrushColor(IntPtr hdc, int crColor);
        [DllImport("gdi32.dll")]
        public static extern int SetDCPenColor(IntPtr hdc, int crColor);
        [DllImport("gdi32.dll")]
        public static extern int GetDCBrushColor(IntPtr hdc);
        [DllImport("gdi32.dll")]
        public static extern int GetDCPenColor(IntPtr hdc);
        [DllImport("gdi32.dll")]
        public static extern int SaveDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        public static extern bool RestoreDC(IntPtr hdc, int nSaveDC);
        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern int GetDeviceCaps(IntPtr hDC, int nIndex);
        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateDC(string szdriver, string szdevice, string szoutput, IntPtr devmode);
        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hDc, IntPtr obj);
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref Win32.BitMapInfo pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);
        [DllImport("gdi32.dll")]
        public static extern unsafe int GetObject(
            IntPtr hgdiobj,
            int cbBuffer,
            void* lpvObject
        );
        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern IntPtr GetStockObject(int index);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);
        [DllImport("gdi32.dll")]
        public static extern int OffsetRgn(IntPtr hdc, int xoffset, int yoffset);
        [DllImport("gdi32.dll")]
        public static extern bool SetRectRgn(IntPtr hrgn, int left, int top, int right, int bottom);
        [DllImport("gdi32.dll")]
        public static extern int GetRgnBox(IntPtr hrgn, ref Win32Rect lprc);
        [DllImport("gdi32.dll")]
        public static extern int SelectClipRgn(IntPtr hdc, IntPtr hrgn);
        public const int NULLREGION = 1;
        public const int SIMPLEREGION = 2;
        public const int COMPLEXREGION = 3;
        [DllImport("gdi32.dll")]
        public static unsafe extern bool SetViewportOrgEx(IntPtr hdc,
            int x, int y, IntPtr expoint);
        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSource, int dwRop);
        [DllImport("gdi32.dll")]
        public static extern bool PatBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, int dwRop);
        [DllImport("Msimg32.dll")]
        public static extern bool AlphaBlend(IntPtr hdc, int nXOriginDest,
            int nYOriginDest, int nWidthDest, int nHeightDest, IntPtr hdcSrc,
            int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, ref _BLENDFUNCTION blendFunction);
        [StructLayout(LayoutKind.Sequential)]
        public struct _BLENDFUNCTION
        {
            public byte BlendOp; public byte BlendFlags; public byte SourceConstantAlpha; public byte AlphaFormat;
            public _BLENDFUNCTION(byte alphaValue)
            {
                BlendOp = AC_SRC_OVER;
                BlendFlags = 0;
                SourceConstantAlpha = alphaValue;
                AlphaFormat = 0;
            }
        }
        //
        //
        public const int AC_SRC_OVER = 0x00;
        //
        public const int AC_SRC_ALPHA = 0x01;
        [DllImport("gdi32.dll")]
        public static extern bool OffsetViewportOrgEx(IntPtr hdc, int nXOffset, int nYOffset, out IntPtr lpPoint);
        [DllImport("gdi32.dll")]
        public static unsafe extern bool GetViewportOrgEx(IntPtr hdc, MyWin32.POINT* p);
        public const int SRCCOPY = 0x00CC0020;/* dest = source                   */
        public const int SRCPAINT = 0x00EE0086;/* dest = source OR dest           */
        public const int SRCAND = 0x008800C6; /* dest = source AND dest          */
        public const int SRCINVERT = 0x008800C6;/* dest = source XOR dest          */
        public const int SRCERASE = 0x00440328; /* dest = source AND (NOT dest )   */
        public const int NOTSRCCOPY = 0x00330008; /* dest = (NOT source)             */
        public const int NOTSRCERASE = 0x001100A6; /* dest = (NOT src) AND (NOT dest) */
        public const int MERGECOPY = 0x00C000CA;/* dest = (source AND pattern)     */
        public const int MERGEPAINT = 0x00BB0226; /* dest = (NOT source) OR dest     */
        public const int PATCOPY = 0x00F00021; /* dest = pattern                  */
        public const int PATPAINT = 0x00FB0A09; /* dest = DPSnoo                   */
        public const int PATINVERT = 0x005A0049; /* dest = pattern XOR dest         */
        public const int DSTINVERT = 0x00550009; /* dest = (NOT dest)               */
        public const int BLACKNESS = 0x00000042;/* dest = BLACK                    */
        public const int WHITENESS = 0x00FF0062;/* dest = WHITE                    */
        public const int CBM_Init = 0x04;
        [DllImport("gdi32.dll")]
        public static extern bool Rectangle(IntPtr hDC, int l, int t, int r, int b);
        [DllImport("user32.dll")]
        public static extern bool FrameRect(IntPtr hDC, ref MyWin32.Win32Rect rect, IntPtr hBrush);
        [StructLayout(LayoutKind.Sequential)]
        public struct WIN32SIZE
        {
            public int Width;
            public int Height;
            public WIN32SIZE(int w, int h)
            {
                this.Width = w;
                this.Height = h;
            }
        }

        [DllImport("gdi32.dll")]
        public static extern bool GetTextExtentPoint32(IntPtr hdc, string lpstring, int c, out WIN32SIZE size);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateSolidBrush(int crColor);
        [DllImport("gdi32.dll")]
        public extern static int SetTextColor(IntPtr hdc, int newcolorRef);
        /// <summary>
        /// request font 
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public unsafe struct LOGFONT
        {
            public int lfHeight;
            public int lfWidth;
            public int lfEscapement;
            public int lfOrientation;
            public int lfWeight;
            public byte lfItalic;
            public byte lfUnderline;
            public byte lfStrikeOut;
            public byte lfCharSet;
            public byte lfOutPrecision;
            public byte lfClipPrecision;
            public byte lfQuality;
            public byte lfPitchAndFamily;
            public fixed char lfFaceName[32];//[LF_FACESIZE = 32];
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)] //need -> unicode
        public extern static IntPtr CreateFontIndirect(ref LOGFONT logFont);


        const int s_POINTS_PER_INCH = 72;
        static float ConvEmSizeInPointsToPixels(float emsizeInPoint, float pixels_per_inch)
        {
            return (int)(((float)emsizeInPoint / (float)s_POINTS_PER_INCH) * pixels_per_inch);
        }
        public static IntPtr CreateFontHelper(string fontName, float emHeight, bool bold, bool italic, float pixels_per_inch = 96)
        {
            //see: MSDN, LOGFONT structure
            //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145037(v=vs.85).aspx
            MyWin32.LOGFONT logFont = new MyWin32.LOGFONT();
            MyWin32.SetFontName(ref logFont, fontName);
            logFont.lfHeight = -(int)ConvEmSizeInPointsToPixels(emHeight, pixels_per_inch);//minus **
            logFont.lfCharSet = 1;//default
            logFont.lfQuality = 0;//default

            //
            MyWin32.LOGFONT_FontWeight weight =
                bold ?
                MyWin32.LOGFONT_FontWeight.FW_BOLD :
                MyWin32.LOGFONT_FontWeight.FW_REGULAR;
            logFont.lfWeight = (int)weight;
            //
            logFont.lfItalic = (byte)(italic ? 1 : 0);
            return MyWin32.CreateFontIndirect(ref logFont);
        }



        public static unsafe void SetFontName(ref LOGFONT logFont, string fontName)
        {
            //font name not longer than 32 chars
            char[] fontNameChars = fontName.ToCharArray();
            int j = Math.Min(fontNameChars.Length, 31);
            fixed (char* c = logFont.lfFaceName)
            {
                char* c1 = c;
                for (int i = 0; i < j; ++i)
                {
                    *c1 = fontNameChars[i];
                    c1++;
                }
            }
        }
        //LOGFONT's  font weight
        public enum LOGFONT_FontWeight
        {
            FW_DONTCARE = 0,
            FW_THIN = 100,
            FW_EXTRALIGHT = 200,
            FW_ULTRALIGHT = 200,
            FW_LIGHT = 300,
            FW_NORMAL = 400,
            FW_REGULAR = 400,
            FW_MEDIUM = 500,
            FW_SEMIBOLD = 600,
            FW_DEMIBOLD = 600,
            FW_BOLD = 700,
            FW_EXTRABOLD = 800,
            FW_ULTRABOLD = 800,
            FW_HEAVY = 900,
            FW_BLACK = 900,
        }
        public const int TA_LEFT = 0;
        public const int TA_RIGHT = 2;
        public const int TA_CENTER = 6;
        public const int TA_TOP = 0;
        public const int TA_BOTTOM = 8;
        public const int TA_BASELINE = 24;
        [DllImport("gdi32.dll")]
        public extern static uint SetTextAlign(IntPtr hdc, uint fMode);
        [DllImport("gdi32.dll")]
        public extern static uint GetTextAlign(IntPtr hdc);
        [DllImport("gdi32.dll")]
        public extern static int SetBkMode(IntPtr hdc, int mode);
        /* Background Modes */
        public const int _SetBkMode_TRANSPARENT = 1;
        public const int _SetBkMode_OPAQUE = 2;
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreatePen(int fnPenStyle, int nWidth, int crColor);
        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern int SetDIBitsToDevice(IntPtr hdc, int xdst, int ydst,
                                                int width, int height, int xsrc, int ysrc, int start, int lines,
                                                IntPtr bitsptr, IntPtr bmiptr, int color);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        [DllImport("gdi32.dll")]
        public static extern int SetDIBits(IntPtr hdc, IntPtr hBitmap, uint uStartScan, uint cScanLines, IntPtr lpbitmapArray, IntPtr lpBitmapData, uint fuColorUse);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateDIBitmap(IntPtr hdc, IntPtr lpBitmapInfo, int fdwInt, IntPtr lpbInit, IntPtr BitmapInfo, uint fuUsage);
        [DllImport("gdi32.dll")]
        public static extern int LineTo(IntPtr hdc, int nXEnd, int nYEnd);
        [DllImport("gdi32.dll")]
        public static extern bool MoveToEx(IntPtr hdc, int X, int Y, int lpPoint);
        [DllImport("gdi32.dll")]
        public static extern bool RoundRect(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nButtomRect, int nWidth, int nHeight);
        [DllImport("user32.dll")]
        public static extern int FillRect(IntPtr hdc, ref Win32Rect rect, IntPtr hBrush);
        [DllImport("gdi32.dll")]
        public static extern bool FillRgn(IntPtr hdc, IntPtr hRgn, IntPtr hBrush);
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string className, string windowName);
        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder winText, int maxCount);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, int wCmd);
        [DllImport("user32.dll")]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);
        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetTopWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern int SetWindowText(IntPtr hWnd, string text);
        [DllImport("user32.dll")]
        public static extern void ShowCaret(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool FlashWindow(IntPtr hwnd, bool bInvert);
        [DllImport("kernel32.dll")]
        public static extern int GetLastError();
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }





        [StructLayout(LayoutKind.Sequential)]
        public struct Win32Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public Win32Rect(int x, int y, int w, int h)
            {
                Left = x;
                Top = y;
                Right = x + w;
                Bottom = y + h;
            }
            public static Win32Rect FromRTLB(int left, int top, int right, int bottom)
            {
                Win32Rect rect = new Win32Rect();
                rect.Left = left;
                rect.Top = top;
                rect.Right = right;
                rect.Bottom = bottom;
                return rect;
            }

            public static readonly Win32Rect Empty = new Win32Rect(0, 0, 0, 0);
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct ABC
        {
            public int abcA;
            public uint abcB;
            public int abcC;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct ABCFLOAT
        {
            public float abcfA;
            public float abcfB;
            public float abcfC;
        }



        /*       BOOL GetCharABCWidths(
         HDC hdc,                  UINT uFirstChar,          UINT uLastChar,           LPABC lpabc             );*/
        [DllImport("gdi32.dll")]
        public static extern bool GetCharWidth32(IntPtr hdc, uint uFirstChar, uint uLastChar, ref int width);
        [DllImport("gdi32.dll")]
        public static extern bool GetCharABCWidths(IntPtr hdc, uint uFirstChar, uint uLastChar, [Out]ABC[] lpabc);
        [DllImport("gdi32.dll")]
        public static extern bool GetCharABCWidthsFloat(IntPtr hdc, uint uFirstChar, uint uLastChar, [In, Out]ABCFLOAT[] lpabc);
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT point);
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, int flags);
        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern int LoadKeyboardLayout(string pwszKLID, int flags);
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int WM_PAINT = 0x000F;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int VK_F2 = 0x71;
        public const int VK_F3 = 0x72;
        public const int VK_LEFT = 0x25;
        public const int VK_RIGHT = 0x27;
        public const int VK_F4 = 0x73;
        public const int VK_F5 = 0x74;
        public const int VK_F6 = 0x75;
        public const int VK_ESCAPE = 0x1B;
        public const int GW_HWNDFIRST = 0;
        public const int GW_HWNDLAST = 1;
        public const int GW_HWNDNEXT = 2;
        public const int GW_HWNDPREV = 3;
        public const int GW_OWNER = 4;
        public const int GW_CHILD = 5;
        public const int CB_SETCURSEL = 0x014E;
        public const int CB_SHOWDROPDOWN = 0x014F;
        public const int HWND_TOP = 0;
        public const int HWND_BOTTOM = 1;
        public const int HWND_TOPMOST = -1;
        public const int HWND_NOTOPMOST = -2;
        public const int SWP_SHOWWINDOW = 0x0040;
        public const int SWP_HIDEWINDOW = 0x0080;
        public const int SWP_NOACTIVATE = 0x0010;
        public const int MK_LBUTTON = 0x0001;
        public const int MK_RBUTTON = 0x0002;
        public const int MK_SHIFT = 0x0004;
        public const int MK_CONTROL = 0x0008;
        public const int MK_MBUTTON = 0x0010;
        public const int WM_NCMOUSEHOVER = 0x02A0;
        public const int WM_NCMOUSELEAVE = 0x02A2;
        public const int WM_NCHITTEST = 0x0084;
        public const int WM_NCPAINT = 0x0085;
        public const int WM_NCACTIVATE = 0x0086;
        public const int WM_NCMOUSEMOVE = 0x00A0;
        public const int WM_MOVING = 0x0216;
        public const int WM_ACTIVATEAPP = 0x001C;
        public static string GetWinTitle(IntPtr handle)
        {
            int winTxtLength = GetWindowTextLength(handle);
            StringBuilder stBuilder = new StringBuilder(winTxtLength + 1);
            GetWindowText(handle, stBuilder, stBuilder.Capacity);
            return stBuilder.ToString();
        }
        public static string GetClassName(IntPtr handle)
        {
            StringBuilder stBuilder = new StringBuilder(100);
            GetClassName(handle, stBuilder, stBuilder.Capacity);
            return stBuilder.ToString();
        }
        public static int SignedLOWORD(int n)
        {
            return (short)(n & 0xffff);
        }
        public static int SignedHIWORD(int n)
        {
            return (short)((n >> 0x10) & 0xffff);
        }
        public static int MAKELONG(int low, int high)
        {
            return ((high << 0x10) | (low & 0xffff));
        }

        public static int ColorToWin32(PixelFarm.Drawing.Color c)
        {
            return ((c.R | (c.G << 8)) | (c.B << 0x10));
        }

        /// <summary>
        /// Create a compatible memory HDC from the given HDC.<br/>
        /// The memory HDC can be rendered into without effecting the original HDC.<br/>
        /// The returned memory HDC and <paramref name="dib"/> must be released using <see cref="ReleaseMemoryHdc"/>.
        /// </summary>
        /// <param name="hdc">the HDC to create memory HDC from</param>
        /// <param name="width">the width of the memory HDC to create</param>
        /// <param name="height">the height of the memory HDC to create</param>
        /// <param name="dib">returns used bitmap memory section that must be released when done with memory HDC</param>
        /// <returns>memory HDC</returns>
        public static IntPtr CreateMemoryHdc(IntPtr hdc, int width, int height, out IntPtr dib, out IntPtr ppvBits)
        {
            // Create a memory DC so we can work off-screen
            IntPtr memoryHdc = MyWin32.CreateCompatibleDC(hdc);
            MyWin32.SetBkMode(memoryHdc, 1);
            // Create a device-independent bitmap and select it into our DC
            var info = new Win32.BitMapInfo();
            info.biSize = Marshal.SizeOf(info);
            info.biWidth = width;
            info.biHeight = -height;
            info.biPlanes = 1;
            info.biBitCount = 32;
            info.biCompression = 0; // BI_RGB 
            dib = MyWin32.CreateDIBSection(hdc, ref info, 0, out ppvBits, IntPtr.Zero, 0);
            MyWin32.SelectObject(memoryHdc, dib);
            return memoryHdc;
        }
        /// <summary>
        /// Release the given memory HDC and dib section created from <see cref="CreateMemoryHdc"/>.
        /// </summary>
        /// <param name="memoryHdc">Memory HDC to release</param>
        /// <param name="dib">bitmap section to release</param>
        public static void ReleaseMemoryHdc(IntPtr memoryHdc, IntPtr dib)
        {
            MyWin32.DeleteObject(dib);
            MyWin32.DeleteDC(memoryHdc);
        }
        //[DllImport("user32.dll")]
        //public static extern bool IsWindowVisible(IntPtr hWnd);
        //[DllImport("user32.dll")]
        //public static extern IntPtr WindowFromDC(IntPtr hdc);
        ///// <summary>
        ///// Retrieves the dimensions of the bounding rectangle of the specified window. The dimensions are given in screen coordinates that are relative to the upper-left corner of the screen.
        ///// </summary>
        ///// <remarks>
        ///// In conformance with conventions for the RECT structure, the bottom-right coordinates of the returned rectangle are exclusive. In other words, 
        ///// the pixel at (right, bottom) lies immediately outside the rectangle.
        ///// </remarks>
        ///// <param name="hWnd">A handle to the window.</param>
        ///// <param name="lpRect">A pointer to a RECT structure that receives the screen coordinates of the upper-left and lower-right corners of the window.</param>
        ///// <returns>If the function succeeds, the return value is nonzero.</returns>
        //[DllImport("User32", SetLastError = true)]
        //public static extern int GetWindowRect(IntPtr hWnd, out Rectangle lpRect);
        ///// <summary>
        ///// Retrieves the dimensions of the bounding rectangle of the specified window. The dimensions are given in screen coordinates that are relative to the upper-left corner of the screen.
        ///// </summary>
        ///// <remarks>
        ///// In conformance with conventions for the RECT structure, the bottom-right coordinates of the returned rectangle are exclusive. In other words, 
        ///// the pixel at (right, bottom) lies immediately outside the rectangle.
        ///// </remarks>
        ///// <param name="handle">A handle to the window.</param>
        ///// <returns>RECT structure that receives the screen coordinates of the upper-left and lower-right corners of the window.</returns>
        //public static Rectangle GetWindowRectangle(IntPtr handle)
        //{
        //    Rectangle rect;
        //    GetWindowRect(handle, out rect);
        //    return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        //}

        //[DllImport("User32.dll")]
        //public static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);
        //[DllImport("gdi32.dll")]
        //public static extern int SetBkMode(IntPtr hdc, int mode);
        //[DllImport("gdi32.dll")]
        //public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiObj);
        //[DllImport("gdi32.dll")]
        //public static extern int SetTextColor(IntPtr hdc, int color);
        //[DllImport("gdi32.dll")]
        //public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        //[DllImport("gdi32.dll")]
        //public static extern int SelectClipRgn(IntPtr hdc, IntPtr hrgn);
        //[DllImport("gdi32.dll")]
        //public static extern bool DeleteObject(IntPtr hObject);
        //[DllImport("gdi32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);
        //[DllImport("gdi32.dll", EntryPoint = "GdiAlphaBlend")]
        //public static extern bool AlphaBlend(IntPtr hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest, IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, BlendFunction blendFunction);
        //[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        //public static extern bool DeleteDC(IntPtr hdc);
        //[DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        //public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        ///// <summary>
        ///// Const for BitBlt copy raster-operation code.
        ///// </summary>
        //public const int BitBltCopy = 0x00CC0020;
        ///// <summary>
        ///// Const for BitBlt paint raster-operation code.
        ///// </summary>
        //public const int BitBltPaint = 0x00EE0086;
    }
}