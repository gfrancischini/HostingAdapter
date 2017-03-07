//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HostingAdapter.Hosting.Protocol
{
    public class MessageParseException : Exception
    {
        public string OriginalMessageText { get; private set; }

        public MessageParseException(
            string originalMessageText,
            string errorMessage,
            params object[] errorMessageArgs)
            : base(string.Format(errorMessage, errorMessageArgs))
        {
            this.OriginalMessageText = originalMessageText;
        }
    }
}
