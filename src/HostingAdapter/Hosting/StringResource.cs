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

using System.Reflection;
using System.Resources;
using System.Globalization;

namespace HostingAdapter.Hosting
{
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class StringResource
    {
        protected StringResource()
        { }


        public class Keys
        {
            static ResourceManager resourceManager = new ResourceManager("HostingAdapter.Hosting.StringResource", typeof(StringResource).GetTypeInfo().Assembly);

            static CultureInfo _culture = null;

            private Keys()
            { }

            public static CultureInfo Culture
            {
                get
                {
                    return _culture;
                }
                set
                {
                    _culture = value;
                }
            }

            public const string HostingHeaderMissingColon = "HostingHeaderMissingColon";
            public const string HostingUnexpectedEndOfStream = "HostingUnexpectedEndOfStream";
            public const string HostingHeaderMissingContentLengthValue = "HostingHeaderMissingContentLengthValue";
            public const string HostingHeaderMissingContentLengthHeader = "HostingHeaderMissingContentLengthHeader";

            public static string GetString(string key)
            {
                return resourceManager.GetString(key, _culture);
            }


            public static string GetString(string key, object arg0)
            {
                return string.Format(global::System.Globalization.CultureInfo.CurrentCulture, resourceManager.GetString(key, _culture), arg0);
            }


            public static string GetString(string key, object arg0, object arg1)
            {
                return string.Format(global::System.Globalization.CultureInfo.CurrentCulture, resourceManager.GetString(key, _culture), arg0, arg1);
            }


            public static string GetString(string key, object arg0, object arg1, object arg2, object arg3)
            {
                return string.Format(global::System.Globalization.CultureInfo.CurrentCulture, resourceManager.GetString(key, _culture), arg0, arg1, arg2, arg3);
            }


            public static string GetString(string key, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5)
            {
                return string.Format(global::System.Globalization.CultureInfo.CurrentCulture, resourceManager.GetString(key, _culture), arg0, arg1, arg2, arg3, arg4, arg5);
            }

        }


        public static string HostingHeaderMissingColon
        {
            get
            {
                return Keys.GetString(Keys.HostingHeaderMissingColon);
            }
        }

        public static string HostingUnexpectedEndOfStream
        {
            get
            {
                return Keys.GetString(Keys.HostingUnexpectedEndOfStream);
            }
        }

        public static string HostingHeaderMissingContentLengthValue
        {
            get
            {
                return Keys.GetString(Keys.HostingHeaderMissingContentLengthValue);
            }
        }

        public static string HostingHeaderMissingContentLengthHeader
        {
            get
            {
                return Keys.GetString(Keys.HostingHeaderMissingContentLengthHeader);
            }
        }

    }
}
