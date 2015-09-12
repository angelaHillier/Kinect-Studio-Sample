//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace StandardKinectStudioPlugin
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Windows.Controls;
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;
    using Microsoft.Kinect.Tools.Helper;
    using Microsoft.Xbox.Tools.Shared;
    using nui = Microsoft.Xbox.Input.Nui;
    using viz = Microsoft.Xbox.Kinect.Viz;
    using KinectStudioPlugin;
    using KinectStudioUtility;
    using System.Windows;

    public class AudioPlugin : BasePlugin, IEventHandlerPlugin, I2DVisualPlugin, I3DVisualPlugin, IImageVisualPlugin, IDisposable
    {
        public AudioPlugin(IServiceProvider serviceProvider)
            : base(Strings.Audio_Plugin_Title, new Guid(0xf0fc9a84, 0x5be9, 0x4598, 0x8c, 0xd8, 0xbb, 0xdd, 0x47, 0x2d, 0x5f, 0xbd))
        {
            if (serviceProvider != null)
            {
                this.loggingService = serviceProvider.GetService(typeof(ILoggingService)) as ILoggingService;
            }

            this.outString = Strings.Audio_Output; 

            this.micStrings = new string[nui.Constants.AUDIO_NUM_MIC];
            for (int i = 0; i < this.micStrings.Length; ++i)
            {
                AudioTrack track = (AudioTrack)(AudioTrack.Mic0 + i);
                this.micStrings[i] = Strings.ResourceManager.GetString("Audio_" + track.ToString());
                Debug.Assert(this.micStrings[i] != null);
            }

            this.speakerStrings = new string[nui.Constants.AUDIO_NUM_SPK];
            for (int i = 0; i < this.speakerStrings.Length; ++i)
            {
                AudioTrack track = (AudioTrack)(AudioTrack.SpeakerL + i);
                this.speakerStrings[i] = Strings.ResourceManager.GetString("Audio_" + track.ToString());
                Debug.Assert(this.speakerStrings[i] != null);
            }
        }

        ~AudioPlugin()
        {
            this.Dispose(false);
        }

        public override void ReadFrom(XElement element)
        {
            base.ReadFrom(element);

            if (element != null)
            {
                lock (this.lockObj)
                {
                    this.audible = XmlExtensions.GetAttribute(element, "audible", this.audible);

                    string str = XmlExtensions.GetAttribute(element, "audibleTrack", String.Empty);
                    AudioTrack temp;
                    if (AudioTrack.TryParse(str, out temp))
                    {
                        this.audibleTrack = temp;
                    }
                }
            }
        }

        public override void WriteTo(XElement element)
        {
            base.WriteTo(element);

            if (element != null)
            {
                lock (this.lockObj)
                {
                    element.RemoveAll();

                    element.SetAttributeValue("audible", this.audible);
                    element.SetAttributeValue("audibleTrack", this.audibleTrack);
                }
            }
        }

        public bool IsAudible
        {
            get
            {
                lock (this.lockObj)
                {
                    return this.audible;
                }
            }
            set
            {
                bool doEvent = false;

                lock (this.lockObj)
                {
                    if (this.audible != value)
                    {
                        doEvent = true;
                        this.audible = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("IsAudible");
                }
            }
        }

        public AudioTrack AudibleTrack
        {
            get
            {
                lock (this.lockObj)
                {
                    return this.audibleTrack;
                }
            }
            set
            {
                bool doEvent = false;

                lock (this.lockObj)
                {
                    if (this.audibleTrack != value)
                    {
                        doEvent = true;
                        this.audibleTrack = value;
                    }
                }

                if (doEvent)
                {
                    RaisePropertyChanged("AudibleTrack");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public IEnumerable AudibleTracks
        {
            get
            {
                return Enum.GetValues(typeof(AudioTrack));
            }
        }

        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public void InitializeRender(EventType eventType, viz.Context context)
        {
            if (eventType == EventType.Monitor)
            {
                lock (this.lockObj)
                {
                    if (this.audioPlayer == null)
                    {
                        try
                        {
                            this.audioPlayer = new AudioPlayer();
                        }
                        catch (Exception)
                        {
                            if (this.loggingService != null)
                            {
                                this.loggingService.LogLine(Strings.Audio_Error_CannotInitializePlayer);
                            }
                        }
                    }

                    if (context != null)
                    {
                        this.font = new viz.Font(context);
                        this.overlay = new viz.Overlay(context);
                        const float cHorizontalFov = 70.6f;
                        float beamZ = 4.5f; // max depth in meters
                        float beamY = (float)(beamZ * Math.Tan(cHorizontalFov / 2.0));

                        viz.Vertex[] vertices = new viz.Vertex[]
                            {
                                new viz.Vertex(0,     0,      0, 0, 0, 0, 0, 0, 0xFFFFFFFF),
                                new viz.Vertex(0,  beamY, beamZ, 0, 0, 0, 0, 0, 0xFFFFFFFF),
                                new viz.Vertex(0, -beamY, beamZ, 0, 0, 0, 0, 0, 0xFFFFFFFF),
                            };

                        uint[] indices = new uint[]
                            {
                                0, 1, 2, 0, 2, 1,
                            };

                        this.beamMesh = new viz::Mesh(context, (uint)vertices.Length, (uint)indices.Length);
                        this.beamMesh.UpdateVertex(vertices);
                        this.beamMesh.UpdateIndex(indices);

                        this.outChart = new viz.TemporalChart(context);
                        this.micCharts = new viz.TemporalChart[nui.Constants.AUDIO_NUM_MIC];

                        for (int i = 0; i < this.micCharts.Length; ++i)
                        {
                            this.micCharts[i] = new viz.TemporalChart(context);
                        }

                        this.speakerCharts = new viz.TemporalChart[nui.Constants.AUDIO_NUM_SPK];
                        for (int i = 0; i < this.speakerCharts.Length; ++i)
                        {
                            this.speakerCharts[i] = new viz.TemporalChart(context);
                        }

#if TODO_AUDIO_OUT // audio out
                        if (FAILED(InitializeWASAPI()))
                        {
                            // TODO_LOG
                        }
#endif // TODO_AUDIO_OUT
                    }
                }
            }
        }

        public void UninitializeRender(EventType eventType)
        {
            if (eventType == EventType.Monitor)
            {
                lock (this.lockObj)
                {
                    this.Dispose(true);
                }
            }
        }

        public bool IsInterestedInEventsFrom(EventType eventType, Guid dataTypeId, Guid semanticId)
        {
            return
                (eventType == EventType.Monitor) &&
                ((dataTypeId == HackKStudioEventStreamDataTypeIds.SystemAudio) || (dataTypeId == HackKStudioEventStreamDataTypeIds.SystemAudioMonitor)) ||
                ((dataTypeId == HackKStudioEventStreamDataTypeIds.TitleAudio) || (dataTypeId == HackKStudioEventStreamDataTypeIds.TitleAudioMonitor));
        }

        public void ClearEvents(EventType eventType)
        {
            if (eventType == EventType.Monitor)
            {
                lock (this.lockObj)
                {
                    this.sharedAudioFrame = null;
                    this.frameTime = TimeSpan.MinValue;
                    this.beamConfidence = 0.0f;
                    this.beamAngle = 0.0f;
                }
            }
        }

        public void HandleEvent(EventType eventType, KStudioEvent eventObj)
        {
            if ((eventType == EventType.Monitor) && (eventObj != null)) 
            {
                if ((eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.SystemAudio) ||
                    (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.SystemAudioMonitor) ||
                    (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.TitleAudio) ||
                    (eventObj.EventStreamDataTypeId == HackKStudioEventStreamDataTypeIds.TitleAudioMonitor))
                    {
                    lock (this.lockObj)
                    {
                        this.sharedAudioFrame = null;
                        this.beamConfidence = 0.0f;
                        this.beamAngle = 0.0f;
                        this.frameTime = TimeSpan.Zero;

                        uint bufferSize;
                        IntPtr bufferPtr;
                        eventObj.AccessUnderlyingEventDataBuffer(out bufferSize, out bufferPtr);

                        if (bufferSize >= cAudioFrameSizeMinimum)
                        {
                            this.sharedAudioFrame = eventObj.GetRetainableEventDataBuffer();
                            this.frameTime = eventObj.RelativeTime;

                            unsafe
                            {
                                nui.AUDIO_FRAME* pFrame = (nui.AUDIO_FRAME*)bufferPtr.ToPointer();
                                nui.AUDIO_SUBFRAME* pSubFrame = &(pFrame->FirstSubFrame);
                                nui.AUDIO_BEAM_FRAME_HEADER* pBeamHeader = &(pSubFrame->BeamFrameHeader);

                                this.beamConfidence = pBeamHeader->BeamAngleConfidence;
                                this.beamAngle = pBeamHeader->BeamAngle;

                                for (int i = 0; i < pFrame->SubFrameCount; ++i)
                                {
                                    UpdateChartSubFrame(pSubFrame + i);
                                }
                            }
                        }
                    }
                }
            }
        }

        public IPluginViewSettings Add2DPropertyView(ContentControl hostControl)
        {
            return null;
        }

        public void UpdatePropertyView(EventType eventType, double x, double y, uint width, uint height)
        {
        }

        public void ClearPropertyView()
        {
        }

        public void RemoveView(Guid viewId, Panel hostControl, IPluginViewSettings pluginViewSettings)
        {
        }

        public IPluginViewSettings Add2DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = null;

            if (eventType == EventType.Monitor)
            {
                pluginViewSettings = new AudioPlugin2DViewSettings(this);
            }

            return pluginViewSettings;
        }

        public IPluginViewSettings Add3DView(EventType eventType, Panel hostControl)
        {
            IPluginViewSettings pluginViewSettings = null;

            if (eventType == EventType.Monitor)
            {
                pluginViewSettings = new AudioPlugin3DViewSettings(this);
            }

            return pluginViewSettings;
        }

        public viz.Texture GetTexture(EventType eventType, IPluginViewSettings pluginViewSettings)
        {
            return null;
        }

        public void Render2D(EventType eventType, IPluginViewSettings pluginViewSettings, viz.Context context, viz.Texture texture, float left, float top, float width, float height)
        {
            if (eventType == EventType.Monitor) 
            {
                lock (this.lockObj)
                {
                    if (this.frameTime == TimeSpan.MinValue)
                    {
                        return;
                    }

                    AudioPlugin2DViewSettings audioPluginViewSettings = pluginViewSettings as AudioPlugin2DViewSettings;
                    if (audioPluginViewSettings != null)
                    {
                        float x = 10; // margin on left
                        float chartX = x + 100; // offset to chart rendering
                        float y = 10; // margin on top
                        float deltaY = 24; // height of each row

                        // chart window size
                        float chartHeight = 20;
                        float chartWidth = 600;

                        // max value in chart, bigger ones got capped
                        float chartValueCap = 0.02f;

                        // special color for current frame in time line
                        viz.Vector currentFrameColor = new viz::Vector(1, 0, 0, 1);

                        viz.Vector? color = null;

                        if ((audioPluginViewSettings.RenderBeam) && (this.font != null))
                        {
                            string str;
                            if (this.beamConfidence > 0.0f)
                            {
                                str = string.Format(CultureInfo.CurrentCulture, Strings.Audio_Beam_Label_Format, this.beamAngle); // TODO: option for other display? like degrees?
                            }
                            else
                            {
                                str = Strings.Audio_Beam_Invalid_Label;
                            }
                            this.font.DrawText(str, x, y, color);
                        }
                        y += deltaY;

                        if (this.sharedAudioFrame != null)
                        {
                            if (this.sharedAudioFrame.Size >= cAudioFrameSizeMinimum)
                            {
                                IntPtr bufferPtr = this.sharedAudioFrame.Buffer;

                                unsafe
                                {
                                    nui.AUDIO_FRAME* pFrame = (nui.AUDIO_FRAME*)bufferPtr.ToPointer();
                                    nui.AUDIO_SUBFRAME* pSubFrame = &(pFrame->FirstSubFrame);

                                    ulong currentFirstFrameTimeStamp = (ulong)this.frameTime.Ticks;
                                    ulong timeStampOffset = currentFirstFrameTimeStamp - pSubFrame->TimeCounter;
                                    ulong currentLastFrameTimeStamp = pSubFrame[pFrame->SubFrameCount - 1].TimeCounter + timeStampOffset;

#if TODO_LOCAL_PLAYBACK
                                    if ((this.timelineBegin != 0) || (this.timelineEnd != 0))
                                    {
                                        // timeline rendering

                                        if (this.timelineDirty)
                                        {
                                            this.timelineData->Sort(new System.Comparison<TimelineEntry>(TimelineEntry.Compare));
                                            timelineDirty = false;
                                        }

                                        if (audioPluginViewSettings.RenderOutput)
                                        {
                                            if (this.font != null)
                                            {
                                                this.font.DrawText(this.outString, x, y, color);

                                                for (int i = 0; i < this.timelineData.Count; i++)
                                                {
                                                    ulong timeStamp = this.timelineData[i]->TimeStamp;
                                                    if (timeStamp < this.timelineBegin) continue;
                                                    if (timeStamp > this.timelineEnd) break;

                                                    float normalizedValue = (float)(Math.Min(chartValueCap, Math.Abs(this.timelineData[i].Output)) / chartValueCap);
                                                    float topLeftX = chartX + (timeStamp - this.timelineBegin) * chartWidth / (this.timelineEnd - this.timelineBegin);
                                                    float topLeftY = y + (1 - normalizedValue) * chartHeight / 2;
                                                    float chartBarHeight = Math.Max(normalizedValue * chartHeight, 1.0f);
                                                    bool isCurrentFrame = (timeStamp >= currentFirstFrameTimeStamp && timeStamp <= currentLastFrameTimeStamp);
                                                    float barZ = isCurrentFrame ? 0 : 0.01f; // put current frame at a smaller z so it's always visible
                                                    float barWidth = isCurrentFrame ? 2.0f : 1.0f; // make current frame tick bold
                                                    viz::Vector? barColor = isCurrentFrame ? currentFrameColor : null;

                                                    if (this.overlay != null)
                                                    {
                                                        this.overlay->DrawColor(topLeftX, topLeftY, barWidth, chartBarHeight, barZ, barColor);
                                                    }
                                                }
                                            }
                                            y += deltaY;
                                        }

                                        for (int iMIC = 0; iMIC < NUIP_AUDIO_NUM_MIC; iMIC++)
                                        {
                                            if ((int)renderOptionType == (int)RenderOptionType::MIC0 + iMIC)
                                            {
                                                _font->DrawText(renderOptionName, x, y, color);

                                                for (int iSample = 0; iSample < _timelineData->Count; iSample++)
                                                {
                                                    UInt64 timeStamp = _timelineData[iSample]->TimeStamp;
                                                    if (timeStamp < _timelineBegin) continue;
                                                    if (timeStamp > _timelineEnd) break;

                                                    float normalizedValue = min(chartValueCap, fabs(_timelineData[iSample]->MIC[iMIC])) / chartValueCap;
                                                    float topLeftX = chartX + (timeStamp - _timelineBegin) * chartWidth / (_timelineEnd - _timelineBegin);
                                                    float topLeftY = y + (1 - normalizedValue) * chartHeight / 2;
                                                    float chartBarHeight = max(normalizedValue * chartHeight, 1.0f);
                                                    bool isCurrentFrame = (timeStamp >= currentFirstFrameTimeStamp && timeStamp <= currentLastFrameTimeStamp);
                                                    float barZ = isCurrentFrame ? 0 : 0.01f;
                                                    float barWidth = isCurrentFrame ? 2.0f : 1.0f;
                                                    Nullable<Xbox::Kinect::Viz::Vector> barColor;
                                                    if (isCurrentFrame) barColor = currentFrameColor;

                                                    _overlay->DrawColor(topLeftX, topLeftY, barWidth, chartBarHeight, barZ, barColor);
                                                }
                                            }
                                            y += deltaY;
                                        }

                                        for (int iSPK = 0; iSPK < NUIP_AUDIO_NUM_SPK; iSPK++)
                                        {
                                            if ((int)renderOptionType == (int)RenderOptionType::SPK0 + iSPK)
                                            {
                                                _font->DrawText(renderOptionName, x, y, color);

                                                for (int iSample = 0; iSample < _timelineData->Count; iSample++)
                                                {
                                                    UInt64 timeStamp = _timelineData[iSample]->TimeStamp;
                                                    if (timeStamp < _timelineBegin) continue;
                                                    if (timeStamp > _timelineEnd) break;

                                                    float normalizedValue = min(chartValueCap, fabs(_timelineData[iSample]->SPK[iSPK])) / chartValueCap;
                                                    float topLeftX = chartX + (timeStamp - _timelineBegin) * chartWidth / (_timelineEnd - _timelineBegin);
                                                    float topLeftY = y + (1 - normalizedValue) * chartHeight / 2;
                                                    float chartBarHeight = max(normalizedValue * chartHeight, 1.0f);
                                                    bool isCurrentFrame = (timeStamp >= currentFirstFrameTimeStamp && timeStamp <= currentLastFrameTimeStamp);
                                                    float barZ = isCurrentFrame ? 0 : 0.01f;
                                                    float barWidth = isCurrentFrame ? 2.0f : 1.0f;
                                                    Nullable<Xbox::Kinect::Viz::Vector> barColor;
                                                    if (isCurrentFrame) barColor = currentFrameColor;

                                                    _overlay->DrawColor(topLeftX, topLeftY, barWidth, chartBarHeight, barZ, barColor);
                                                }
                                            }
                                            y += deltaY;
                                        }
                                    }
                                    else
#endif // TODO_LOCAL_PLAYBACK
                                    {
                                        // live rendering
                                        float barWidth = 1;
                                        UInt64 timeSpan = 2 * 10 * 1000 * 1000; // in 100ns unit

                                        if (audioPluginViewSettings.RenderOutput)
                                        {
                                            if (this.font != null)
                                            {
                                                this.font.DrawText(this.outString, x, y, color);
                                            }

                                            if (this.outChart != null)
                                            {
                                                this.outChart.RenderBar(chartX, y, chartWidth, chartHeight, 0, barWidth, color, currentLastFrameTimeStamp, timeSpan, 0, chartValueCap);
                                            }

                                            y += deltaY;
                                        }

                                        if (this.micCharts != null)
                                        {
                                            for (int i = 0; i < this.micCharts.Length; ++i)
                                            {
                                                if (audioPluginViewSettings.GetTrackOption((AudioTrack)(AudioTrack.Mic0 + i)))
                                                {
                                                    if (this.font != null)
                                                    {
                                                        this.font.DrawText(this.micStrings[i], x, y, color);
                                                    }

                                                    this.micCharts[i].RenderBar(chartX, y, chartWidth, chartHeight, 0, barWidth, color, currentLastFrameTimeStamp, timeSpan, 0, chartValueCap);

                                                    y += deltaY;
                                                }
                                            }
                                        }

                                        if (this.speakerCharts != null)
                                        {
                                            for (int i = 0; i < this.speakerCharts.Length; ++i)
                                            {
                                                if (audioPluginViewSettings.GetTrackOption((AudioTrack)(AudioTrack.SpeakerL + i)))
                                                {
                                                    if (this.font != null)
                                                    {
                                                        this.font.DrawText(this.speakerStrings[i], x, y, color);
                                                    }

                                                    this.speakerCharts[i].RenderBar(chartX, y, chartWidth, chartHeight, 0, barWidth, color, currentLastFrameTimeStamp, timeSpan, 0, chartValueCap);

                                                    y += deltaY;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Render3D(EventType eventType, IPluginViewSettings pluginViewSettings, viz.Context context, viz.Texture texture)
        {
            if (eventType == EventType.Monitor) 
            {
                lock (this.lockObj)
                {
                    if (pluginViewSettings is AudioPlugin3DViewSettings)
                    {
                        if (this.beamConfidence > 0.0f)
                        {
                            viz.Effect effectBeam = new viz.Effect()
                                {
                                    EnableLighting = true,
                                    Ambient = new viz.Vector(0.0f, 1.0f, 0.0f, 0.5f),
                                };

                            float[] matrixFloats = new float[16];
                            unsafe
                            {
                                fixed (float* pMatrix = &matrixFloats[0])
                                {
                                    MatrixHelper.CalculateBeamMatrix(this.beamAngle, pMatrix);
                                }
                            }

                            viz.Matrix mat = new viz.Matrix();
                            mat.R0 = new viz.Vector(matrixFloats[0], matrixFloats[1], matrixFloats[2], matrixFloats[3]);
                            mat.R1 = new viz.Vector(matrixFloats[4], matrixFloats[5], matrixFloats[6], matrixFloats[7]);
                            mat.R2 = new viz.Vector(matrixFloats[8], matrixFloats[9], matrixFloats[10], matrixFloats[11]);
                            mat.R3 = new viz.Vector(matrixFloats[12], matrixFloats[13], matrixFloats[14], matrixFloats[15]);

                            this.beamMesh.Render(viz.MeshRenderMode.IndexedTriangleList, mat, effectBeam, null);
                        }
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this.lockObj)
                {
                    this.sharedAudioFrame = null;

                    if (this.audioPlayer != null)
                    {
                        this.audioPlayer.Dispose();
                        this.audioPlayer = null;
                    }

                    if (this.font != null)
                    {
                        this.font.Dispose();
                        this.font = null;
                    }

                    if (this.overlay != null)
                    {
                        this.overlay.Dispose();
                        this.overlay = null;
                    }

                    if (this.beamMesh != null)
                    {
                        this.beamMesh.Dispose();
                        this.beamMesh = null;
                    }

                    if (this.outChart != null)
                    {
                        this.outChart.Dispose();
                        this.outChart = null;
                    }

                    if (this.micCharts != null)
                    {
                        foreach (viz.TemporalChart chart in this.micCharts)
                        {
                            chart.Dispose();
                        }
                        this.micCharts = null;
                    }

                    if (this.speakerCharts != null)
                    {
                        foreach (viz.TemporalChart chart in this.speakerCharts)
                        {
                            chart.Dispose();
                        }
                        this.speakerCharts = null;
                    }
                }
            }
        }

        unsafe private void UpdateChartSubFrame(nui.AUDIO_SUBFRAME* pSubFrame)
        {
            if (this.outChart != null)
            {
                IntPtr ptrOut = new IntPtr(pSubFrame) + cOutOffset;
                float* pFloats = (float*)ptrOut;
                float average = 0.0f;

                for (uint i = 0; i < nui.Constants.AUDIO_SAMPLES_PER_SUBFRAME; ++i)
                {
                    average += Math.Abs(pFloats[i]);
                }

                average = average / nui.Constants.AUDIO_SAMPLES_PER_SUBFRAME;
                this.outChart.Update(average, pSubFrame->TimeCounter);
            }

            if (this.micCharts != null)
            {
                float[] averages = new float[this.micCharts.Length];
                IntPtr ptrMic = new IntPtr(pSubFrame) + cMicOffset;
                float* pFloats = (float*)ptrMic;

#if OLD // less performant
                fixed (float* pAverages = &averages[0])
                {
                    AverageHelper.CalculateAbsAverageBy4((uint)nui.Constants.AUDIO_SAMPLES_PER_SUBFRAME * 4, pFloats, pAverages);
                }
#endif // OLD
                uint total = nui.Constants.AUDIO_SAMPLES_PER_SUBFRAME * 4;

                for (uint i = 0; i < total; i += 4)
                {
                    averages[0] += Math.Abs(pFloats[i]);
                    averages[1] += Math.Abs(pFloats[i + 1]);
                    averages[2] += Math.Abs(pFloats[i + 2]);
                    averages[3] += Math.Abs(pFloats[i + 3]);
                }

                for (int i = 0; i < 4; ++i)
                {
                    averages[i] /= nui.Constants.AUDIO_SAMPLES_PER_SUBFRAME;
                    this.micCharts[i].Update(averages[i], pSubFrame->TimeCounter);
                }
            }

            if (this.speakerCharts != null)
            {
                float[] averages = new float[this.speakerCharts.Length];
                IntPtr ptrSpeaker = new IntPtr(pSubFrame) + cSpeakerOffset;
                float* pFloats = (float*)ptrSpeaker;
#if OLD 
                fixed (float* pAverages = &averages[0])
                {
                    AverageHelper.CalculateAbsAverageBy8((uint)nui.Constants.AUDIO_SAMPLES_PER_SUBFRAME * 8, pFloats, pAverages);
                }
#endif // OLD
                uint total = nui.Constants.AUDIO_SAMPLES_PER_SUBFRAME * 8;
                for (uint i = 0; i < total; i += 8)
                {
                    averages[0] += Math.Abs(pFloats[i]);
                    averages[1] += Math.Abs(pFloats[i + 1]);
                    averages[2] += Math.Abs(pFloats[i + 2]);
                    averages[3] += Math.Abs(pFloats[i + 3]);
                    averages[4] += Math.Abs(pFloats[i + 4]);
                    averages[5] += Math.Abs(pFloats[i + 5]);
                    averages[6] += Math.Abs(pFloats[i + 6]);
                    averages[7] += Math.Abs(pFloats[i + 7]);
                }

                for (int i = 0; i < this.speakerCharts.Length; ++i)
                {
                    averages[i] /= nui.Constants.AUDIO_SAMPLES_PER_SUBFRAME;
                    this.speakerCharts[i].Update(averages[i], pSubFrame->TimeCounter);
                }
            }

            if (this.IsAudible && (this.audioPlayer != null))
            {
                int offset = 0;
                uint stride = 0;

                switch (this.AudibleTrack)
                {
                    case AudioTrack.Output:
                        stride = 1;
                        offset = AudioPlugin.cOutOffset;
                        break;

                    case AudioTrack.Mic0:
                    case AudioTrack.Mic1:
                    case AudioTrack.Mic2:
                    case AudioTrack.Mic3:
                        stride = (uint)this.micCharts.Length;
                        offset = AudioPlugin.cMicOffset + (sizeof(float) * (int)(this.AudibleTrack - AudioTrack.Mic0));
                        break;

                    case AudioTrack.SpeakerL:
                    case AudioTrack.SpeakerR:
                    case AudioTrack.SpeakerC:
                    case AudioTrack.SpeakerLFE:
                    case AudioTrack.SpeakerBL:
                    case AudioTrack.SpeakerBR:
                    case AudioTrack.SpeakerSL:
                    case AudioTrack.SpeakerSR:
                        stride = (uint)this.speakerCharts.Length;
                        offset = AudioPlugin.cSpeakerOffset + (sizeof(float) * (int)(this.AudibleTrack - AudioTrack.SpeakerL));
                        break;
                }

                if (stride != 0)
                {
                    IntPtr ptr = new IntPtr(pSubFrame) + offset;
                    float* pFloats = (float*)ptr;

                    this.audioPlayer.PlayAudio(nui.Constants.AUDIO_SAMPLES_PER_SUBFRAME, pFloats, nui.Constants.AUDIO_SAMPLERATE, stride);
                }
            }
        }

#if TODO_LOCAL_PLAYBACK // local playback
        private struct TimelineEntry
        {
            public TimelineEntry()
            {
                this.Mics = new float[nui.Constants.AUDIO_NUM_MIC];
                this.Speakers = new float[nui.Constants.AUDIO_NUM_SPK];
            }
            private ulong TimeStamp;
            private float Output;
            private float[] Mics;
            private float[] Speakers;

            public static int Compare(TimelineEntry a, TimelineEntry b)
            {
                return a.TimeStamp.CompareTo(b.TimeStamp);
            }
        }
#endif // TODO_LOCAL_PLAYBACK

        private static readonly int cAudioFrameSizeMinimum = Marshal.SizeOf(typeof(nui.AUDIO_FRAME));
        private static readonly int cOutOffset = Marshal.OffsetOf(typeof(nui.AUDIO_SUBFRAME), "OutBuffer").ToInt32();
        private static readonly int cMicOffset = Marshal.OffsetOf(typeof(nui.AUDIO_SUBFRAME), "MicBuffer").ToInt32();
        private static readonly int cSpeakerOffset = Marshal.OffsetOf(typeof(nui.AUDIO_SUBFRAME), "SpkBuffer").ToInt32();
        private object lockObj = new object();
        private string outString;
        private string[] micStrings = null;
        private string[] speakerStrings = null;
        private viz.Font font = null;
        private viz.Overlay overlay = null;
        private viz.TemporalChart outChart = null;
        private viz.TemporalChart[] micCharts = null;
        private viz.TemporalChart[] speakerCharts = null;
        private viz.Mesh beamMesh = null;
        private TimeSpan frameTime = TimeSpan.MinValue;
        private float beamConfidence = 0.0f;
        private float beamAngle = 0.0f;
        private HGlobalBuffer sharedAudioFrame = null;
        private bool audible = true;
        private AudioTrack audibleTrack = AudioTrack.Output;
        private AudioPlayer audioPlayer = null;
        private ILoggingService loggingService = null;
#if TODO_LOCAL_PLAYBACK
        List<TimelineEntry> timelineData = new List<TimelineEntry>();
        ulong timelineBegin = 0;
        ulong timelineEnd = 0;
        bool timelineDirty = false;
#endif // TODO_LOCAL_PLAYBACK
    }
}
