//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    public class PlaybackFileSettings : IDisposable
    {
        public PlaybackFileSettings(IFileSettingsService fileSettingsService, KStudioClipSource clipSource)
        {
            this.Initialize(fileSettingsService, null, clipSource);
        }

        public PlaybackFileSettings(IFileSettingsService fileSettingsService, string targetAlias, KStudioClipSource clipSource)
        {
            if (String.IsNullOrWhiteSpace(targetAlias))
            {
                throw new ArgumentNullException("targetAlias");
            }

            this.Initialize(fileSettingsService, targetAlias, clipSource);
        }

        ~PlaybackFileSettings()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "targetAlias"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "clipSource"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "fileSettingsService")]
        private void Initialize(IFileSettingsService fileSettingsService, string targetAlias, KStudioClipSource clipSource)
        {
            if (clipSource == null)
            {
                throw new ArgumentNullException("clipSource");
            }

            this.fileSettingsService = fileSettingsService;
            this.targetAlias = targetAlias;
            this.clipSource = clipSource;

            if (this.fileSettingsService != null)
            {
                if (targetAlias == null)
                {
                    this.fileSettingsService.LoadSettings(this.clipSource);
                }
                else
                {
                    this.fileSettingsService.LoadSettings(this.targetAlias, this.clipSource);
                }
            }
        }

        public uint LoadSetting(string settingKey, string valueName, uint defaultValue)
        {
            Debug.Assert(this.clipSource != null);

            uint value = defaultValue;

            if (this.fileSettingsService != null)
            {
                XElement element = null;

                if (this.targetAlias == null)
                {
                    element = this.fileSettingsService.GetSettings(this.clipSource, settingKey);
                }
                else
                {
                    element = this.fileSettingsService.GetSettings(targetAlias, this.clipSource, settingKey);
                }

                value = XmlExtensions.GetAttribute(element, valueName, value);
            }

            return value;
        }

        public void SaveSetting(string settingKey, string valueName, uint value)
        {
            Debug.Assert(this.clipSource != null);


            if (this.fileSettingsService != null)
            {
                XElement element = null;

                if (this.targetAlias == null)
                {
                    element = this.fileSettingsService.GetSettings(this.clipSource, settingKey);
                }
                else
                {
                    element = this.fileSettingsService.GetSettings(targetAlias, this.clipSource, settingKey);
                }

                element.SetAttributeValue(valueName, value.ToString(CultureInfo.InvariantCulture));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IReadOnlyCollection<Tuple<KStudioEventStreamSelectorItem, bool>> GetLastStreamSelection()
        {
            List<Tuple<KStudioEventStreamSelectorItem, bool>> value = null;

            if (this.fileSettingsService != null)
            {
                XElement streamsElement = null;

                if (this.targetAlias == null)
                {
                    streamsElement = this.fileSettingsService.GetSettings(this.clipSource, "streams");
                }
                else
                {
                    streamsElement = this.fileSettingsService.GetSettings(targetAlias, this.clipSource, "streams");
                }

                if (streamsElement != null)
                {
                    value = new List<Tuple<KStudioEventStreamSelectorItem, bool>>();

                    foreach (XElement streamElement in streamsElement.Elements("stream"))
                    {
                        Guid dataTypeId = XmlExtensions.GetAttribute(streamElement, "dataTypeId", Guid.Empty);
                        Guid sourceSemanticId = XmlExtensions.GetAttribute(streamElement, "fileSemanticId", Guid.Empty);
                        Guid destinationSemanticId = XmlExtensions.GetAttribute(streamElement, "liveSemanticId", Guid.Empty);
                        bool selected = XmlExtensions.GetAttribute(streamElement, "selected", false);

                        value.Add(new Tuple<KStudioEventStreamSelectorItem, bool>(new KStudioEventStreamSelectorItem(dataTypeId, sourceSemanticId, destinationSemanticId), selected));
                    }

                    if (value.Count == 0)
                    {
                        value = null;
                    }
                }
            }

            return value;
        }

        public void SaveStreamSelection()
        {
            Debug.Assert(this.clipSource != null);

            if (this.fileSettingsService != null)
            {
                XElement streamsElement = null;

                if (this.targetAlias == null)
                {
                    streamsElement = this.fileSettingsService.GetSettings(this.clipSource, "streams");
                }
                else
                {
                    streamsElement = this.fileSettingsService.GetSettings(targetAlias, this.clipSource, "streams");
                }

                if (streamsElement != null)
                {
                    streamsElement.RemoveNodes();

                    KStudioEventFile eventFile = this.clipSource as KStudioEventFile;
                    if (eventFile != null)
                    {
                        foreach (KStudioEventStream s in eventFile.EventStreams)
                        {
                            EventStreamState ess = s.UserState as EventStreamState;
                            if (ess != null)
                            {
                                if (ess.SelectedLivePlaybackStream != null)
                                {
                                    XElement streamElement = new XElement("stream");
                                    streamElement.SetAttributeValue("dataTypeId", s.DataTypeId);
                                    streamElement.SetAttributeValue("fileSemanticId", s.SemanticId);
                                    streamElement.SetAttributeValue("liveSemanticId", ess.SelectedLivePlaybackStream.SemanticId);

                                    EventStreamState ess2 = ess.SelectedLivePlaybackStream.UserState as EventStreamState;
                                    if (ess2 != null)
                                    {
                                        if (ess2.IsSelectedForTargetPlayback)
                                        {
                                            streamElement.SetAttributeValue("selected", true.ToString());
                                        }
                                    }

                                    streamsElement.Add(streamElement);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Debug.Assert(this.clipSource != null);

                if (this.fileSettingsService != null)
                {
                    if (this.targetAlias == null)
                    {
                        this.fileSettingsService.UnloadSettings(this.clipSource);
                    }
                    else
                    {
                        this.fileSettingsService.UnloadSettings(targetAlias, this.clipSource);
                    }
                }
            }
        }

        private IFileSettingsService fileSettingsService;
        private string targetAlias;
        private KStudioClipSource clipSource;
    }
}
