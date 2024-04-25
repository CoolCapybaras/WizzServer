using System.Net;

namespace WizzServer
{
	public class HttpAuthHandler
    {
        private HttpListener httpListener;

        public async Task Start()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:8888/auth/");
            httpListener.Start();
            Logger.LogInfo("HTTP auth server started");

            while (true)
            {
                HttpListenerContext context;
                try
                {
                    context = await httpListener.GetContextAsync();
                }
                catch (HttpListenerException)
                {
                    break;
                }

                var request = context.Request;
                var response = context.Response;

                int idx = request.RawUrl!.IndexOf("code") + 5;
                if (idx != 4)
                {
                    string code = request.RawUrl.Substring(idx, request.RawUrl.Length - idx);
                    string accessToken = await VkApi.GetAccessToken(code);
                    var profile = await VkApi.GetUserInfo(accessToken);

                    Console.WriteLine(profile);
                }

                response.OutputStream.Close();
            }
        }

        public void Stop() => httpListener.Close();
    }
}
