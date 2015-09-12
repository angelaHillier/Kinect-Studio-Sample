//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioApp
{
    using System.Collections;

    public class MetadataInfo
    {
        public MetadataInfo(bool isReadOnly, string shortName, string longName, IEnumerable publicMetadata, IEnumerable personalMetadata)
        {
            this.IsReadOnly = isReadOnly;
            this.ShortName = shortName;
            this.LongName = longName;
            this.PublicMetadata = publicMetadata;
            this.PersonalMetadata = personalMetadata;
        }

        public bool IsReadOnly { get; private set; }
        public string ShortName { get; private set; }
        public string LongName { get; private set; }
        public IEnumerable PublicMetadata { get; private set; }
        public IEnumerable PersonalMetadata { get; private set; }
    }
}
