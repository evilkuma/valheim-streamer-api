using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ValheimStreamerApi
{
    public class HttpEventArgs : EventArgs
    {
        public HttpListenerRequest Request { get; }
        public HttpListenerResponse Response { get; }
        public string Action { get; }

        public HttpEventArgs(HttpListenerRequest request, HttpListenerResponse response, string action = null)
        {
            Request = request;
            Response = response;
            Action = action;
        }
    }

    public class HttpServer : IDisposable
    {
        private readonly EventManager Events;

        private HttpListener listener;
        private Thread serverThread;
        private bool isRunning;

        public HttpServer(int port, EventManager events)
        {
            Events = events;

            listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{port}/");
            listener.Prefixes.Add($"http://localhost:{port}/");
        }

        public void Start()
        {
            if (!HttpListener.IsSupported)
            {
                Log.LogError("HttpListener не поддерживается");
                return;
            }

            try
            {
                listener.Start();
                isRunning = true;
                serverThread = new Thread(async () => await HandleRequests());
                serverThread.Start();

                Log.LogInfo("HTTP сервер запущен");
            }
            catch (Exception ex)
            {
                Log.LogError($"Ошибка запуска: {ex.Message}");
            }
        }

        private async Task HandleRequests()
        {
            while (isRunning)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequest(context));
                }
                catch (Exception ex)
                {
                    Log.LogError($"Ошибка обработки: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                response.ContentType = "application/json";

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.Close();
                    return;
                }

                var (eventName, action) = ParsePath(request.Url.AbsolutePath);
                if (eventName != null)
                {
                    object result = await Events.Dispatch<object>(eventName, this, new HttpEventArgs(request, response, action));
                    await WriteJsonResponse(response, result);
                }
                else
                {
                    response.StatusCode = 404;
                    await WriteJsonResponse(response, new { error = "Endpoint not found" });
                }
            }
            catch (TimeoutException ex)
            {
                Log.LogError($"Таймаут RPC: {ex.Message}");
                response.StatusCode = 504;
                await WriteJsonResponse(response, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                Log.LogError($"Ошибка ProcessRequest: {ex.Message}");
                response.StatusCode = 500;
                await WriteJsonResponse(response, new { error = ex.Message });
            }
            finally
            {
                response.Close();
            }
        }

        private (string eventName, string action) ParsePath(string absolutePath)
        {
            string path = absolutePath.ToLower().TrimEnd('/');

            if (Events.HasEvent(path))
                return (path, null);

            int lastSlash = path.LastIndexOf('/');
            if (lastSlash > 0)
            {
                string basePath = path.Substring(0, lastSlash);
                string action = absolutePath.TrimEnd('/').Substring(lastSlash + 1);

                if (Events.HasEvent(basePath))
                    return (basePath, action);
            }

            return (null, null);
        }

        private async Task WriteJsonResponse(HttpListenerResponse response, object data)
        {
            string json = JsonParser.Serialize(data);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        public void Dispose()
        {
            isRunning = false;
            serverThread?.Join(1000);
        }
    }
}
