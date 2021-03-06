﻿using Minitor.Status;
using Minitor.Utility;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Minitor.Web
{
    //--------------------------------------------------------------------------
    static class Web
    {
        //----------------------------------------------------------------------
        public static void SetCaching(HttpListenerResponse response, TimeSpan? duration)
        {
            if (duration.HasValue)
                response.AddHeader("Cache-Control", $"private, max-age={duration.Value.TotalSeconds}");
            else
                response.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");
        }

        //----------------------------------------------------------------------
        public static void RespondError(int reqnum, HttpListenerResponse response, HttpStatusCode code)
        {
            Logger.Debug($"{reqnum}: Returning error {code.ToString()}");

            try
            {
                response.StatusCode = (int)code;
                response.StatusDescription = code.ToString();
                response.Close(Encoding.UTF8.GetBytes(code.ToString()), false);
            }
            catch
            {
                try { response.Close(); } catch { }
            }
        }

        //----------------------------------------------------------------------
        public static async Task RespondResourceAsync(int reqnum, HttpListenerResponse response, string path, CancellationToken token)
        {
            string ext;
            Stream stream;

            if ((stream = Helpers.GetResourceStream(path)) == null)
            {
                RespondError(reqnum, response, HttpStatusCode.NotFound);
                return;
            }
            using (stream)
            {
                ext = Path.GetExtension(path).ToLowerInvariant();
                switch (ext)
                {
                    case ".txt":
                        response.ContentType = "text/plain";
                        break;
                    case ".html":
                        response.ContentType = "text/html";
                        break;
                    case ".css":
                        response.ContentType = "text/css";
                        break;
                    case ".ico":
                        response.ContentType = "image/x-icon";
                        break;
                    case ".js":
                        response.ContentType = "application/javascript";
                        break;
                    default:
                        response.ContentType = "application/octet-stream";
                        Logger.Debug($"{reqnum}: Mime type for {ext} is not defined");
                        break;
                }

                //FIXME for some reason, serving bootstrap.min.css from resources sometimes fail
                // around here with an ObjectDisposedException or similar. Tried caching in a byte
                // array with identical results. Workaround for now is to serve bootstrap from CDN.
                // Problem probably has to do with resource size.
                response.ContentLength64 = stream.Length;

                // This does not help...
                //response.SendChunked = true;

                await stream.CopyToAsync(response.OutputStream, 8192, token);

                // ...and this also not
                //await response.OutputStream.FlushAsync(token);

                response.Close();
            }
        }

        //----------------------------------------------------------------------
        public static string ExtractPathLevel(string path, out string remaining)
        {
            int pos;
            int prefixed;

            prefixed = (path.Length > 0 && path[0] == '/') ? 1 : 0;
            pos = path.IndexOf('/', prefixed);
            if (pos >= 0)
            {
                remaining = path.Substring(pos + 1);
                return path.Substring(prefixed, pos - prefixed);
            }
            else
            {
                remaining = string.Empty;
                return path.Substring(prefixed);
            }
        }

        //----------------------------------------------------------------------
        public static ArraySegment<byte> GetEventBytes(StatusEvent evnt, ref byte[] buffer)
        {
            int count;

            count = 0;

            JsonBuffer.AppendByte((byte)'{', ref buffer, ref count);

            JsonBuffer.AppendProperty("type", evnt.EventType.ToString(), ref buffer, ref count);

            if (evnt.Id != 0)
            {
                JsonBuffer.AppendByte((byte)',', ref buffer, ref count);
                JsonBuffer.AppendProperty("id", evnt.Id.ToString(), ref buffer, ref count);
            }

            if (evnt.Name != null)
            {
                JsonBuffer.AppendByte((byte)',', ref buffer, ref count);
                JsonBuffer.AppendProperty("name", evnt.Name, ref buffer, ref count);
            }

            if (evnt.Text != null)
            {
                JsonBuffer.AppendByte((byte)',', ref buffer, ref count);
                JsonBuffer.AppendProperty("text", evnt.Text, ref buffer, ref count);
            }


            if (evnt.Status != null)
            {
                JsonBuffer.AppendByte((byte)',', ref buffer, ref count);
                JsonBuffer.AppendProperty("status", evnt.Status.Value.ToString(), ref buffer, ref count);
            }

            JsonBuffer.AppendByte((byte)'}', ref buffer, ref count);

            return new ArraySegment<byte>(buffer, 0, count);
        }
    }
}
