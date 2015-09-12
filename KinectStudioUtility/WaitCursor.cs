//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioUtility
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    public class WaitCursor : IDisposable
    {
        public WaitCursor(FrameworkElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            this.element = element;
            this.cursor = this.element.Cursor;
            this.element.Cursor = Cursors.Wait;
        }

        ~WaitCursor()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "disposing")]
        private void Dispose(bool disposing)
        {
            if (this.element != null)
            {
                this.element.Cursor = this.cursor;
                this.element = null;
            }
        }

        private FrameworkElement element;
        private Cursor cursor;
    }
}
