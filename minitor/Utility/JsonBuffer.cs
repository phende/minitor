using System;
using System.Collections.Generic;
using System.Text;

namespace Minitor.Utility
{
    //--------------------------------------------------------------------------
    // Simple and ugly way to construct JSON in a bytes array
    static class JsonBuffer
    {
        //----------------------------------------------------------------------
        public static void AppendProperty(string name, string value, ref byte[] buffer, ref int count)
        {
            AppendString(name, ref buffer, ref count);
            AppendByte((byte)':', ref buffer, ref count);
            AppendString(value, ref buffer, ref count);
        }

        //----------------------------------------------------------------------
        public static void AppendProperty(string name, int value, ref byte[] buffer, ref int count)
        {
            AppendString(name, ref buffer, ref count);
            AppendByte((byte)':', ref buffer, ref count);
            AppendStringRaw(value.ToString(), ref buffer, ref count);
        }

        //----------------------------------------------------------------------
        public static void AppendString(string s, ref byte[] buffer, ref int count)
        {
            char ch;
            bool escape;

            AppendByte((byte)'"', ref buffer, ref count);

            escape = false;
            for (int i = 0; i < s.Length; i++)
            {
                ch = s[i];
                if (ch == '"' || ch == '\\' || ch < ' ')
                {
                    escape = true;
                    break;
                }
            }

            if (escape)
                AppendEscapedStringRaw(s, ref buffer, ref count);
            else
                AppendStringRaw(s, ref buffer, ref count);

            AppendByte((byte)'"', ref buffer, ref count);
        }

        //----------------------------------------------------------------------
        private static void AppendEscapedStringRaw(string s, ref byte[] buffer, ref int count)
        {
            List<char> list;
            char[] chars;
            int len;
            ushort u;
            int d;


            list = new List<char>(s.Length * 2);
            foreach (char ch in s)
            {
                if (ch == '"')
                {
                    list.Add('\\');
                    list.Add('"');
                }
                else if (ch == '\\')
                {
                    list.Add('\\');
                    list.Add('\\');
                }
                else if (ch < ' ')
                {
                    list.Add('\\');
                    if (ch == '\b')
                        list.Add('b');
                    else if (ch == '\f')
                        list.Add('f');
                    else if (ch == '\n')
                        list.Add('n');
                    else if (ch == '\r')
                        list.Add('r');
                    else if (ch == '\t')
                        list.Add('t');
                    else
                    {
                        list.Add('u');
                        u = ch;
                        for (int i = 0; i < 4; i++)
                        {
                            d = (int)((u & 0xf000u) >> 12);
                            if (d <= 9)
                                list.Add((char)('0' + d));
                            else
                                list.Add((char)('a' + (d - 10)));
                            u <<= 4;
                        }
                    }
                }
                else list.Add(ch);

            }
            chars = list.ToArray();
            len = Encoding.UTF8.GetByteCount(chars);
            EnsureSpace(len, ref buffer, ref count);
            count += Encoding.UTF8.GetBytes(chars, 0, chars.Length, buffer, count);
        }

        //----------------------------------------------------------------------
        public static void AppendStringRaw(string s, ref byte[] buffer, ref int count)
        {
            int len;

            len = Encoding.UTF8.GetByteCount(s);
            EnsureSpace(len, ref buffer, ref count);
            count += Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, count);
        }

        //----------------------------------------------------------------------
        public static void AppendBytes(byte[] data, ref byte[] buffer, ref int count)
        {
            EnsureSpace(data.Length, ref buffer, ref count);
            Array.Copy(data, 0, buffer, count, data.Length);
            count += data.Length;
        }

        //----------------------------------------------------------------------
        public static void AppendByte(byte b, ref byte[] buffer, ref int count)
        {
            EnsureSpace(1, ref buffer, ref count);
            buffer[count++] = b;
        }

        //----------------------------------------------------------------------
        private static void EnsureSpace(int data, ref byte[] buffer, ref int count)
        {
            byte[] nbuffer;
            int nlen;

            if (count + data > buffer.Length)
            {
                nlen = buffer.Length * 2;
                while (count + data > nlen) nlen *= 2;
                nbuffer = new byte[nlen];
                Array.Copy(buffer, nbuffer, count);
                buffer = nbuffer;
            }
        }
    }
}
