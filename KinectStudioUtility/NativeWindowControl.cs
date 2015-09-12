//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Windows.Interop;

    public class NativeWindowControl : HwndHost
    {
        public NativeWindowControl() 
        {
            DebugHelper.AssertUIThread();
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            DebugHelper.AssertUIThread();

            Debug.Assert(this.handle == IntPtr.Zero);

            this.handle = UnsafeNativeMethods.CreateWindowEx(0, "static", "", WS_CHILD | WS_VISIBLE | WS_DISABLED, 
                                                             0, 0, 0, 0, 
                                                             hwndParent.Handle, IntPtr.Zero, IntPtr.Zero, 0);

            return new HandleRef(this, this.handle);
        }

        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;

            switch (msg)
            {
                case WM_ERASEBKGND:
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            Debug.Assert(hwnd.Handle == this.handle);

            UnsafeNativeMethods.DestroyWindow(this.handle);

            this.handle = IntPtr.Zero;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources")]
        private IntPtr handle = IntPtr.Zero;

        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_DISABLED = 0x08000000;
        private const int WM_SIZE = 0x0005;
        private const int WM_PAINT = 0x000f;
        private const int WM_ERASEBKGND = 0x0014;
    }
}
