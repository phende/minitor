﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using Minitor.Engine;

namespace Minitor
{
    //--------------------------------------------------------------------------
    static class Helpers
    {
        private const string _resourcesPrefix = "Minitor.Resources.";
        private static Dictionary<string, Status> _statuses;

        //----------------------------------------------------------------------
        public static Stream GetResourceStream(string name)
        {
            return typeof(Helpers).Assembly.GetManifestResourceStream(_resourcesPrefix + name);
        }

        //----------------------------------------------------------------------
        public static bool TryParseTimeSpan(string text, out TimeSpan timeSpan)
        {
            char ch;
            int multiplier;

            timeSpan = default(TimeSpan);

            if (string.IsNullOrWhiteSpace(text))
                return false;

            ch = text[text.Length - 1];
            multiplier = 0;
            switch (ch)
            {
                case 's': multiplier = 1; break;
                case 'm': multiplier = 60 * 1; break;
                case 'h': multiplier = 60 * 60 * 1; break;
                case 'd': multiplier = 24 * 60 * 60 * 1; break;
                case 'w': multiplier = 7 * 24 * 60 * 60 * 1; break;
                case 'y': multiplier = 365 * 24 * 60 * 60 * 1; break;
            }

            if (multiplier != 0)
                text = text.Substring(0, text.Length - 1);
            else
                multiplier = 60;

            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
                return false;

            timeSpan = TimeSpan.FromSeconds(num * multiplier);
            return true;
        }

        //----------------------------------------------------------------------
        public static bool TryParseStatus(string text, out Status status)
        {
            if (_statuses == null)
            {
                Dictionary<string, Status> statuses = new Dictionary<string, Status>();
                foreach (string s in Enum.GetNames(typeof(Status)))
                    statuses.Add(s.ToLowerInvariant(), (Status)Enum.Parse(typeof(Status), s));
                _statuses = statuses;
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                text = text.ToLowerInvariant();
                foreach (string s in _statuses.Keys)
                    if (s.StartsWith(text))
                    {
                        status = _statuses[s];
                        return true;
                    }
            }
            status = default(Status);
            return false;
        }
    }
}
