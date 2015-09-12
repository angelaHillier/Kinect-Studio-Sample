//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

namespace KinectStudioPlugin
{
    using System;
    using System.Xml.Linq;
    using Microsoft.Kinect.Tools;

    public interface IPlugin 
    {
        string Name { get; }

        Guid Id { get; }

        void ReadFrom(XElement element);

        void WriteTo(XElement element);
    }

    public abstract class BasePlugin : KStudioUserState, IPlugin
    {
        public string Name 
        { 
            get 
            { 
                return name; 
            } 
        }

        public Guid Id 
        { 
            get 
            { 
                return id; 
            } 
        }

        public virtual void ReadFrom(XElement element) { }

        public virtual void WriteTo(XElement element) { }

        protected BasePlugin(string name, Guid id)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            this.name = name;
            this.id = id;
        }

        private readonly string name;
        private readonly Guid id;
    }
}
