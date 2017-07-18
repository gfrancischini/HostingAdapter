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

using System.Threading.Tasks;
using HostingAdapter.Hosting.Messages;
using HostingAdapter.Hosting.Serializers;

namespace HostingAdapter.Hosting.Channel
{

    /// <summary>
    /// Defines a base implementation for servers and their clients over a
    /// single kind of communication channel.
    /// </summary>
    public abstract class ChannelBase
    {
        /// <summary>
        /// Gets a boolean that is true if the channel is connected or false if not.
        /// </summary>
        public bool IsConnected { get; protected set; }

        /// <summary>
        /// Gets the MessageReader for reading messages from the channel.
        /// </summary>
        public IMessageReader MessageReader { get; protected set; }

        /// <summary>
        /// Gets the MessageWriter for writing messages to the channel.
        /// </summary>
        public IMessageWriter MessageWriter { get; protected set; }

        /// <summary>
        /// The IMessageSerializer to use for message serialization.
        /// </summary>
        /// <param name="messageProtocolType">The type of message protocol used by the channel.</param>
        public IMessageSerializer MessageSerializer { get; protected set; }

        public ChannelBase(IMessageSerializer messageSerializer)
        {
            this.MessageSerializer = messageSerializer;
        }

        /// <summary>
        /// Starts the channel and initializes the MessageDispatcher.
        /// </summary>
        public void Start()
        {
            this.Initialize();
        }

        /// <summary>
        /// Returns a Task that allows the consumer of the ChannelBase
        /// implementation to wait until a connection has been made to
        /// the opposite endpoint whether it's a client or server.
        /// </summary>
        /// <returns>A Task to be awaited until a connection is made.</returns>
        public abstract Task WaitForConnection();

        /// <summary>
        /// Stops the channel.
        /// </summary>
        public void Stop()
        {
            this.Shutdown();
        }

        /// <summary>
        /// A method to be implemented by subclasses to handle the
        /// actual initialization of the channel and the creation and
        /// assignment of the MessageReader and MessageWriter properties.
        /// </summary>
        protected abstract void Initialize();


        /// <summary>
        /// A method to be implemented by subclasses to handle shutdown
        /// of the channel once Stop is called.
        /// </summary>
        protected abstract void Shutdown();
    }

}
