using System;

namespace Minitor
{
    //--------------------------------------------------------------------------
    public static class Configuration
    {
        //----------------------------------------------------------------------
        static Configuration()
        {
            DefaultValidity = TimeSpan.FromMinutes(60);
            DefaultExpiration = TimeSpan.FromHours(48);
#if DEBUG
            TrimInterval = TimeSpan.FromSeconds(System.Diagnostics.Debugger.IsAttached ? 2 : 10);
            Binding = "http://localhost:12345/";
#else
            TrimInterval = TimeSpan.FromSeconds(10);
            Binding = "http://+:80/";
#endif
        }

        //----------------------------------------------------------------------
        public static TimeSpan DefaultValidity { get; }
        public static TimeSpan DefaultExpiration { get; }
        public static TimeSpan TrimInterval { get; }
        public static string Binding { get; }
    }
}
