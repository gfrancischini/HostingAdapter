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
