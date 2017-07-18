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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HostingAdapter.Hosting.Channel;
using HostingAdapter.Hosting.Contracts;
using HostingAdapter.Hosting.Exceptions;
using HostingAdapter.Utility;
using Newtonsoft.Json.Linq;

namespace HostingAdapter.Hosting.Messages
{
    public class MessageDispatcher
    {
        #region Fields

        private ChannelBase protocolChannel;

        private AsyncContextThread messageLoopThread;

        internal Dictionary<string, Func<Message, IMessageWriter, Task>> requestHandlers =
            new Dictionary<string, Func<Message, IMessageWriter, Task>>();

        internal Dictionary<string, Func<Message, IMessageWriter, Task>> eventHandlers =
            new Dictionary<string, Func<Message, IMessageWriter, Task>>();

        private Action<Message> responseHandler;

        private CancellationTokenSource messageLoopCancellationToken =
            new CancellationTokenSource();

        #endregion

        #region Properties

        public System.Threading.SynchronizationContext SynchronizationContext { get; private set; }

        public bool InMessageLoopThread
        {
            get
            {
                // We're in the same thread as the message loop if the
                // current synchronization context equals the one we
                // know.
                return SynchronizationContext.Current == this.SynchronizationContext;
            }
        }

        protected IMessageReader MessageReader { get; private set; }

        protected IMessageWriter MessageWriter { get; private set; }


        #endregion

        #region Constructors

        public MessageDispatcher(ChannelBase protocolChannel)
        {
            this.protocolChannel = protocolChannel;
            
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            this.MessageReader = protocolChannel.MessageReader;
            this.MessageWriter = protocolChannel.MessageWriter;

            // Start the main message loop thread.  The Task is
            // not explicitly awaited because it is running on
            // an independent background thread.
            this.messageLoopThread = new AsyncContextThread("Message Dispatcher");
            this.messageLoopThread
                .Run(() => this.ListenForMessages(this.messageLoopCancellationToken.Token))
                .ContinueWith(this.OnListenTaskCompleted);
        }

        public void Stop()
        {
            // Stop the message loop thread
            if (this.messageLoopThread != null)
            {
                this.messageLoopCancellationToken.Cancel();
                this.messageLoopThread.Stop();
            }
        }

        public void SetRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Func<TParams, RequestContext<TResult>, Task> requestHandler)
        {
            this.SetRequestHandler(
                requestType,
                requestHandler,
                false);
        }

        public void SetRequestHandler<TParams, TResult>(
            RequestType<TParams, TResult> requestType,
            Func<TParams, RequestContext<TResult>, Task> requestHandler,
            bool overrideExisting)
        {
            if (overrideExisting)
            {
                // Remove the existing handler so a new one can be set
                this.requestHandlers.Remove(requestType.MethodName);
            }

            this.requestHandlers.Add(
                requestType.MethodName,
                (requestMessage, messageWriter) =>
                {
                    var requestContext =
                        new RequestContext<TResult>(
                            requestMessage,
                            messageWriter);

                    TParams typedParams = default(TParams);
                    if (requestMessage.Contents != null)
                    {
                        // TODO: Catch parse errors!
                        typedParams = requestMessage.Contents.ToObject<TParams>();
                    }

                    return requestHandler(typedParams, requestContext);
                });
        }

        public void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler)
        {
            this.SetEventHandler(
                eventType,
                eventHandler,
                false);
        }

        public void SetEventHandler<TParams>(
            EventType<TParams> eventType,
            Func<TParams, EventContext, Task> eventHandler,
            bool overrideExisting)
        {
            if (overrideExisting)
            {
                // Remove the existing handler so a new one can be set
                this.eventHandlers.Remove(eventType.MethodName);
            }

            this.eventHandlers.Add(
                eventType.MethodName,
                (eventMessage, messageWriter) =>
                {
                    var eventContext = new EventContext(messageWriter);

                    TParams typedParams = default(TParams);
                    if (eventMessage.Contents != null)
                    {
                        try
                        {
                            typedParams = eventMessage.Contents.ToObject<TParams>();
                        }
                        catch (Exception ex)
                        {
                            Logger.Write(LogLevel.Verbose, ex.ToString());
                        }
                    }

                    return eventHandler(typedParams, eventContext);
                });
        }

        public void SetResponseHandler(Action<Message> responseHandler)
        {
            this.responseHandler = responseHandler;
        }

        #endregion

        #region Events

        public event EventHandler<Exception> UnhandledException;

        protected void OnUnhandledException(Exception unhandledException)
        {
            if (this.UnhandledException != null)
            {
                this.UnhandledException(this, unhandledException);
            }
        }

        #endregion

        #region Private Methods

        private async Task ListenForMessages(CancellationToken cancellationToken)
        {
            this.SynchronizationContext = SynchronizationContext.Current;

            // Run the message loop
            while (!cancellationToken.IsCancellationRequested)
            {
                Message newMessage;

                try
                {
                    // Read a message from the channel
                    newMessage = await this.MessageReader.ReadMessage();
                }
                catch (MessageParseException e)
                {
                    string message = string.Format("Exception occurred while parsing message: {0}", e.Message);
                    Logger.Write(LogLevel.Error, message);
                    //await MessageWriter.WriteEvent(HostingErrorEvent.Type, new HostingErrorParams { Message = message });

                    // Continue the loop
                    continue;
                }
                catch (EndOfStreamException e)
                {
                    // The stream has ended, end the message loop
                    //await MessageWriter.WriteEvent(HostDisconnectEvent.Type, new HostDisconnectEventParams { Message = e.Message });

                    newMessage = Message.Event(HostDisconnectEvent.Type.MethodName, JToken.FromObject(new HostDisconnectEventParams { Message = e.Message }));
                    await this.DispatchMessage(newMessage, this.MessageWriter);

                    break;
                }
                catch (Exception e)
                {
                    // Log the error and send an error event to the client
                    string message = string.Format("Exception occurred while receiving message: {0}", e.Message);
                    Logger.Write(LogLevel.Error, message);
                    //await MessageWriter.WriteEvent(HostingErrorEvent.Type, new HostingErrorParams { Message = message });

                    // Continue the loop
                    continue;
                }

                // The message could be null if there was an error parsing the
                // previous message.  In this case, do not try to dispatch it.
                if (newMessage != null)
                {
                    // Verbose logging
                    string logMessage = string.Format("Received message of type[{0}] and method[{1}]",
                        newMessage.MessageType, newMessage.Method);
                    Logger.Write(LogLevel.Verbose, logMessage);

                    // Process the message
                    await this.DispatchMessage(newMessage, this.MessageWriter);
                }
            }
        }

        protected async Task DispatchMessage(
            Message messageToDispatch,
            IMessageWriter messageWriter)
        {
            Task handlerToAwait = null;

            if (messageToDispatch.MessageType == MessageType.Request)
            {
                Func<Message, IMessageWriter, Task> requestHandler = null;
                if (this.requestHandlers.TryGetValue(messageToDispatch.Method, out requestHandler))
                {
                    handlerToAwait = requestHandler(messageToDispatch, messageWriter);
                }
                // else
                // {
                //     // TODO: Message not supported error
                // }
            }
            else if (messageToDispatch.MessageType == MessageType.Response)
            {
                if (this.responseHandler != null)
                {
                    this.responseHandler(messageToDispatch);
                }
            }
            else if (messageToDispatch.MessageType == MessageType.Event)
            {
                Func<Message, IMessageWriter, Task> eventHandler = null;
                if (this.eventHandlers.TryGetValue(messageToDispatch.Method, out eventHandler))
                {
                    handlerToAwait = eventHandler(messageToDispatch, messageWriter);
                }
                else
                {
                    // TODO: Message not supported error
                }
            }
            // else
            // {
            //     // TODO: Return message not supported
            // }

            if (handlerToAwait != null)
            {
                try
                {
                    await handlerToAwait;
                }
                catch (TaskCanceledException)
                {
                    // Some tasks may be cancelled due to legitimate
                    // timeouts so don't let those exceptions go higher.
                }
                catch (AggregateException e)
                {
                    if (!(e.InnerExceptions[0] is TaskCanceledException))
                    {
                        // Cancelled tasks aren't a problem, so rethrow
                        // anything that isn't a TaskCanceledException
                        throw e;
                    }
                }
            }
        }

        internal void OnListenTaskCompleted(Task listenTask)
        {
            if (listenTask.IsFaulted)
            {
                this.OnUnhandledException(listenTask.Exception);
            }
            else if (listenTask.IsCompleted || listenTask.IsCanceled)
            {
                // TODO: Dispose of anything?
            }
        }

        #endregion
    }
}
