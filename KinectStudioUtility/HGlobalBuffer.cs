//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Runtime.InteropServices;

    public class HGlobalBuffer : CriticalHandle
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        public HGlobalBuffer(uint size)
            : base(IntPtr.Zero)
        {
            this.handle = Marshal.AllocHGlobal((int)size);
            if (this.handle == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            this.size = size;

            GC.AddMemoryPressure(this.size);
        }

        public override bool IsInvalid
        {
            get
            {
                lock (HGlobalBuffer.lockObj)
                {
                    return this.handle == IntPtr.Zero;
                }
            }
        }

        public IntPtr Buffer
        {
            get
            {
                lock (HGlobalBuffer.lockObj)
                {
                    return this.handle;
                }
            }
        }

        public uint Size
        {
            get
            {
                lock (HGlobalBuffer.lockObj)
                {
                    return this.size;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            ReleaseHandle();

            base.Dispose(disposing);
        }

        protected override bool ReleaseHandle()
        {
            lock (HGlobalBuffer.lockObj)
            {
                if (this.handle != IntPtr.Zero)
                {
                    GC.RemoveMemoryPressure(this.size);

                    Marshal.FreeHGlobal(handle);
                    this.handle = IntPtr.Zero;
                    this.size = 0;
                }
            }

            return true;
        }

        private uint size = 0;

        private static readonly object lockObj = new object(); // no need for a lock for each instance
    }
}
