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

using System.IO;
using System.Text;
using System.Threading.Tasks;
using HostingAdapter.Hosting.Messages;
using HostingAdapter.Hosting.Serializers;

namespace HostingAdapter.Hosting.Channel
{
    /// <summary>
    /// Provides a server implementation for the standard I/O channel.
    /// When started in a process, attaches to the console I/O streams
    /// to communicate with the client that launched the process.
    /// </summary>
    public class StdioServerChannel : ChannelBase
    {
        private Stream inputStream;
        private Stream outputStream;

        public StdioServerChannel(IMessageSerializer messageSerializer) :  base(messageSerializer)
        {

        }

        protected override void Initialize()
        {
#if !NanoServer
            // Ensure that the console is using UTF-8 encoding
            System.Console.InputEncoding = Encoding.UTF8;
            System.Console.OutputEncoding = Encoding.UTF8;
#endif

            // Open the standard input/output streams
            this.inputStream = System.Console.OpenStandardInput();
            this.outputStream = System.Console.OpenStandardOutput();

            // Set up the reader and writer
            this.MessageReader =
                new MessageReader(
                    this.inputStream,
                    this.MessageSerializer);

            this.MessageWriter =
                new MessageWriter(
                    this.outputStream,
                    this.MessageSerializer);

            this.IsConnected = true;
        }

        public override Task WaitForConnection()
        {
            // We're always connected immediately in the stdio channel
            return Task.FromResult(true);
        }

        protected override void Shutdown()
        {
            // No default implementation needed, streams will be
            // disposed on process shutdown.
        }
    }
}
