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

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HostingAdapter.Hosting.Messages;

namespace HostingAdapter.Hosting.Channel
{
    /// <summary>
    /// Provides a client implementation for the standard I/O channel.
    /// Launches the server process and then attaches to its console
    /// streams.
    /// </summary>
    public class StdioClientChannel : ChannelBase
    {
        private string serviceProcessPath;
        private string serviceProcessArguments;

        private Stream inputStream;
        private Stream outputStream;
        private Process serviceProcess;

        /// <summary>
        /// Gets the process ID of the server process.
        /// </summary>
        public int ProcessId { get; private set; }

        /// <summary>
        /// Initializes an instance of the StdioClient.
        /// </summary>
        /// <param name="serverProcessPath">The full path to the server process executable.</param>
        /// <param name="serverProcessArguments">Optional arguments to pass to the service process executable.</param>
        public StdioClientChannel(
            string serverProcessPath,
            params string[] serverProcessArguments): base(null)
        {
            this.serviceProcessPath = serverProcessPath;

            if (serverProcessArguments != null)
            {
                this.serviceProcessArguments =
                    string.Join(
                        " ",
                        serverProcessArguments);
            }
        }

        protected override void Initialize()
        {
            this.serviceProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = this.serviceProcessPath,
                    Arguments = this.serviceProcessArguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                },
                EnableRaisingEvents = true,
            };

            // Start the process
            this.serviceProcess.Start();
            this.ProcessId = this.serviceProcess.Id;

            // Open the standard input/output streams
            this.inputStream = this.serviceProcess.StandardOutput.BaseStream;
            this.outputStream = this.serviceProcess.StandardInput.BaseStream;

            // Set up the message reader and writer
            this.MessageReader =
                new MessageReader(
                    inputStream,
                    this.MessageSerializer);

            this.MessageWriter =
                new MessageWriter(
                    outputStream,
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
            if (this.inputStream != null)
            {
                this.inputStream.Dispose();
                this.inputStream = null;
            }

            if (this.outputStream != null)
            {
                this.outputStream.Dispose();
                this.outputStream = null;
            }

            if (this.MessageReader != null)
            {
                this.MessageReader = null;
            }

            if (this.MessageWriter != null)
            {
                this.MessageWriter = null;
            }

            this.serviceProcess.Kill();
        }
    }
}
