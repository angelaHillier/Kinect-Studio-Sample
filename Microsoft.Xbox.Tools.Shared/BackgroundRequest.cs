//// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
//// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
//// PARTICULAR PURPOSE. 
//// 
//// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Threading;
using System.Windows.Threading;

namespace Microsoft.Xbox.Tools.Shared
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public abstract class BackgroundRequest : IBackgroundRequest
    {
        object lockObject = new object();
        bool responseDispatched;
        bool inProgress;
        HResult dispatchedResult = HResult.S_OK;
        ManualResetEventSlim workDone;

        protected object LockObject { get { return lockObject; } }
        protected bool ResponseDispatched { get { return responseDispatched; } }

        public BackgroundRequestProcessor Processor { get; private set; }
        public bool IsDispatchComplete { get; private set; }
        public HResult Result { get { return dispatchedResult; } }
        public virtual string Name { get { return this.GetType().Name; } }

        public event EventHandler Dispatched;

        // This is called on the thread that called EngineRequestProcess.Enqueue.  
        HResult IBackgroundRequest.OnEnqueued(BackgroundRequestProcessor processor)
        {
            this.Processor = processor;
            this.responseDispatched = false;
            this.inProgress = false;

            HResult hr = this.OnEnqueued();

            if (hr.Failed)
            {
                this.DispatchResponse(hr);
            }

            return hr;
        }

        // Override this to perform any validation of parameters that you need done on the UI thread
        // instead of the processor thread.  If this fails, the response will be dispatched without
        // this request ever making it into the queue.  It *will* be dispatched, though... so it will
        // still appear to be fully async.
        protected virtual HResult OnEnqueued()
        {
            return HResult.S_OK;
        }

        // This is the main execution workhorse of the request, called on the processor thread (of course).
        // It is expected to:
        //  1) Perform its work
        //  2) Dispatch its result by calling DispatchResponse().  Note that this does not have to be done
        //      before this method returns; it could be done later by some other thread.
        //  3) (OPTIONAL) -- react to OnCancelRequested() calls (which will happen off-thread) by 
        //      aborting work and indicating abort via an E_REQUEST_CANCELED result.
        protected abstract void DoWork();

        // This is called on the processor thread when this request gets dequeued for processing.
        void IBackgroundRequest.Execute()
        {
            lock (lockObject)
            {
                // The request may have been canceled while in the queue.
                if (responseDispatched)
                    return;

                // Cancellation from this point forward will only be successful if the request object
                // reacts accordingly.
                this.inProgress = true;
            }

            this.DoWork();
        }

        // Give derivations a chance to customize cancellation.  Note, a lock on the LockObject is
        // held while this is called!
        protected virtual void OnCancelRequested()
        {
        }

        // This can be called from any thread...
        public void Cancel()
        {
            lock (lockObject)
            {
                // Can't cancel anything that's already been dispatched
                if (responseDispatched)
                    return;

                if (this.inProgress)
                {
                    // Requests that have been dequeued and started must voluntarily react to cancel.
                    OnCancelRequested();
                }
                else
                {
                    // This one hasn't started yet, so we can cancel it directly.
                    DispatchResponse(HResult.E_REQUEST_CANCELED);
                }
            }
        }

        // Wait for this request to have its final result dispatched.  Note that this only makes sense
        // to call from the thread that owns the processor's dispatcher.
        public bool WaitForDispatch(int timeout)
        {
            lock (lockObject)
            {
                if (responseDispatched)
                    return true;

                // Note, we only create the event if something actually waits for this request.
                if (workDone == null)
                    workDone = new ManualResetEventSlim();
            }

            return workDone.Wait(timeout);
        }

        public bool ListenToDispatchEvent(EventHandler handler)
        {
            lock (lockObject)
            {
                if (responseDispatched)
                    return false;

                this.Dispatched += handler;
                return true;
            }
        }

        // Implementations should call this to dispatch the final result of this request.
        protected virtual void DispatchResponse(HResult result)
        {
            lock (lockObject)
            {
                if (responseDispatched)
                    return;

                responseDispatched = true;
                dispatchedResult = result;
                if (workDone != null)
                    workDone.Set();
            }

            this.Processor.Dispatcher.BeginInvoke((Action<HResult>)InternalOnResponseDispatched, this.Processor.DispatchPriority, result);
        }

        // This call is made on the processor's dispatcher thread (presumably the UI).  It is guaranteed to be
        // called exactly once, provided that this request is enqueued and the processor is not shut down before
        // it gets to it.
        protected virtual void OnResponseDispatched(HResult result)
        {
        }

        private void InternalOnResponseDispatched(HResult result)
        {
            Processor.OnRequestDispatched(this);

            var handler = this.Dispatched;
            if (handler != null)
                handler(this, EventArgs.Empty);

            OnResponseDispatched(result);
            this.IsDispatchComplete = true;
        }
    }

    internal interface IBackgroundRequest
    {
        HResult OnEnqueued(BackgroundRequestProcessor processor);
        void Execute();
    }
}
