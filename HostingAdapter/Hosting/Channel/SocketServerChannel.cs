/*
MIT License

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
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using HostingAdapter.Hosting.Messages;
using HostingAdapter.Hosting.Serializers;

namespace HostingAdapter.Hosting.Channel
{
    public class SocketServerChannel : ChannelBase
    {
        /// <summary>
        /// The port of the connection
        /// </summary>
        public int Port
        {
            get; private set;
        }

        /// <summary>
        /// The socket server
        /// </summary>
        private Socket ClientSocket
        {
            get; set;
        }

        /// <summary>
        /// Create an instance of the TcpListener class.
        /// </summary>
        private TcpListener TcpListener
        {
            get; set;
        }

        /// <summary>
        /// Construct a new Socket Server Channel on the required port
        /// </summary>
        /// <param name="messageSerializer"></param>
        /// <param name="port"></param>
        public SocketServerChannel(IMessageSerializer messageSerializer, int port) :  base(messageSerializer)
        {
            this.Port = port;
        }

        /// <summary>
        /// Initialize the socket server
        /// </summary>
        protected override void Initialize()
        {
            //retrieve the local ip address
            IPAddress ipAddress = GetPhysicalIPAdress();

            // Set the listener on the local IP address and specify the port.
            TcpListener = new TcpListener(ipAddress, this.Port);

            // start listening,
            TcpListener.Start();
        
            this.IsConnected = false;
        }

        /// <summary>
        /// Wait for socket connection
        /// </summary>
        /// <returns></returns>
        public override Task WaitForConnection()
        {

            var SocketTask = TcpListener.AcceptSocketAsync();

            if (SocketTask.Result != null)
            {
                ClientSocket = SocketTask.Result;

                // Set up the reader and writer
                NetworkStream networkStream = new NetworkStream(ClientSocket);

                this.SetupMessageStreams(networkStream, networkStream);

                this.IsConnected = true;

                return Task.FromResult(true);
            }
            return Task.FromResult(false);

        }

        /// <summary>
        /// Setup message streams
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="outputStream"></param>
        protected virtual void SetupMessageStreams(Stream inputStream, Stream outputStream)
        {
            this.MessageReader =
                   new MessageReader(
                       inputStream,
                       MessageSerializer);

            this.MessageWriter =
                new MessageWriter(
                    outputStream,
                    MessageSerializer);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Shutdown()
        {
            // No default implementation needed, streams will be
            // disposed on process shutdown.
            ClientSocket.Shutdown(SocketShutdown.Both);
        }

        /// <summary>
        /// Get the first available phisical ip address
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetPhysicalIPAdress()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }
                var addr = ni.GetIPProperties().GatewayAddresses.FirstOrDefault();
                if (addr != null && !addr.Address.ToString().Equals("0.0.0.0"))
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                return ip.Address;
                            }
                        }
                    }
                }
            }
            return null;
        }

    }

}
