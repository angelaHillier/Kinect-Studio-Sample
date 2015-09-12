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

    public class EditTimeSpanDialog : EditValueDialog<TimeSpan>
    {
        protected override bool IsValidText(string text)
        {
            bool result = true;

            if (text != null)
            {
                foreach (char ch in text)
                {
                    if (!Char.IsDigit(ch) && (ch.ToString() != CultureInfo.CurrentCulture.DateTimeFormat.TimeSeparator) && (ch.ToString() != CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator))
                    {
                        result = false;
                        break;
                    }
                }
            }

            return result;
        }

        protected override TimeSpan? ParseText(string text)
        {
            TimeSpan? result = null;

            if (!String.IsNullOrWhiteSpace(text))
            {
                TimeSpan ts;
                if (TimeSpan.TryParse(text, out ts))
                {
                    result = ts;
                }
            }

            return result;
        }

        protected override string ConvertValueForString(TimeSpan value)
        {
            return value.ToString("g", CultureInfo.CurrentCulture);
        }
    }
}

namespace KinectStudioApp
{
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Xbox.Tools.Shared;

    public class SidebarTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LocalPlayback
        {
            get
            {
                return this.local;
            }

            set
            {
                this.local = value;
            }
        }

        public DataTemplate TargetPlayback
        {
            get
            {
                return this.target;
            }

            set
            {
                this.target = value;
            }
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            DataTemplate value = null;

            if (this.kstudioService == null)
            {
                this.kstudioService = ToolsUIApplication.Instance.RootServiceProvider.GetService(typeof(IKStudioService)) as IKStudioService;
            }

            if (this.kstudioService != null)
            {
                value = this.kstudioService.IsPlaybackFileOnTarget ? this.target : this.local;
            }

            return value;
        }

        private DataTemplate local = null;
        private DataTemplate target = null;
        private IKStudioService kstudioService = null;
    }
}
