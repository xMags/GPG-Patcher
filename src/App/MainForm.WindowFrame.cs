using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GpgPatcher.Gui
{
    /// <summary>
    /// Removes the native caption while keeping the standard frame, so Windows still provides
    /// resizing, snapping, the drop shadow and rounded corners.
    /// </summary>
    internal sealed partial class MainForm
    {
        private const int WmNcCalcSize = 0x0083;
        private const int WmNcHitTest = 0x0084;
        private const int HtClient = 1;
        private const int HtCaption = 2;
        private const int HtTop = 12;
        private const int HtTopLeft = 13;
        private const int HtTopRight = 14;
        private const int TopResizeThickness = 6;
        private const int TopCornerResizeWidth = 16;
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpFrameChanged = 0x0020;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // The caption is only removed when Windows recalculates the frame.
            SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WmNcCalcSize when m.WParam != IntPtr.Zero:
                    RemoveCaptionFromClientCalculation(ref m);
                    return;
                case WmNcHitTest:
                    base.WndProc(ref m);
                    if (m.Result.ToInt64() == HtClient)
                    {
                        m.Result = new IntPtr(ResolveTitleBarHit(m.LParam));
                    }

                    return;
                default:
                    base.WndProc(ref m);
                    return;
            }
        }

        private void RemoveCaptionFromClientCalculation(ref Message m)
        {
            // lParam points at NCCALCSIZE_PARAMS, whose first member is the proposed window rect.
            var proposed = Marshal.PtrToStructure<NativeRect>(m.LParam);
            base.WndProc(ref m);
            var client = Marshal.PtrToStructure<NativeRect>(m.LParam);

            // A maximized window overhangs the monitor by the frame thickness on every side,
            // so the side inset Windows just computed is also the top inset we need.
            var topInset = IsZoomed(Handle) ? client.Left - proposed.Left : 0;
            client.Top = proposed.Top + topInset;
            Marshal.StructureToPtr(client, m.LParam, false);
            m.Result = IntPtr.Zero;
        }

        private int ResolveTitleBarHit(IntPtr pointParameter)
        {
            var packed = pointParameter.ToInt64();
            var screenPoint = new Point(unchecked((short)packed), unchecked((short)(packed >> 16)));
            var point = PointToClient(screenPoint);

            if (windowTitleBar == null || !windowTitleBar.Bounds.Contains(point))
            {
                return HtClient;
            }

            if (WindowState != FormWindowState.Normal || point.Y >= TopResizeThickness)
            {
                return HtCaption;
            }

            if (point.X < TopCornerResizeWidth)
            {
                return HtTopLeft;
            }

            return point.X >= ClientSize.Width - TopCornerResizeWidth ? HtTopRight : HtTop;
        }

        private void ToggleMaximized()
        {
            WindowState = WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr window);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
