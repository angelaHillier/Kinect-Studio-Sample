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
    using System.IO;
    using System.Text;
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;
    using KinectStudioUtility;

    public class FileSettingsService : IFileSettingsService
    {
        public void LoadSettings(KStudioClipSource clipSource)
        {
            this.LoadSettingsInternal(null, clipSource);
        }

        public void UnloadSettings(KStudioClipSource clipSource)
        {
            this.UnloadSettingsInternal(null, clipSource);
        }

        public XElement GetSettings(KStudioClipSource clipSource, string settingsKey)
        {
            this.LoadSettingsInternal(null, clipSource);

            return this.GetSettingsInternal(null, clipSource, settingsKey);
        }

        public void LoadSettings(string targetAlias, KStudioClipSource clipSource)
        {
            if (String.IsNullOrEmpty(targetAlias))
            {
                throw new ArgumentNullException("targetAlias");
            }

            LoadSettingsInternal(targetAlias, clipSource);
        }

        public void UnloadSettings(string targetAlias, KStudioClipSource clipSource)
        {
            if (String.IsNullOrEmpty(targetAlias))
            {
                throw new ArgumentNullException("targetAlias");
            }

            UnloadSettingsInternal(targetAlias, clipSource);
        }

        public XElement GetSettings(string targetAlias, KStudioClipSource clipSource, string settingsKey)
        {
            if (String.IsNullOrEmpty(targetAlias))
            {
                throw new ArgumentNullException("targetAlias");
            }

            this.LoadSettingsInternal(targetAlias, clipSource);

            return GetSettingsInternal(targetAlias, clipSource, settingsKey);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void LoadSettingsInternal(string targetAlias, KStudioClipSource clipSource)
        {
            if (clipSource == null)
            {
                throw new ArgumentNullException("clipSource");
            }

            Guid identifier = GetFileId(targetAlias, clipSource);

            lock (this.fileSettings)
            {
                XDocument xml;

                if (!this.fileSettings.TryGetValue(identifier, out xml))
                {
                    string fileName = GetFileName(targetAlias, identifier);
                    if (File.Exists(fileName))
                    {
                        try
                        {
                            xml = XDocument.Load(fileName);
                        }
                        catch (Exception)
                        {
                            // TODO_LOG
                        }
                    }

                    if (xml == null)
                    {
                        xml = new XDocument();
                    }

                    if (xml.Root == null)
                    {
                        XElement fileElement = new XElement("file");
                        if (targetAlias != null)
                        {
                            fileElement.Add(new XAttribute("targetAlias", targetAlias));
                        }
                        fileElement.Add(new XAttribute("id", identifier.ToString()));
                        xml.Add(fileElement);
                    }

                    this.fileSettings[identifier] = xml;
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void UnloadSettingsInternal(string targetAlias, KStudioClipSource clipSource)
        {
            if (clipSource == null)
            {
                throw new ArgumentNullException("clipSource");
            }

            Guid identifier = GetFileId(targetAlias, clipSource);

            lock (this.fileSettings)
            {
                XDocument xml;
                if (this.fileSettings.TryGetValue(identifier, out xml))
                {
                    string fileName = GetFileName(targetAlias, identifier);
                    try
                    {
                        xml.Save(fileName);
                    }
                    catch (Exception)
                    {
                        // TODO_LOG
                    }

                    this.fileSettings.Remove(identifier);
                }
            }
        }

        private XElement GetSettingsInternal(string targetAlias, KStudioClipSource clipSource, string settingsKey)
        {
            if (clipSource == null)
            {
                throw new ArgumentNullException("clipSource");
            }

            if (String.IsNullOrEmpty(settingsKey))
            {
                throw new ArgumentNullException("settingsKey");
            }

            Guid identifier = GetFileId(targetAlias, clipSource);

            XElement value = null;

            lock (this.fileSettings)
            {
                XDocument xml;
                if (!this.fileSettings.TryGetValue(identifier, out xml))
                {
                    throw new InvalidOperationException("settings not loaded");
                }

                Debug.Assert(xml.Root != null);

                value = xml.Root.Element(settingsKey);
                if (value == null)
                {
                    value = new XElement(settingsKey);
                    xml.Root.Add(value);
                }
            }

            return value;
        }

        private static Guid GetFileId(string targetAlias, KStudioClipSource clipSource)
        {
            Debug.Assert(clipSource != null);

            Guid value = clipSource.Id;

            if (value == Guid.Empty)
            {
                KStudioEventFile file = clipSource as KStudioEventFile;
                if (file == null)
                {
                    throw new ArgumentOutOfRangeException("clipSource",  "not a KStudioEventFile");
                }
                else
                {
                    // File did not have a real identifier (legacy file), so create one that is "reasonable"

                    string fileNameUpper = file.FilePath.ToUpperInvariant();
                    int fullPathHash = fileNameUpper.GetHashCode();
                    fileNameUpper = Path.GetFileName(fileNameUpper.Reverse());
                    int fileNameHash = fileNameUpper.GetHashCode();

                    if (targetAlias == null)
                    {
                        value = new Guid((UInt32)fullPathHash,
                            (UInt16)fileNameHash,
                            (UInt16)(((UInt32)fileNameHash) >> 16),
                            0, 0, 0, 0, 0, 0, 0, 0);
                    }
                    else
                    {
                        string targetAliasUpper = targetAlias.ToUpperInvariant();
                        UInt32 targetAliasHash1 = (UInt32)targetAliasUpper.GetHashCode();
                        targetAliasUpper= targetAliasUpper.Reverse();
                        UInt32 targetAliasHash2 = (UInt32)targetAliasUpper.GetHashCode();

                        value = new Guid((UInt32)fullPathHash,
                            (UInt16)fileNameHash,
                            (UInt16)(((UInt32)fileNameHash) >> 16),
                            (byte)targetAliasHash1,
                            (byte)(targetAliasHash1 >> 8),
                            (byte)(targetAliasHash1 >> 16),
                            (byte)(targetAliasHash1 >> 24),
                            (byte)targetAliasHash2,
                            (byte)(targetAliasHash2 >> 8),
                            (byte)(targetAliasHash2 >> 16),
                            (byte)(targetAliasHash2 >> 24));
                    }
                }
            }

            return value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.IO.FileInfo"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "fileInfo")]
        private static string GetFileName(string targetAlias, Guid identifier)
        {
            Debug.Assert(identifier != Guid.Empty);

            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", settingsFolder);
            
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            string value = null;

            if (targetAlias == null)
            {
                value = Path.Combine(filePath, String.Format(CultureInfo.InvariantCulture, settingsLocalFileFormat, identifier.ToString()));
            }
            else
            {
                value = Path.Combine(filePath, String.Format(CultureInfo.InvariantCulture, settingsTargetFileFormat, identifier.ToString(), targetAlias));

                try
                {
                    FileInfo fileInfo = new FileInfo(value);
                }
                catch (Exception)
                {
                    byte[] bytes = UnicodeEncoding.Unicode.GetBytes(targetAlias);
                    string targetAliasHash = Convert.ToBase64String(bytes);
                    while (targetAliasHash.EndsWith("=", StringComparison.Ordinal))
                    {
                        targetAliasHash = targetAliasHash.Substring(0, targetAliasHash.Length - 1);
                    }

                    value = Path.Combine(filePath, String.Format(CultureInfo.InvariantCulture, settingsTargetFileFormat, identifier.ToString(), targetAliasHash));
                }
            }

            return value;
        }

        private const string settingsFolder = "KinectStudio\\Files";
        private const string settingsLocalFileFormat = "kstudio-{0}.xml";
        private const string settingsTargetFileFormat = "kstudio-{1}-{0}.xml";

        private Dictionary<Guid, XDocument> fileSettings = new Dictionary<Guid, XDocument>();
    }
}
