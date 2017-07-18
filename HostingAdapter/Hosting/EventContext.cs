//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Threading.Tasks;
using HostingAdapter.Hosting.Contracts;
using HostingAdapter.Hosting.Messages;

namespace HostingAdapter.Hosting
{
    /// <summary>
    /// Provides context for a received event so that handlers
    /// can write events back to the channel.
    /// </summary>
    public class EventContext
    {
        private readonly IMessageWriter messageWriter;

        /// <summary>
        /// Parameterless constructor required for mocking
        /// </summary>
        public EventContext() { }

        public EventContext(IMessageWriter messageWriter)
        {
            this.messageWriter = messageWriter;
        }

        public async Task SendEvent<TParams>(
            EventType<TParams> eventType,
            TParams eventParams)
        {
            await this.messageWriter.WriteEvent(
                eventType,
                eventParams);
        }
    }
}
