//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;

namespace Microsoft.Xbox.Tools.Shared
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class BackgroundRequestProcessor
    {
        Thread processorThread;
        Queue<IBackgroundRequest> requests = new Queue<IBackgroundRequest>();
        ObservableCollection<BackgroundRequest> observableRequestList;
        object lockObject = new object();
        ManualResetEventSlim workReadyEvent = new ManualResetEventSlim();
        ManualResetEventSlim initHandshakeEvent = new ManualResetEventSlim();
        ManualResetEventSlim exitHandshakeEvent = new ManualResetEventSlim();
        HResult initResult;
        bool shutdownRequested;

        public IEnumerable<BackgroundRequest> RequestQueue
        {
            get
            {
                if (observableRequestList == null)
                {
                    // Unfortunate that we need two lists, but the behavior we want to observe is different from
                    // that which we'd get if we just observed the queue (the items are removed from the queue before
                    // they're done, so you can't see when they're in progress, and what's currently being executed).
                    observableRequestList = new ObservableCollection<BackgroundRequest>();
                    foreach (var r in requests)
                        observableRequestList.Add((BackgroundRequest)r);
                }
                return observableRequestList;
            }
        }

        public Dispatcher Dispatcher { get; private set; }
        public virtual DispatcherPriority DispatchPriority { get { return DispatcherPriority.Normal; } }

        protected virtual ApartmentState ApartmentState { get { return ApartmentState.STA; } }
        protected virtual int StartupTimeout { get { return 5000; } }
        protected virtual int ShutdownTimeout { get { return 5000; } }
        protected bool ShutdownRequested { get { return shutdownRequested; } }

        protected virtual HResult PreThreadStartInitialize()
        {
            return HResult.S_OK;
        }

        protected virtual HResult PostThreadStartInitialize()
        {
            return HResult.S_OK;
        }

        public HResult Initialize()
        {
            this.Dispatcher = Dispatcher.FromThread(Thread.CurrentThread);

            if (this.Dispatcher == null)
            {
                return HResult.FromException(new InvalidOperationException());
            }

            HResult hr = this.PreThreadStartInitialize();

            if (hr.Failed)
            {
                return hr;
            }

            this.processorThread = new Thread(this.ProcessRequests);
            this.processorThread.SetApartmentState(this.ApartmentState);
            this.processorThread.IsBackground = true;
            this.processorThread.Name = "BackgroundRequestWorkerThread";
            this.processorThread.Start();

            if (!this.initHandshakeEvent.Wait(this.StartupTimeout))
            {
                return HResult.FromException(new TimeoutException());
            }

            if (this.initResult.Failed)
            {
                return this.initResult;
            }

            return this.PostThreadStartInitialize();
        }

        protected void SignalWorkReady()
        {
            this.workReadyEvent.Set();
        }

        public void Enqueue(BackgroundRequest request)
        {
            HResult hr = ((IBackgroundRequest)request).OnEnqueued(this);

            // If OnEnqueued returns failure, it means that the request will already have
            // dispatched its return value.  No need to put it in the queue.
            if (hr.Succeeded)
            {
                if (observableRequestList != null)
                {
                    if (!this.Dispatcher.CheckAccess())
                    {
                        this.Dispatcher.BeginInvoke((Action)(() => observableRequestList.Add(request)));
                    }
                    else
                    {
                        observableRequestList.Add(request);
                    }
                }

                lock (lockObject)
                {
                    this.requests.Enqueue(request);
                    this.workReadyEvent.Set();
                }
            }
        }

        protected virtual void ForceShutdown()
        {
        }

        public bool Shutdown()
        {
            this.shutdownRequested = true;
            this.workReadyEvent.Set();
            if (!this.exitHandshakeEvent.Wait(this.ShutdownTimeout))
            {
                this.ForceShutdown();
                return this.exitHandshakeEvent.Wait(this.ShutdownTimeout);
            }

            return true;
        }

        internal void OnRequestDispatched(BackgroundRequest request)
        {
            if (observableRequestList != null)
                observableRequestList.Remove(request);
        }

        /// <summary>
        /// This method is called from the background processor thread before it
        /// enters the main processing loop.
        /// </summary>
        /// <returns>Result of initialization -- a failed HResult will circumvent processing.</returns>
        protected virtual HResult InitializeBackgroundThread()
        {
            return HResult.S_OK;
        }

        /// <summary>
        /// This method is called from the background processor thread when it has
        /// exited its main processing loop (i.e., Shutdown() was called, or
        /// ShouldContinueProcessing returned false).
        /// </summary>
        protected virtual void CleanupBackgroundThread()
        {
        }

        /// <summary>
        /// This property is called from the background processor thread, while a
        /// lock object is held so it should only do a quick local state check.
        /// </summary>
        protected virtual bool ShouldContinueProcessing { get { return true; } }

        /// <summary>
        /// This method is called in from the background processor thread each time
        /// it is alerted that work items are ready for processing, just before it
        /// processes them.
        /// </summary>
        protected virtual void DoCustomProcessing()
        {
        }

        void ProcessRequests()
        {
            this.initResult = this.InitializeBackgroundThread();
            this.initHandshakeEvent.Set();

            if (this.initResult.Failed)
                return;

            IBackgroundRequest[] requestList;

            while (true)
            {
                workReadyEvent.Wait();

                lock (lockObject)
                {
                    if (shutdownRequested)
                        break;

                    if (!this.ShouldContinueProcessing)
                        break;

                    if (this.requests.Count > 0)
                    {
                        requestList = this.requests.ToArray();
                        this.requests.Clear();
                    }
                    else
                    {
                        requestList = null;
                    }

                    workReadyEvent.Reset();
                }

                this.DoCustomProcessing();

                if (requestList != null)
                {
                    foreach (var request in requestList)
                    {
                        // Note that these requests may have already been canceled... this is a no-op in that case.
                        request.Execute();

                        // Allow COM callbacks to arrive between each request if present
                        this.processorThread.Join(0);
                    }

                    requestList = null;
                }
            }

            this.CleanupBackgroundThread();
            this.exitHandshakeEvent.Set();
        }
    }
}
