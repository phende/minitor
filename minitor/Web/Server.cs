using Minitor.Engine;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Minitor.Web
{
    //--------------------------------------------------------------------------
    // Web server based on HttpListener
    class Server
    {
        private Engine.Engine _manager;
        private HttpListener _listener;

        //----------------------------------------------------------------------
        public Server()
        {
            if (!HttpListener.IsSupported)
                throw new ApplicationException("HttpListener is not supported on this platform.");

            _manager = new Engine.Engine();
        }

        //----------------------------------------------------------------------
        public Task Run(string uri, CancellationToken token)
        {
            TaskCompletionSource<bool> tcs;

            if (_listener != null)
                throw new InvalidOperationException();

            _listener = new HttpListener();
            _listener.Prefixes.Add(uri);
            _listener.Start();

            tcs = new TaskCompletionSource<bool>();
            Task.Factory.StartNew(() => ListenAsync(tcs, token));
            return tcs.Task;
        }

        //----------------------------------------------------------------------
        private async Task ListenAsync(TaskCompletionSource<bool> tcs, CancellationToken token)
        {
            HttpListenerContext context;
            int reqnum = 0;

            try
            {
                using (token.Register(() => _listener.Stop()))
                {
                    while (_listener.IsListening && !token.IsCancellationRequested)
                    {
                        try
                        {
                            context = await _listener.GetContextAsync();
                        }
                        catch (Exception e)
                        {
                            if (!token.IsCancellationRequested)
                                Log.Write(e);
                            break;
                        }
                        var _ = Task.Factory.StartNew(async ()
                            => await DispatchContextAsync(++reqnum, context, token), token);
                    }
                }
            }
            finally
            {
                _listener.Stop();
                _listener = null;
                tcs.TrySetResult(false);
            }
        }

        //----------------------------------------------------------------------
        private async Task DispatchContextAsync(int reqnum, HttpListenerContext context, CancellationToken token)
        {
            bool isSocket;

            isSocket = context.Request.IsWebSocketRequest;

            Log.Debug($@"{reqnum}: Web{(isSocket ? "Socket" : "")} request from {
                context.Request.RemoteEndPoint} for {context.Request.Url.ToString()}");

            try
            {
                if (isSocket)
                    await HandleSocketRequestAsync(reqnum, context, token);
                else
                    await HandleRequestAsync(reqnum, context, token);
            }
            catch (Exception e)
            {
                if (!token.IsCancellationRequested)
                {
                    Log.Debug($"{reqnum}: Request failed");
                    Log.Write(e);
                }
                Web.RespondError(reqnum, context.Response, HttpStatusCode.InternalServerError);
            }
        }

        //----------------------------------------------------------------------
        private async Task HandleRequestAsync(int reqnum, HttpListenerContext context, CancellationToken token)
        {
            HttpListenerRequest request;
            HttpListenerResponse response;
            string top, path;

            request = context.Request;
            response = context.Response;

            if (request.HttpMethod != "GET")
            {
                Web.RespondError(reqnum, response, HttpStatusCode.MethodNotAllowed);
                return;
            }
            top = Web.ExtractPathLevel(request.Url.LocalPath, out path).ToLowerInvariant();

            switch (top)
            {
                case "":
                    Web.SetCaching(response, null);
                    response.Redirect("status");
                    response.Close();
                    return;

                case "status":
                    Web.SetCaching(response, TimeSpan.FromHours(2));
                    await Web.RespondResourceAsync(reqnum, response, "status.html", token);
                    return;

                case "resource":
                    Web.SetCaching(response, TimeSpan.FromHours(2));
                    await Web.RespondResourceAsync(reqnum, response, path, token);
                    return;

                //case "rest":
                //TODO future REST API
                //    Helpers.SetCaching(response, null);
                //    Helpers.RespondError(response, HttpStatusCode.Unauthorized);
                //    return;

                case "set":
                    Web.SetCaching(response, null);
                    HandleApiRequest(reqnum, path, request, response);
                    return;
            }
            Web.RespondError(reqnum, response, HttpStatusCode.NotFound);
        }

        //----------------------------------------------------------------------
        // Kind of ugly, but indeed goto has some use sometimes
        private void HandleApiRequest(int reqnum, string path, HttpListenerRequest request, HttpListenerResponse response)
        {
            Status sta;
            string str, s;
            TimeSpan ts;

            string monitor;
            string text;
            Status? status;
            TimeSpan? validity;
            TimeSpan? expiration;

            status = null;
            monitor = text = null;
            validity = expiration = null;

            foreach (string key in request.QueryString.AllKeys)
            {
                str = key.ToLowerInvariant();

                //--------------------------------
                if ("monitor".StartsWith(str))
                {
                    if (monitor != null) goto BadRequest;
                    monitor = request.QueryString[key];
                    if (monitor == null) goto BadRequest;
                }
                //--------------------------------
                else if ("text".StartsWith(str))
                {
                    if (text != null) goto BadRequest;
                    text = request.QueryString[key];
                    if (text == null) goto BadRequest;
                }
                //--------------------------------
                else if ("status".StartsWith(str))
                {
                    if (status.HasValue) goto BadRequest;
                    s = request.QueryString[key];
                    if (s == null || !Helpers.TryParseStatus(s, out sta)) goto BadRequest;
                    status = sta;
                }
                //--------------------------------
                else if ("validity".StartsWith(str))
                {
                    if (validity.HasValue) goto BadRequest;
                    s = request.QueryString[key];
                    if (s == null || !Helpers.TryParseTimeSpan(s, out ts)) goto BadRequest;
                    validity = ts;
                }
                //--------------------------------
                else if ("expiration".StartsWith(str))
                {
                    if (expiration.HasValue) goto BadRequest;
                    s = request.QueryString[key];
                    if (s == null || !Helpers.TryParseTimeSpan(s, out ts)) goto BadRequest;
                    expiration = ts;
                }
                //--------------------------------
                else goto BadRequest;
            }

            if (!status.HasValue)
                status = Status.Unknown;

            if (!validity.HasValue)
                validity = Configuration.DefaultValidity;

            if (!expiration.HasValue)
                expiration = Configuration.DefaultExpiration;

            if (_manager.Update(path, monitor, text, status.Value, validity.Value, expiration.Value))
            {
                response.Close();
                return;
            }

            // Yes, this is a goto target
            BadRequest:
            Web.RespondError(reqnum, response, HttpStatusCode.BadRequest);
        }

        //----------------------------------------------------------------------
        private async Task HandleSocketRequestAsync(int reqnum, HttpListenerContext context, CancellationToken token)
        {
            HttpListenerWebSocketContext wscontext;
            WebSocket socket;
            string top;
            string path;
            byte[] recv;
            byte[] send;

            wscontext = await context.AcceptWebSocketAsync(null);
            socket = wscontext.WebSocket;

            top = Web.ExtractPathLevel(context.Request.Url.LocalPath, out path).ToLowerInvariant();
            if (top != "status")
            {
                await socket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "Not a subscription URL", CancellationToken.None);
                return;
            }

            Log.Debug($"{reqnum}: WebSocket client connected to {context.Request.Url.LocalPath} from {context.Request.RemoteEndPoint}");

            using (token.Register(() => socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server terminating", CancellationToken.None)))
            {
                send = new byte[256];
                using (IDisposable sub = _manager.Subscribe(path, async (evnt)
                    => await socket.SendAsync(Web.GetEventBytes(evnt, ref send), WebSocketMessageType.Text, true, token)))
                {
                    recv = new byte[128];
                    while (socket.State == WebSocketState.Open)
                    {
                        var _ = await socket.ReceiveAsync(new ArraySegment<byte>(recv), CancellationToken.None);
                    }
                }
            }

            Log.Debug($"{reqnum}: WebSocket client disconnected from {context.Request.RemoteEndPoint}");
        }
    }
}
