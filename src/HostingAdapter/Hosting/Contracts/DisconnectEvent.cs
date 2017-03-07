using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HostingAdapter.Hosting.Contracts
{
    public class HostDisconnectEvent
    {
        public static readonly
            EventType<HostDisconnectEventParams> Type =
            EventType<HostDisconnectEventParams>.Create("HostDisconnect");
    }

    public class HostDisconnectEventParams
    {
        public string Message
        {
            get; set;
        }
    }
}
