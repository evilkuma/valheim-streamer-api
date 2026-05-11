using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace ValheimStreamerApi
{
    public static class JsonParser
    {
        public static TResult Parse<TResult>(HttpListenerRequest request)
        {
            string requestBody;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                requestBody = reader.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<TResult>(requestBody);
        }

        public static TResult Parse<TResult>(string value)
        {
            return JsonConvert.DeserializeObject<TResult>(value);
        }

        public static TResult Parse<TResult>(ZPackage pkg)
        {
            string jsonData = pkg.ReadString();
            return JsonConvert.DeserializeObject<TResult>(jsonData);
        }

        public static string Serialize(object data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }
}
