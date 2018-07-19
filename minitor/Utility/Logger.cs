using System;

namespace Minitor.Utility
{
    //--------------------------------------------------------------------------
    public static class Logger
    {
#if DEBUG
        private static bool IsDebug => true;
#else
        private static bool IsDebug => System.Diagnostics.Debugger.IsAttached;
#endif

        //----------------------------------------------------------------------
        public static void Debug(string s)
        {
            if (IsDebug)
                Console.WriteLine(s);
        }

        public static void Debug(FormattableString s)
        {
            if (IsDebug)
                Console.WriteLine(s.ToString());
        }

        public static void Debug(Exception e)
        {
            if (IsDebug)
                Console.WriteLine(e.ToString());
        }

        //----------------------------------------------------------------------
        public static void Write(string s)
        {
            Console.WriteLine(s);
        }

        public static void Write(FormattableString s)
        {
            Console.WriteLine(s.ToString());
        }

        public static void Write(Exception e)
        {
            if (IsDebug)
                Console.WriteLine(e.ToString());
            else
                Console.WriteLine(e.Message);
        }

        //----------------------------------------------------------------------
        public static void Error(string s)
        {
            Console.WriteLine(s);
        }

        public static void Error(FormattableString s)
        {
            Console.WriteLine(s.ToString());
        }

        public static void Error(Exception e)
        {
            if (IsDebug)
                Console.WriteLine(e.ToString());
            else
                Console.WriteLine(e.Message);
        }
    }
}
