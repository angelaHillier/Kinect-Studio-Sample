//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;

    public interface IPluginViewSettings : INotifyPropertyChanged
    {
        string RequirementsToolTip { get; }

        bool AreRequirementsSatisfied { get; }

        void CheckRequirementsSatisfied(HashSet<KStudioEventStreamIdentifier> availableStreamIds);

        bool IsRendingOpaque { get; }

        bool OtherIsRenderingOpaque();

        void ReadFrom(XElement element);

        void WriteTo(XElement element);
    }

    public interface IPluginEditableViewSettings : IPluginViewSettings
    {
        DataTemplate SettingsEditDataTemplate { get; }

        IPluginEditableViewSettings CloneForEdit();
    }
}
