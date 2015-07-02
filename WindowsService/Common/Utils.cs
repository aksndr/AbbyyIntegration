using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Authentication = WindowsService.Authentication;
using ContentService = WindowsService.ContentService;
using DocumentManagement = WindowsService.DocumentManagement;
using WindowsService.Models;
using NLog;


namespace WindowsService.Common
{
    public class Utils       
    {
        private static Utils instance;
        private static NLog.Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
        
        public static Authentication.OTAuthentication auth(string login, string password)
        {
            Authentication.AuthenticationClient authClient = new Authentication.AuthenticationClient();
            Authentication.OTAuthentication otAuth = new Authentication.OTAuthentication();
            try
            {
                string authToken = authClient.AuthenticateUser(login, password);
                otAuth.AuthenticationToken = authToken;
            }
            catch (Exception e)
            {
                log.Error("Exception while proceeding authenticatiog for user: "+login+".",e);                
            }
            finally
            {
                authClient.Close();
            }
            return otAuth;
        }

        public static string makeOTCSRequest(string authToken, string requestUrl)
        {
            Stream receiveStream = null;
            try
            {
                HttpWebRequest requestLL = (HttpWebRequest)WebRequest.Create(requestUrl);
                requestLL.Method = "GET";
                requestLL.Headers.Add("Cookie", "LLCookie=" + authToken + ", LLTZCookie=0");

                HttpWebResponse responseLL = null;
                responseLL = (HttpWebResponse)requestLL.GetResponse();
                receiveStream = responseLL.GetResponseStream();
                Encoding encode = System.Text.Encoding.GetEncoding("utf-8");

                StreamReader readStream = new StreamReader(receiveStream, encode);
                String responseText = readStream.ReadToEnd();
                return responseText;
            }
            catch (Exception e)
            {
                log.Error("Exception while proceeding OTCS request.", e);
                return null;
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

        public static bool getVersionContent(Authentication.OTAuthentication otAuth, Record record)
        {
            DocumentManagement.DocumentManagementClient docManClient = new DocumentManagement.DocumentManagementClient();
            DocumentManagement.OTAuthentication docManOTAuth = new DocumentManagement.OTAuthentication();
            docManOTAuth.AuthenticationToken = otAuth.AuthenticationToken;

            byte[] content;
            DocumentManagement.Attachment attachment = null;
            try
            {
                attachment = docManClient.GetVersionContents(ref docManOTAuth, record.objectId, record.versionNum);

                if (attachment != null)
                {
                    content = attachment.Contents;
                    record.content = content;
                    record.fileName = attachment.FileName;
                    return true;
                }
                else
                {
                    //TODO: Add error record to log add counter incrementation into table and getVersionContent method fail                     
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                //TODO: Add error record to log add counter incrementation into table and getVersionContent method fail                
            }
            return false;

        }

        internal static void sendFilesToAbbyyRS(Settings programSettings, List<Record> listToProceed)
        {            
            String abbyyRSServicesUrl = programSettings.abbyyRSServicesUrl;

            if (String.IsNullOrEmpty(abbyyRSServicesUrl))
            {
                //TODO: Add error record to log
                return;
            }

            

        }

        internal static byte[] recognizeFile(Settings programSettings, Record record)
        {
            String abbyyRSServicesUrl = programSettings.abbyyRSServicesUrl;

            if (String.IsNullOrEmpty(abbyyRSServicesUrl))
            {
                //TODO: Add error record to log
                return null;
            }

          


            return null;
        }



        public static string[] getFromMetadataLangArray(DocumentManagement.MetadataLanguage[] langs)
        {
            List<string> list = new List<string>();

            foreach (DocumentManagement.MetadataLanguage l in langs)
            {
                list.Add(l.LanguageCode);
            }
            return list.ToArray();
        }

        public static string getRecognizedFileDescription(string p)
        {
            switch (p)
            {
                case "ru_RU": return "Содержимое версии получено из сервиса распознавания ABBYY";
                case "en_US": return "Content received from ABBY Recognition Service";
                default: return "Content received from ABBY Recognition Service";
            }

        }

        public static string replaceFileExtension(string fileName, string newExtension)
        {
            string extension = Path.GetExtension(fileName);
            fileName = fileName.Replace(extension, newExtension);
            return fileName;
        }

        public static ContentService.FileAtts createFileAtts(Record record)
        {
            ContentService.FileAtts fileAtts = new ContentService.FileAtts();
            fileAtts.CreatedDate = DateTime.Now;
            fileAtts.FileName = Utils.replaceFileExtension(record.fileName, ".pdf");
            fileAtts.FileSize = record.content.Length;
            fileAtts.ModifiedDate = DateTime.Now;

            return fileAtts;
        }

        public static ContentService.FileAtts createFileAtts(Record record, int length)
        {
            ContentService.FileAtts fileAtts = new ContentService.FileAtts();
            fileAtts.CreatedDate = DateTime.Now;
            fileAtts.FileName = Utils.replaceFileExtension(record.fileName, ".pdf");
            fileAtts.FileSize = length;
            fileAtts.ModifiedDate = DateTime.Now;

            return fileAtts;
        }

        internal static void logWarning(string p)
        {
            throw new NotImplementedException();
        }

        internal static void logError(string p)
        {
            throw new NotImplementedException();
        }

                     

        public static void saveToFileSystem(Record record)
        {
            string tmpFilePath = @"G:\Temp\1\{0}";
            string filepath = String.Format(tmpFilePath, Utils.replaceFileExtension(record.fileName, ".pdf"));
            FileStream stream = new FileStream(filepath, FileMode.Create);
            try
            {

                byte[] buffer = record.recognizedContent;
                stream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                //TODO: Add log error 
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        public static void saveToFileSystem(byte[] content, string filename)
        {
            string tmpFilePath = @"G:\Temp\1\{0}";
            string filepath = String.Format(tmpFilePath, filename);
            FileStream stream = new FileStream(filepath, FileMode.Create);
            try
            {
                byte[] buffer = content;
                stream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        internal static string makeUnAuthenticatedOTCSRequest(string rhUrl)
        {
            throw new NotImplementedException();
        }

        internal static void logError(string p, Exception ex)
        {
            throw new NotImplementedException();
        }

        internal static void logInfo(string p)
        {
            throw new NotImplementedException();
        }

        internal static string decryptPass(string encryptedPass)
        {
            return encryptedPass;
        }
    }
}
