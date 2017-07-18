//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using HostingAdapter.Hosting.Contracts;
using HostingAdapter.Hosting.Messages;
using HostingAdapter.Hosting.Protocol;
using Newtonsoft.Json.Linq;

namespace HostingAdapter.Hosting
{
    public class RequestContext<TResult> : IEventSender
    {
        public  Message RequestMessage
        {
            get;
        }
        private readonly IMessageWriter messageWriter;

        public RequestContext(Message requestMessage, IMessageWriter messageWriter)
        {
            this.RequestMessage = requestMessage;
            this.messageWriter = messageWriter;
        }

        public RequestContext() { }

        public virtual async Task SendResult(TResult resultDetails)
        {
            await this.messageWriter.WriteResponse(
                resultDetails,
                RequestMessage.Method,
                RequestMessage.Id);
        }

        public virtual async Task SendEvent<TParams>(EventType<TParams> eventType, TParams eventParams)
        {
            await this.messageWriter.WriteEvent(
                eventType,
                eventParams);
        }

        public virtual async Task SendError(object errorDetails)
        {
            await this.messageWriter.WriteMessage(
                Message.ResponseError(
                    RequestMessage.Id,
                    RequestMessage.Method,
                    JToken.FromObject(errorDetails)));
        }
    }
}
