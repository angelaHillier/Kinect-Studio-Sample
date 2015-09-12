//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;

    public class StreamMetadataDataTemplateKey : FileMetadataDataTemplateKey
    {
        public StreamMetadataDataTemplateKey(Type valueType)
            : base(valueType)
        {
        }

        public StreamMetadataDataTemplateKey(Type valueType, string keyName)
            : base(valueType, keyName)
        {
        }

        public StreamMetadataDataTemplateKey(Type valueType, string keyName, Guid dataTypeId)
            : base(valueType, keyName)
        {
            if (dataTypeId == Guid.Empty)
            {
                throw new ArgumentNullException("dataTypeId");
            }

            this.dataTypeId = dataTypeId;
        }

        public StreamMetadataDataTemplateKey(Type valueType, string keyName, Guid dataTypeId, Guid semanticId)
            : this(valueType, keyName, dataTypeId)
        {
            if (semanticId == Guid.Empty)
            {
                throw new ArgumentNullException("semanticId");
            }

            this.semanticId = semanticId;
        }

        public override bool Equals(object obj)
        {
            bool result = base.Equals(obj);

            if (result)
            {
                StreamMetadataDataTemplateKey other = obj as StreamMetadataDataTemplateKey;
                if (other == null)
                {
                    result = false;
                }
                else
                {
                    result = (this.dataTypeId == other.dataTypeId) &&
                             (this.semanticId == other.semanticId);
                }
            }

            return result;
        }

        public override int GetHashCode()
        {
            int result = base.GetHashCode();
            result ^= dataTypeId.GetHashCode();
            result ^= semanticId.GetHashCode();
            return result;
        }

        public override string ToString()
        {
            string value = "StreamMetadataDataTemplateKey {" + this.ToStringFragment() + "}";

            return value;
        }

        protected override string ToStringFragment()
        {
            string value = base.ToStringFragment();

            if (this.dataTypeId != Guid.Empty)
            {
                value += ":" + this.dataTypeId.ToString();

                if (this.semanticId != Guid.Empty)
                {
                    value += ":" + this.semanticId.ToString();
                }
            }

            return value;
        }

        private readonly Guid dataTypeId;
        private readonly Guid semanticId;
    }
}
