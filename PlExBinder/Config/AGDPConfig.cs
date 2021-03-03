using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using BCh.KTC.TttEntities.Enums;

namespace BCh.KTC.PlExBinder.Config
{
    public class AGDPConfig
    {
        public static IList<string> GetTrainNumers(string url, IList<CategoryTrain> categories)
        {
            try
            {
                var curUrl = "http://ardp.ktc.rw/Service/GetTrainsForCategory?";
                if (IsTryUrl(url))
                    curUrl = url;
                //
                return (new JavaScriptSerializer()).Deserialize<IList<string>>(Read(GetUrl(curUrl, categories))).ToList();
            }
            catch { return new List<string>(); }
        }

        public static bool IsTryUrl(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }

        private static string GetUrl(string url, IList<CategoryTrain> categories)
        {
            var paramStr = new StringBuilder();
            paramStr.Append(url);
            foreach(var category in categories)
                paramStr.Append($"categories={category}&");
            //
            return paramStr.ToString();
        }

        private static string  Read(string url)
        {
            var result = new StringBuilder();
            var request = WebRequest.Create(url);
            request.Timeout = 30000;
            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        string line = "";
                        while ((line = reader.ReadLine()) != null)
                            result.Append(line);
                    }
                }
            }
            //
            return result.ToString();
        }
    }

}
