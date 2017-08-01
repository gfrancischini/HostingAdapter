/*
MIT License

Copyright (c) Microsoft. All rights reserved.
Copyright (c) Gabriel Parelli Francischini 2017

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HostingAdapter.Hosting.Channel;
using HostingAdapter.Hosting.Contracts;
using HostingAdapter.Hosting.Messages;
using HostingAdapter.Utility;

namespace HostingAdapter.Hosting.Protocol
{
    /// <summary>
    /// Provides behavior for a client or server endpoint that
    /// communicates using the specified protocol.
    /// </summary>
    public class ProtocolEndpoint : IProtocolEndpoint
    {
        private bool isStarted;
        private int currentMessageId;
        private ChannelBase protocolChannel;
        //private MessageProtocolType messageProtocolType;
        private TaskCompletionSource<bool> endpointExitedTask;
        private SynchronizationContext originalSynchronizationContext;

        private Dictionary<string, TaskCompletionSource<Message>> pendingRequests =
            new Dictionary<string, TaskCompletionSource<Message>>();

        /// <summary>
        /// When true, SendEvent will ignore exceptions and write them
        /// to the log instead. Intended to be used for test scenarios
        /// where SendEvent throws exceptions unrelated to what is
        /// being tested.
        /// </summary>
        internal static bool SendEventIgnoreExceptions = false;

        /// <summary>
        /// Gets the MessageDispatcher which allows registration of
        /// handlers for requests, responses, and events that are
        /// transmitted through the channel.
        /// </summary>
        protected MessageDispatcher MessageDispatcher { get; set; }

        /// <summary>
        /// The maximum time to wait for a message response
        /// </summary>
        private int Timeout { get; set; }

        /// <summary>
        /// Initializes an instance of the protocol server using the
        /// specified channel for communication.
        /// </summary>
        /// <param name="protocolChannel">
        /// The channel to use for communication with the connected endpoint.
        /// </param>
        /// <param name="messageProtocolType">
        /// The type of message protocol used by the endpoint.
        /// </param>
        public ProtocolEndpoint(
            ChannelBase protocolChannel, int timeout = 10000)
        {
			this.originalSynchronizationContext = SynchronizationContext.Current;
			this.Setup(protocolChannel, timeout);
		}
		
		public void Setup(ChannelBase protocolChannel, int timeout = 10000) {
			this.protocolChannel = protocolChannel;
			this.Timeout = timeout;
		}

        /// <summary>
        /// Starts the language server client and sends the Initialize method.
        /// </summary>
        /// <returns>A Task that can be awaited for initialization to complete.</returns>
        public async Task Start()
        {
            if (!this.isStarted)
            {
                // Start the provided protocol channel
                this.protocolChannel.Start();

                // Setup the message dispatcher
                this.MessageDispatcher = new MessageDispatcher(this.protocolChannel);

                // Set the handler for any message responses that come back
                this.MessageDispatcher.SetResponseHandler(this.HandleResponse);

                // Listen for unhandled exceptions from the dispatcher
                this.MessageDispatcher.UnhandledException += MessageDispatcher_UnhandledException;

                // Notify implementation about endpoint start
                await this.OnStart();

                // Wait for connection and notify the implementor
                // NOTE: This task is not meant to be awaited.
                Task waitTask =
                    this.protocolChannel
                        .WaitForConnection()
                        .ContinueWith(
                            async (t) =>
                            {
                                // Start the MessageDispatcher
                                this.MessageDispatcher.Start();

                                // Wait the client signalling with the connection
                                await this.OnConnect();
                            });

                // Endpoint is now started
                this.isStarted = true;
            }
        }

        public void WaitForExit()
        {
            this.endpointExitedTask = new TaskCompletionSource<bool>();
            this.endpointExitedTask.Task.Wait();
        }

        public async Task Stop()
        {
            if (this.isStarted)
            {
                // Make sure no future calls try to stop the endpoint during shutdown
                this.isStarted = false;

                // Stop the implementation first
                await this.OnStop();

                // Stop the dispatcher and channel
                this.MessageDispatcher.Stop();
                this.protocolChannel.Stop();

                // Notify anyone waiting for exit
                if (this.endpointExitedTask != null)
                {
                    this.endpointExitedTask.SetResult(true);
                }
            }
        }

        #region Message Sending

        /// <summary>
        /// Sends a request to the server
        /// </summary>
        /// <typeparam name="TParams"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="requestType"></param>
        /// <param name="requestParams"></param>
        /// <returns></returns>
        public Task<TResult> SendRequest<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            TParams requestParams)
        {
            return this.SendRequest(requestType, requestParams, true);
        }

        public async Task<TResult> SendRequest<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            TParams requestParams,
            bool waitForResponse)
        {
            if (!this.protocolChannel.IsConnected)
            {
                throw new InvalidOperationException("SendRequest called when ProtocolChannel was not yet connected");
            }

            this.currentMessageId++;

            TaskCompletionSource<Message> responseTask = null;

            if (waitForResponse)
            {
                responseTask = new TaskCompletionSource<Message>();
                this.pendingRequests.Add(
                    this.currentMessageId.ToString(),
                    responseTask);
            }

            await this.protocolChannel.MessageWriter.WriteRequest<TParams, TResult>(
                requestType,
                requestParams,
                this.currentMessageId);

            if (responseTask != null)
            {
                //if this is a response task we should wait the maximum timeout. If called by timeout we will return a null response
                var timeoutCancellationTokenSource = new CancellationTokenSource();
                var completedTask = await Task.WhenAny(responseTask.Task, Task.Delay(Timeout, timeoutCancellationTokenSource.Token));

                if (completedTask == responseTask.Task)
                {
                    timeoutCancellationTokenSource.Cancel();
                    var responseMessage = await responseTask.Task;  // Very important in order to propagate exceptions

                    return
                    responseMessage.Contents != null ?
                        responseMessage.Contents.ToObject<TResult>() :
                        default(TResult);
                }
                else
                {
                    //Every response handler must be carefull that timeout will return null object
                    //throw new TimeoutException("The operation has timed out.");
                    return default(TResult);
                }


            }
            else
            {
                // TODO: Better default value here?
                return default(TResult);
            }
        }

        /// <summary>
        /// Sends an event to the channel's endpoint.
        /// </summary>
        /// <typeparam name="TParams">The event parameter type.</typeparam>
        /// <param name="eventType">The type of event being sent.</param>
        /// <param name="eventParams">The event parameters being sent.</param>
        /// <returns>A Task that tracks completion of the send operation.</returns>
        public Task SendEvent<TParams>(
            EventType<TParams> eventType,
            TParams eventParams)
        {
            try
            {
                if (!this.protocolChannel.IsConnected)
                {
                    throw new InvalidOperationException("SendEvent called when ProtocolChannel was not yet connected");
                }

                // Some events could be raised from a different thread.
                // To ensure that messages are written serially, dispatch
                // dispatch the SendEvent call to the message loop thread.

                if (!this.MessageDispatcher.InMessageLoopThread)
                {
                    TaskCompletionSource<bool> writeTask = new TaskCompletionSource<bool>();

                    this.MessageDispatcher.SynchronizationContext.Post(
                        async (obj) =>
                        {
                            await this.protocolChannel.MessageWriter.WriteEvent(
                                eventType,
                                eventParams);

                            writeTask.SetResult(true);
                        }, null);

                    return writeTask.Task;
                }
                else
                {
                    return this.protocolChannel.MessageWriter.WriteEvent(
                        eventType,
                        eventParams);
                }
            }
            catch (Exception ex)
            {
                if (SendEventIgnoreExceptions)
                {
                    Logger.Write(LogLevel.Verbose, "Exception in SendEvent " + ex.ToString());
                }
                else
                {
                    throw;
                }
            }
            return Task.FromResult(false);
        }

        #endregion

        #region Message Handling

        public void SetRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Func<TParams, RequestContext<TResult>, Task> requestHandler)
        {
            this.MessageDispatcher.SetRequestHandler(
                requestType,
                requestHandler);
        }

        public void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler)
        {
            this.MessageDispatcher.SetEventHandler(
                eventType,
                eventHandler,
                false);
        }

        public void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler,
            bool overrideExisting)
        {
            this.MessageDispatcher.SetEventHandler(
                eventType,
                eventHandler,
                overrideExisting);
        }

        private void HandleResponse(Message responseMessage)
        {
            TaskCompletionSource<Message> pendingRequestTask = null;

            if (this.pendingRequests.TryGetValue(responseMessage.Id, out pendingRequestTask))
            {
                pendingRequestTask.SetResult(responseMessage);
                this.pendingRequests.Remove(responseMessage.Id);
            }
        }

        #endregion

        #region Subclass Lifetime Methods

        protected virtual Task OnStart()
        {
            return Task.FromResult(true);
        }

        protected virtual Task OnConnect()
        {
            return Task.FromResult(true);
        }

        protected virtual Task OnStop()
        {
            return Task.FromResult(true);
        }

        #endregion

        #region Event Handlers

        private void MessageDispatcher_UnhandledException(object sender, Exception e)
        {
            if (this.endpointExitedTask != null)
            {
                this.endpointExitedTask.SetException(e);
            }

            else if (this.originalSynchronizationContext != null)
            {
                this.originalSynchronizationContext.Post(o => { throw e; }, null);
            }
        }

        #endregion
    }
}
