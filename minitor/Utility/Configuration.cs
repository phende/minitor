using System;
using System.Diagnostics;

namespace Minitor.Utility
{
    //--------------------------------------------------------------------------
    public static class Configuration
    {
        //----------------------------------------------------------------------
        static Configuration()
        {
#if DEBUG
            Binding = "http://localhost:12345/";
            StatusTrimInterval = TimeSpan.FromSeconds(Debugger.IsAttached ? 2 : 10);
#else
            Binding = "http://+:80/";
            StatusTrimInterval = TimeSpan.FromSeconds(10);
#endif
            StatusValidity = TimeSpan.FromMinutes(60);
            StatusExpiration = TimeSpan.FromHours(48);
        }

        //----------------------------------------------------------------------
        public static string Binding { get; }

        //----------------------------------------------------------------------
        public static TimeSpan StatusValidity { get; }
        public static TimeSpan StatusExpiration { get; }
        public static TimeSpan StatusTrimInterval { get; }
    }
}
