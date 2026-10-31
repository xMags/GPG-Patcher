using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GpgPatcher.Gui
{
    /// <summary>
    /// Removes the native caption from a fixed-size frame, so Windows still provides dragging,
    /// the drop shadow and rounded corners.
    /// </summary>
    internal sealed partial class MainForm
    {
        private const int WmSysCommand = 0x0112;
        private const int ScMaximizeMask = 0xFFF0;
        private const int ScMaximize = 0xF030;
        private const int WmNcCalcSize = 0x0083;
        private const int WmNcHitTest = 0x0084;
        private const int HtClient = 1;
        private const int HtCaption = 2;
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpFrameChanged = 0x0020;

        // The layout is designed for exactly this client area.
        private static readonly Size FixedClientSize = new Size(1272, 910);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // The caption is only removed when Windows recalculates the frame.
            SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);

            // ClientSize's setter assumes a native caption, so size from the measured difference.
            Size = new Size(
                Width + FixedClientSize.Width - ClientSize.Width,
                Height + FixedClientSize.Height - ClientSize.Height);
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WmSysCommand when (m.WParam.ToInt64() & ScMaximizeMask) == ScMaximize:
                    return;
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
            client.Top = proposed.Top;
            Marshal.StructureToPtr(client, m.LParam, false);
            m.Result = IntPtr.Zero;
        }

        private int ResolveTitleBarHit(IntPtr pointParameter)
        {
            var packed = pointParameter.ToInt64();
            var screenPoint = new Point(unchecked((short)packed), unchecked((short)(packed >> 16)));
            var point = PointToClient(screenPoint);

            return windowTitleBar != null && windowTitleBar.Bounds.Contains(point) ? HtCaption : HtClient;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

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
