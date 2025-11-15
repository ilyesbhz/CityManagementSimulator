using System;
using System.Runtime.InteropServices;

namespace CityManager
{
    internal static class NativeMethods
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );
    }
}
