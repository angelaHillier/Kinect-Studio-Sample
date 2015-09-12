//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using Microsoft.Xbox.Tools.Shared;
    using KinectStudioUtility;

    public class TargetMostRecentlyUsedState : MostRecentlyUsedState
    {
        public TargetMostRecentlyUsedState(string targetAlias)
        {
            if (String.IsNullOrWhiteSpace(targetAlias))
            {
                throw new ArgumentNullException("targetAlias");
            }

            byte[] bytes = UnicodeEncoding.Unicode.GetBytes(targetAlias);
            this.id = Convert.ToBase64String(bytes);
            while (this.id.EndsWith("=", StringComparison.Ordinal))
            {
                this.id = this.id.Substring(0, this.id.Length - 1);
            }

            this.targetAlias = targetAlias;
        }


        public double Left { get; set; }

        public double Top { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public double NameWidth { get; set; }

        public double DateWidth { get; set; }

        public double SizeWidth { get; set; }


        [IgnoreSessionStateField]
        public string Id
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.id;
            }
        }

        [IgnoreSessionStateField]
        public string TargetAlias
        {
            get
            {
                DebugHelper.AssertUIThread();

                return this.targetAlias;
            }
        }

        private readonly string id;
        private readonly string targetAlias;
    }
}
