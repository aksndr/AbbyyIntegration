using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;

using Authentication = WindowsService.Authentication;
using ContentService = WindowsService.ContentService;
using DocumentManagement = WindowsService.DocumentManagement;
using WindowsService.Models;

using NLog;


namespace WindowsService.Common
{
    public class Utils       
    {       
        //private static Settings settings;
        private static NLog.Logger log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
        //private static Authentication.OTAuthentication otAuth; 
                     

        //public static Authentication.OTAuthentication auth(string login, string password)
        //{
        //    Authentication.AuthenticationClient authClient = new Authentication.AuthenticationClient();            
        //    try
        //    {
        //        string authToken = authClient.AuthenticateUser(login, password);
        //        otAuth = new Authentication.OTAuthentication();
        //        otAuth.AuthenticationToken = authToken;
        //    }
        //    catch (Exception e)
        //    {
        //        log.Error("Exception while proceeding authentication for user: "+login+".",e);                
        //    }
        //    finally
        //    {
        //        authClient.Close();
        //    }
        //    return otAuth;
        //}       

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

        //public static void saveToFileSystem(Record record)
        //{
        //    string tmpFilePath = @"G:\Temp\1\{0}";
        //    string filepath = String.Format(tmpFilePath, Utils.replaceFileExtension(record.fileName, ".pdf"));
        //    FileStream stream = new FileStream(filepath, FileMode.Create);
        //    try
        //    {

        //        byte[] buffer = record.recognizedContent;
        //        stream.Write(buffer, 0, buffer.Length);
        //    }
        //    catch (Exception e)
        //    {
        //        //TODO: Add log error 
        //        Console.WriteLine(e.Message);
        //    }
        //    finally
        //    {
        //        if (stream != null)
        //        {
        //            stream.Close();
        //        }
        //    }
        //}

        //public static void saveToFileSystem(byte[] content, string filename)
        //{
        //    string tmpFilePath = @"G:\Temp\1\{0}";
        //    string filepath = String.Format(tmpFilePath, filename);
        //    FileStream stream = new FileStream(filepath, FileMode.Create);
        //    try
        //    {
        //        byte[] buffer = content;
        //        stream.Write(buffer, 0, buffer.Length);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.Message);
        //    }
        //    finally
        //    {
        //        if (stream != null)
        //        {
        //            stream.Close();
        //        }
        //    }
        //}
        
        internal static string decryptPass(string encryptedPass)
        {
            return encryptedPass;
        }
    }
}
