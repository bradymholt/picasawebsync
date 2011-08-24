using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Google.GData.Photos;
using Google.GData.Client;
using Google.GData.Extensions;
using Google.GData.Extensions.Location;
using Google.Picasa;
using System.Configuration;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace PicasaWebSync
{
    class Program
    {
        static void Main(string[] args)
        {
            string commandLineArgs = string.Join(" ", args);

            if (args.Length == 0 || string.IsNullOrEmpty(args[0]) || commandLineArgs.Contains("-help"))
            {
                Console.WriteLine("PicasaWebSync: A utility to sync local folder photos to Picasa Web Albums.");
                Console.WriteLine("       Author: Brady Holt (http://www.GeekyTidBits.com)");
                Console.WriteLine();
                Console.WriteLine("Usage: picasawebsync.exe folderPath [options]");
                Console.WriteLine();
                Console.WriteLine("Example Usage:");
                Console.WriteLine("   picasawebsync.exe \"C:\\Users\\Public\\Pictures\\My Pictures\\\" -r -v");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("   -r,                 recursive (include subfolders)");
                Console.WriteLine("   -emptyAlbumFirst,   delete all images in album before adding photos");
                Console.WriteLine("   -addOnly,           add only and do not remove anything from online albums (overrides -emptyAlbumFirst)");
                Console.WriteLine("   -v,                 verbose output");
                Console.WriteLine("   -help,              print this help menu");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("[Initializing]");
                ServicePointManager.ServerCertificateValidationCallback = CertificateValidator;
                PicasaAlbumSynchronizer uploader = new PicasaAlbumSynchronizer();

                try
                {
                    string includeExtensionsConfig = ConfigurationManager.AppSettings["file.includeExtensions"];
                    string excludeFolderNamesConfig = ConfigurationManager.AppSettings["folder.excludeNames"];
                    string excludeFilesNamesContainingTextConfig = ConfigurationManager.AppSettings["file.excludeWhenFileNameContains"];
                    string privateAccessFolderNamesConfig = ConfigurationManager.AppSettings["album.privateAccess.folderNames"];

                    uploader.PicasaUsername = ConfigurationManager.AppSettings["picasa.username"];
                    uploader.PicasaPassword = ConfigurationManager.AppSettings["picasa.password"];
                    uploader.AlbumAccess = (AlbumAccessEnum)Enum.Parse(typeof(AlbumAccessEnum), ConfigurationManager.AppSettings["album.access.default"], true);
                    uploader.IncludeSubFolders = commandLineArgs.Contains("-r");
                    uploader.ClearAlbumPhotosFirst = commandLineArgs.Contains("-emptyAlbumFirst");
                    uploader.IncludeBaseDirectoryInAlbumName = Convert.ToBoolean(ConfigurationManager.AppSettings["album.includeTopDirectoryName"]);
                    uploader.IncludeExtensions = includeExtensionsConfig.Split(',');
                    uploader.ExcludeFileNamesContainingText = excludeFilesNamesContainingTextConfig.Split(',');
                    uploader.ExcludeFilesLargerThan = Convert.ToInt64(ConfigurationManager.AppSettings["file.excludeWhenSizeLargerThan"]);
                    uploader.ExcludeFolderNames = excludeFolderNamesConfig.Split(',');
                    uploader.ExcludeFoldersContainingFileName = ConfigurationManager.AppSettings["folder.exclude.hintFileName"];
                    uploader.ResizePhotos = Convert.ToBoolean(ConfigurationManager.AppSettings["photo.resize"]);
                    uploader.ResizePhotosMaxSize = Convert.ToInt32(ConfigurationManager.AppSettings["photo.resize.maxSize"]);
                    uploader.ResizeVideos = Convert.ToBoolean(ConfigurationManager.AppSettings["video.resize"]);
                    uploader.ResizeVideosCommand = ConfigurationManager.AppSettings["video.resize.command"];
                    uploader.AlbumNameFormat = ConfigurationManager.AppSettings["album.nameFormat"];
                    uploader.AlbumPrivateFileName = ConfigurationManager.AppSettings["album.privateAccess.hintFileName"];
                    if (privateAccessFolderNamesConfig != null && privateAccessFolderNamesConfig.Length > 0)
    					uploader.AlbumPrivateFolderNames = privateAccessFolderNamesConfig.Split(',');
                    uploader.AlbumPublicFileName = ConfigurationManager.AppSettings["album.publicAccess.hintFileName"];
                    uploader.AddOnly = commandLineArgs.Contains("-addOnly");
                    uploader.VerboseOutput = commandLineArgs.Contains("-v");

                    uploader.SyncFolder(args[0]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Fatal Error Occured: {0}", ex.Message));
                }
            }
        }

        public static bool CertificateValidator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //trust all certificates
            return true;
        }


    }
}
