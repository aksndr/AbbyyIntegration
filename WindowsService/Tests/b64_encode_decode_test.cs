using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Web;
using System.Net;

namespace WindowsService.Tests
{
    class b64_encode_decode_test
    {

        public static void Main2()
        {
            //Загрузить файл в массив
            byte[] content = File.ReadAllBytes("G:\\Temp\\1\\1\\0000555.pdf"); //New_Market_Leader_-_Upper-Intermediate_Practice0001


            uploadContentInTempObject("0000555", content, "pdf");
            
            ////Перевести в Base64
            //string b64 = System.Convert.ToBase64String(content);

            ////Encode URL
            //byte[] data = Encoding.UTF8.GetBytes(b64);

            ////Decode URL
            //string b64_b = Encoding.UTF8.GetString(data);

            ////Перевести из Base64 в массив
            //byte[] content_b = System.Convert.FromBase64String(b64_b);

            ////Собрать файл
            //FileStream fs = new FileStream("G:\\Temp\\1\\1\\0000555_b.pdf", FileMode.Create, FileAccess.Write);
            ////Выгрузить файл
            //fs.Write(content_b, 0, content_b.Length);
            //fs.Close();
            
        }


        internal static void uploadContentInTempObject(string barcode, byte[] content, string mimeType = "pdf")
        {
            string url = "http://localhost/OTCS/cs.exe";
            string b64 = System.Convert.ToBase64String(content);

            //b64 = b64.Split('=')[0]; 
            b64 = b64.Replace('+', '-'); 
            b64 = b64.Replace('/', '_');

            string filename = barcode + "_" + DateTime.Now.Ticks;

            string p = "func=abbyyIntegration.uploadContentInTempObject&barcode=" + filename + "&content=" + b64 + "&fileExt=" + mimeType;

            string r = makePostRequest(getAuthToken(), url, p);          



        }

        private static string makePostRequest(string token, string url, String param)
        {
            Stream receiveStream = null;
            try
            {
                HttpWebRequest requestLL = (HttpWebRequest)WebRequest.Create(url);

                byte[] data = Encoding.UTF8.GetBytes(param);

                requestLL.Method = "POST";
                requestLL.Referer = "http://sibcon.org";
                requestLL.ContentType = "application/x-www-form-urlencoded";
                requestLL.Headers.Add("Cookie", "LLCookie=" + token + ", LLTZCookie=0");
                requestLL.ContentLength = data.Length;

                using (var stream = requestLL.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                }
                HttpWebResponse responseLL = null;
                responseLL = (HttpWebResponse)requestLL.GetResponse();
                receiveStream = responseLL.GetResponseStream();

                StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
                String responseText = readStream.ReadToEnd();
                return responseText;
            }
            finally
            {
                if (receiveStream != null)
                {
                    receiveStream.Close();
                    receiveStream.Dispose();
                }

            }
        }

        public static string getAuthToken()
        {           
            Authentication.AuthenticationClient authClient = new Authentication.AuthenticationClient();
            try
            {
                string authToken = authClient.AuthenticateUser("Admin","livelink");                
                return authToken;
            }
            catch (Exception e)
            {
                Console.Write("Exception while proceeding authentication.");
            }
            finally
            {
                authClient.Close();
            }
            return null;
        }

    }
}
