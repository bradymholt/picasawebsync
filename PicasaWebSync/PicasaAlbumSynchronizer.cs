using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Drawing.Imaging;
using Google.GData.Client;
using Google.GData.Photos;
using Google.Picasa;
using System.Net;
using System.Xml;
using System.Net.Sockets;
using NLog;

namespace PicasaWebSync
{
    /// <summary>
    /// A class which interacts with Picasa to provide syncing between local folders and online Picasa Web Albums.
    /// </summary>
    public class PicasaAlbumSynchronizer
    {
        private static Logger s_logger = LogManager.GetLogger("*");

        #region Constructor
        public PicasaAlbumSynchronizer()
        {
            this.ResizePhotos = true;
            this.ResizePhotosMaxSize = DEFAULT_RESIZE_MAX_SIZE;
            this.AlbumAccess = AlbumAccessEnum.Private;
            this.AlbumNameFormat = DEFAULT_ALBUM_NAME_FORMAT;
            this.ExcludeFilesLargerThan = DEFAULT_EXCLUDE_FILES_LARGER_THAN;
        }
        #endregion

        #region Members
        private const string APP_NAME = "PicasaWebSync";
        private const int DEFAULT_RESIZE_MAX_SIZE = 800;
        private const string DEFAULT_ALBUM_NAME_FORMAT = "{0}";
        private const string ENTRY_DELETED_SUMMARY = "[deleted]";
        private const string GDATA_RESPONSE_BADAUTH = "BadAuthentication";
        private const long DEFAULT_EXCLUDE_FILES_LARGER_THAN = 1073741824; //1GB

        private string m_baseDirectoryName;
        private int m_albumCreateCount;
        private int m_albumUpdateCount;
        private int m_albumDeleteCount;
        private int m_folderSkipCount;
        private int m_fileCreateCount;
        private int m_fileDeleteCount;
        private int m_fileSkipCount;
        private int m_errorsCount;
        private DateTime m_startTime;

        public string PicasaUsername { get; set; }
        public string PicasaPassword { get; set; }
        public AlbumAccessEnum AlbumAccess { get; set; }
        public string[] IncludeExtensions { get; set; }
        public string[] ExcludeFileNamesContainingText { get; set; }
        public long ExcludeFilesLargerThan { get; set; }
        public string[] ExcludeFolderNames { get; set; }
        public string ExcludeFoldersContainingFileName { get; set; }
        public bool IncludeSubFolders { get; set; }
        public bool ResizePhotos { get; set; }
        public int ResizePhotosMaxSize { get; set; }
        public bool ResizeVideos { get; set; }
        public string ResizeVideosCommand { get; set; }
        public bool ClearAlbumPhotosFirst { get; set; }
        public string AlbumNameFormat { get; set; }
        public bool IncludeBaseDirectoryInAlbumName { get; set; }
        public string AlbumPublicFileName { get; set; }
        public string[] AlbumPrivateFolderNames { get; set; }
        public string AlbumPrivateFileName { get; set; }
        public bool VerboseOutput { get; set; }
        public bool AddOnly { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Syncs a local folder to an online Picasa Web Album location
        /// </summary>
        /// <param name="folderPath">The source folder to sync.</param>
        public void SyncFolder(string folderPath)
        {
            ResetCounters();
            PicasaService session = new PicasaService("PicasaWebSync");
            session.setUserCredentials(this.PicasaUsername, this.PicasaPassword);
            AlbumQuery query = new AlbumQuery(PicasaQuery.CreatePicasaUri(this.PicasaUsername));

            try
            {
                WriteOutput("[Connecting to Picasa web service]");
                //fetch all albums
                PicasaFeed albumFeed = session.Query(query);

                WriteOutput("[Starting sync]");

                folderPath = folderPath.TrimEnd(new char[] { '\\' });
                DirectoryInfo sourceFolder = new DirectoryInfo(folderPath);
                m_baseDirectoryName = sourceFolder.Name;

                SyncFolder(sourceFolder, null, this.AlbumAccess, this.IncludeSubFolders, session, albumFeed);
            }
            catch (GDataRequestException gdrex)
            {
                if (gdrex.ResponseString.Contains(GDATA_RESPONSE_BADAUTH))
                {
                    throw new AuthenticationException("Picasa error - username and/or password is incorrect!  Check the config file values.");
                }
                else
                {
                    throw new Exception(string.Format("Picasa error - {0}", gdrex.ResponseString));
                }
            }

            WriteOutput("[Done]");
            WriteOutput(string.Empty);
            WriteOutput("Summary:");
            WriteOutput(string.Format("  Folders skipped: {0}", m_folderSkipCount.ToString()));
            WriteOutput(string.Format("   Albums created: {0}", m_albumCreateCount.ToString()));
            WriteOutput(string.Format("   Albums updated: {0}", m_albumUpdateCount.ToString()));
            WriteOutput(string.Format("   Albums deleted: {0}", m_albumDeleteCount.ToString()));
            WriteOutput(string.Format("   Files uploaded: {0}", m_fileCreateCount.ToString()));
            WriteOutput(string.Format("    Files removed: {0}", m_fileDeleteCount.ToString()));
            WriteOutput(string.Format("    Files skipped: {0}", m_fileSkipCount.ToString()));
            WriteOutput(string.Format("   Errors occured: {0}", m_errorsCount.ToString()));
            WriteOutput(string.Format("     Elapsed time: {0}", (DateTime.Now - m_startTime).ToString()));
            WriteOutput(string.Empty);
        }

        /// <summary>
        /// Syncs a local folder to an online Picasa Web Album location
        /// </summary>
        /// <param name="sourceFolder">The source folder to sync.</param>
        /// <param name="albumNamePrefix">A prefix to prepend to the album name.</param>
        /// <param name="includeSubFolders">Indicated whether to sync all subfolders recursively.</param>
        /// <param name="session">The current authenticated Picasa session.</param>
        /// <param name="albumFeed">A feed listing all existing Picass Web Albums in the Picasa account.</param>
        private void SyncFolder(DirectoryInfo sourceFolder, string albumNamePrefix, AlbumAccessEnum targetAlbumAccess, bool includeSubFolders, PicasaService session, PicasaFeed albumFeed)
        {
            if (!sourceFolder.Exists)
            {
                throw new DirectoryNotFoundException(string.Format("Folder '{0}' cannot be located.", sourceFolder.FullName));
            }

            string targetAlbumName = GetTargetAlbumName(sourceFolder, albumNamePrefix);
            Dictionary<string, FileInfo> sourceFiles = GetSourceFiles(sourceFolder);
            bool excludeFolder = ShouldExcludeFolder(sourceFolder, sourceFiles);
            PicasaEntry existingAlbumEntry = albumFeed.Entries.FirstOrDefault(a => a.Title.Text == targetAlbumName) as PicasaEntry;

            if (excludeFolder)
            {
                if (existingAlbumEntry != null && !this.AddOnly)
                {
                    //album needs to be removed
                    WriteOutput(string.Format("Removing Album: {0} (excluded but already exists)", targetAlbumName));
                    existingAlbumEntry.Delete();
                    existingAlbumEntry.Summary.Text = ENTRY_DELETED_SUMMARY;
                    m_albumDeleteCount++;
                }
                else
                {
                    m_folderSkipCount++;
                }
            }
            else
            {
                WriteOutput(string.Format("Syncing Folder: {0} to Album: {1}", sourceFolder.FullName, targetAlbumName), true);

                try
                {
                    targetAlbumAccess = DetermineAlbumAccess(sourceFolder, targetAlbumAccess);

                    if (sourceFiles.Count > 0)
                    {
                        Album targetAlbum = new Album();
                        if (existingAlbumEntry != null)
                        {
                            targetAlbum.AtomEntry = existingAlbumEntry;
                            if (targetAlbum.Access != targetAlbumAccess.ToString().ToLower())
                            {
                                UpdateAlbumAccess(session, targetAlbum, targetAlbumAccess);
                            }
                        }
                        else
                        {
                            targetAlbum = CreateAlbum(session, albumFeed, targetAlbumName, targetAlbumAccess);
                        }

                        PhotoQuery existingAlbumPhotosQuery = new PhotoQuery(PicasaQuery.CreatePicasaUri(this.PicasaUsername, targetAlbum.Id));
                        PicasaFeed existingAlbumPhotosFeed = session.Query(existingAlbumPhotosQuery);

                        //sync folder files
                        DeleteFilesFromAlbum(sourceFiles, existingAlbumPhotosFeed);

                        foreach (KeyValuePair<string, FileInfo> file in sourceFiles.OrderBy(f => f.Value.LastWriteTime))
                        {
                            AddFileToAlbum(file.Value, targetAlbum, existingAlbumPhotosFeed, session);
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteOutput(string.Format("Skipping Folder: {0} (Error - {1})", sourceFolder.Name, ex.Message), true);
                }

                if (includeSubFolders)
                {
                    DirectoryInfo[] subfolders = sourceFolder.GetDirectories().OrderBy(d => d.CreationTime).ToArray();
                    if (subfolders.Length > 0)
                    {
                        foreach (DirectoryInfo folder in subfolders)
                        {
                            SyncFolder(folder, targetAlbumName, targetAlbumAccess, includeSubFolders, session, albumFeed);
                        }
                    }
                }
            }
        }

        private bool ShouldExcludeFolder(DirectoryInfo sourceFolder, Dictionary<string, FileInfo> sourceFiles)
        {
            bool excludeFolder = false;

            if (this.ExcludeFolderNames.Any(e => sourceFolder.Name.StartsWith(e)))
            {
                WriteOutput(string.Format("Skipping Folder: {0} (excluded by name)", sourceFolder.FullName), true);
                excludeFolder = true;
            }
            else if (!string.IsNullOrEmpty(this.ExcludeFoldersContainingFileName) && File.Exists(Path.Combine(sourceFolder.FullName, this.ExcludeFoldersContainingFileName)))
            {
                WriteOutput(string.Format("Skipping Folder: {0} (exclude file '{1}' was found in folder)", sourceFolder.FullName, this.ExcludeFoldersContainingFileName), true);
                excludeFolder = true;
            }

            return excludeFolder;
        }

        private string GetTargetAlbumName(DirectoryInfo sourceFolder, string albumNamePrefix)
        {
            string targetAlbumName = string.Format(this.AlbumNameFormat, sourceFolder.Name);
            if (!string.IsNullOrEmpty(albumNamePrefix)
                && (this.IncludeBaseDirectoryInAlbumName || (albumNamePrefix != m_baseDirectoryName)))
            {
                targetAlbumName = string.Concat(albumNamePrefix, " - ", targetAlbumName);
            }
            return targetAlbumName;
        }

        private void DeleteFilesFromAlbum(Dictionary<string, FileInfo> sourceFiles, PicasaFeed existingAlbumPhotosFeed)
        {
            foreach (PicasaEntry albumPhotoEntry in existingAlbumPhotosFeed.Entries)
            {
                bool deleteAlbumPhoto = false;

                if (!this.AddOnly && this.ClearAlbumPhotosFirst)
                {
                    WriteOutput(string.Format("Deleting Album File: {0} (-emptyAlbumFirst option specified)", albumPhotoEntry.Title.Text), true);
                    deleteAlbumPhoto = true;
                }
                else if (!this.AddOnly && !sourceFiles.ContainsKey(albumPhotoEntry.Title.Text))
                {
                    WriteOutput(string.Format("Deleting Album File: {0} (does not exist in source folder)", albumPhotoEntry.Title.Text), true);
                    deleteAlbumPhoto = true;
                }
                else if (!this.AddOnly && sourceFiles[albumPhotoEntry.Title.Text].LastWriteTime > albumPhotoEntry.Published)
                {
                    WriteOutput(string.Format("Deleting Album File: {0} (updated since last upload)", albumPhotoEntry.Title.Text), true);
                    deleteAlbumPhoto = true;
                }

                if (deleteAlbumPhoto)
                {
                    albumPhotoEntry.Delete();
                    albumPhotoEntry.Summary.Text = ENTRY_DELETED_SUMMARY;
                    m_fileDeleteCount++;
                }
            }
        }

        private void UpdateAlbumAccess(PicasaService session, Album albumToUpdate, AlbumAccessEnum albumAccess)
        {
            //update existing album
            WriteOutput(string.Concat("Updating Album: ", albumToUpdate.AtomEntry.Title.Text), true);
            albumToUpdate.Updated = DateTime.Now;
            albumToUpdate.Access = albumAccess.ToString().ToLower();

            albumToUpdate.AtomEntry = (PicasaEntry)albumToUpdate.AtomEntry.Update();
            m_albumUpdateCount++;
        }

        private Album CreateAlbum(PicasaService session, PicasaFeed albumFeed, string targetAlbumName, AlbumAccessEnum albumAccess)
        {
            //create album (doesn't exist)
            WriteOutput(string.Concat("Creating Album: ", targetAlbumName), true);
            AlbumEntry newAlbumEntry = new AlbumEntry();
            newAlbumEntry.Title.Text = targetAlbumName;
            newAlbumEntry.Summary.Text = targetAlbumName;
            newAlbumEntry.Published = DateTime.Now;

            Album newAlbum = new Album();
            newAlbum.AtomEntry = newAlbumEntry;
            newAlbum.Access = albumAccess.ToString().ToLower();

            Uri insertAlbumFeedUri = new Uri(PicasaQuery.CreatePicasaUri(this.PicasaUsername));
            newAlbum.AtomEntry = (PicasaEntry)session.Insert(insertAlbumFeedUri, newAlbumEntry);
            m_albumCreateCount++;

            return newAlbum;
        }

        private Dictionary<string, FileInfo> GetSourceFiles(DirectoryInfo sourceFolder)
        {
            Dictionary<string, FileInfo> sourceFiles = new Dictionary<string, FileInfo>();
            foreach (string extension in this.IncludeExtensions)
            {
                foreach (FileInfo file in sourceFolder.GetFiles(string.Format("*.{0}*", extension)))
                {
                    bool addFile = true;

                    if (this.ExcludeFileNamesContainingText != null)
                    {
                        foreach (string excludeText in this.ExcludeFileNamesContainingText)
                        {
                            if (file.Name.Contains(excludeText))
                            {
                                WriteOutput(string.Format("Excluding File: {0} (exclude text '{1}' found in name)", file.Name, excludeText), true);
                                m_fileSkipCount++;
                                addFile = false;
                                break;
                            }
                        }
                    }

                    if (file.Length > this.ExcludeFilesLargerThan)
                    {
                        WriteOutput(string.Format("Excluding File: {0} (size larger than {1} bytes)", file.Name, this.ExcludeFilesLargerThan.ToString()), true);
                        m_fileSkipCount++;
                        addFile = false;
                    }

                    if (addFile)
                    {
                        sourceFiles[file.Name] = file;
                    }
                }
            }

            return sourceFiles;
        }

        private AlbumAccessEnum DetermineAlbumAccess(DirectoryInfo sourceFolder, AlbumAccessEnum parentFolderAccess)
        {
            AlbumAccessEnum albumAccess = parentFolderAccess;

            if (this.AlbumPrivateFolderNames != null && this.AlbumPrivateFolderNames.Contains(sourceFolder.Name))
            {
                WriteOutput(string.Format("Album Marked Private (folder named '{0}')", sourceFolder.Name), true);
                albumAccess = AlbumAccessEnum.Private;
            }
            else if (!string.IsNullOrEmpty(this.AlbumPrivateFileName) && File.Exists(Path.Combine(sourceFolder.FullName, this.AlbumPrivateFileName)))
            {
                WriteOutput(string.Format("Album Marked Private ('{0}' file was found in folder)", this.AlbumPrivateFileName), true);
                albumAccess = AlbumAccessEnum.Private;
            }
            else if (!string.IsNullOrEmpty(this.AlbumPublicFileName) && File.Exists(Path.Combine(sourceFolder.FullName, this.AlbumPublicFileName)))
            {
                WriteOutput(string.Format("Album Marked Public ('{0}' file was found in folder)", this.AlbumPublicFileName), true);
                albumAccess = AlbumAccessEnum.Public;
            }
            return albumAccess;
        }

        /// <summary>
        /// Syncs a local file to an online Picasa Web Album location
        /// </summary>
        /// <param name="sourceFile">The image to sync.</param>
        /// <param name="targetAlbum">The target Picasa Web Album.</param>
        /// <param name="targetAlbumPhotoFeed">The target Picasa Web Album photo feed listing existing photos.</param>
        /// <param name="session">The current authenticated Picasa session.</param>
        private void AddFileToAlbum(FileInfo sourceFile, Album targetAlbum, PicasaFeed targetAlbumPhotoFeed, PicasaService session)
        {
            try
            {
                PicasaEntry existingPhotoEntry = (PicasaEntry)targetAlbumPhotoFeed.Entries.FirstOrDefault(
                    p => p.Title.Text == sourceFile.Name && p.Summary.Text != ENTRY_DELETED_SUMMARY);

                if (existingPhotoEntry != null)
                {
                    WriteOutput(string.Format("Skipping File: {0} (already exists)", sourceFile.Name), true);
                    m_fileSkipCount++;
                }
                else
                {
                    ImageFormat imageFormat;
                    string contentType;
                    bool isImage;
                    bool isVideo;
                    GetFileInfo(sourceFile, out isImage, out isVideo, out imageFormat, out contentType);

                    Stream postResponseStream = null;
                    Stream postFileStream = null;
                    string souceFilePath = sourceFile.FullName;

                    try
                    {
                        if (isVideo && this.ResizeVideos)
                        {
                            WriteOutput(string.Concat("Resizing Video: ", sourceFile.FullName), true);
                            string resizedVideoPath = VideoResizer.ResizeVideo(sourceFile, this.ResizeVideosCommand);
                            //change souceFilePath to resized video file location
                            souceFilePath = resizedVideoPath;
                        }

                        using (Stream sourceFileStream = new FileStream(souceFilePath, FileMode.Open, FileAccess.Read))
                        {
                            postFileStream = sourceFileStream;

                            if (isImage && this.ResizePhotos)
                            {
                                WriteOutput(string.Concat("Resizing Photo: ", sourceFile.FullName), true);
                                postFileStream = ImageResizer.ResizeImage(postFileStream, imageFormat, this.ResizePhotosMaxSize);
                            }

                            WriteOutput(string.Format("Uploading File: {0}", sourceFile.FullName), true);
                            Uri insertPhotoFeedUri = new Uri(PicasaQuery.CreatePicasaUri(this.PicasaUsername, targetAlbum.Id));

                            PhotoEntry newFileEntry = new PhotoEntry();
                            newFileEntry.Title.Text = sourceFile.Name;
                            newFileEntry.Summary.Text = string.Empty;
                            newFileEntry.MediaSource = new MediaFileSource(postFileStream, sourceFile.Name, contentType);

                            //upload file using multipart request
                            postResponseStream = session.EntrySend(insertPhotoFeedUri, newFileEntry, GDataRequestType.Insert);
                            newFileEntry.MediaSource.GetDataStream().Dispose();
                        }

                        m_fileCreateCount++;

                    }
                    finally
                    {
                        if (postResponseStream != null)
                        {
                            postResponseStream.Dispose();
                        }

                        if (postFileStream != null)
                        {
                            postFileStream.Dispose();
                        }
                    }

                    if (isVideo && this.ResizeVideos)
                    {
                        //video was resized and souceFilePath should be temp/resized video path
                        try
                        {
                            File.Delete(souceFilePath);
                        }
                        catch (Exception ex)
                        {
                            WriteOutput(string.Format("Error Deleting Resized Video: {0} (Error - {1})", sourceFile.FullName, ex.Message), true);
                        }
                    }
                }
            }
            catch (GDataRequestException gdex)
            {
                WriteOutput(string.Format("Skipping File: {0} (Error - {1})", sourceFile.Name, gdex.ResponseString), true);
                m_errorsCount++;
            }
            catch (Exception ex)
            {
                WriteOutput(string.Format("Skipping File: {0} (Error - {1})", sourceFile.Name, ex), true);
                m_errorsCount++;
            }
        }

        /// <summary>
        /// Returns info about a media file based upon the file extension.
        /// </summary>
        /// <param name="file">The media file.</param>
        /// /// <param name="isImage">Indicates of file is an image.</param>
        /// <param name="imageFormat">GDI Image format (Out)</param>
        /// <param name="imageContentType">MIME content type (Out)</param>
        public static void GetFileInfo(FileInfo file, out bool isImage, out bool isVideo, out ImageFormat imageFormat, out string contentType)
        {
            imageFormat = null;
            contentType = null;
            isImage = true;
            isVideo = false;

            string extension = file.Extension.ToLower();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg": imageFormat = ImageFormat.Jpeg; contentType = "image/jpeg"; break;
                case ".gif": imageFormat = ImageFormat.Gif; contentType = "image/gif"; break;
                case ".tiff":
                case ".tif": imageFormat = ImageFormat.Tiff; contentType = "image/tiff"; break;
                case ".png": imageFormat = ImageFormat.Png; contentType = "image/png"; break;
                case ".bmp": imageFormat = ImageFormat.Bmp; contentType = "image/bmp"; break;
                case ".avi": contentType = "video/x-msvideo"; break;
                case ".wmv": contentType = "video/x-ms-wmv"; break;
                case ".mpg": contentType = "video/mpeg"; break;
                case ".asf": contentType = "video/x-ms-asf"; break;
                case ".mov": contentType = "video/quicktime"; break;
                case ".mp4": contentType = "video/mp4"; break;
                default:
                    throw new InvalidDataException(string.Format("Extension '(0}' was not handled.", extension));
            }

            isImage = (imageFormat != null);
            isVideo = !isImage;
        }

        private void ResetCounters()
        {
            m_albumCreateCount = 0;
            m_albumUpdateCount = 0;
            m_albumDeleteCount = 0;
            m_fileCreateCount = 0;
            m_fileDeleteCount = 0;
            m_startTime = DateTime.Now;
            m_errorsCount = 0;
        }

        private void WriteOutput(string message)
        {
            WriteOutput(message, false);
        }

        private void WriteOutput(string message, bool verboseOnly)
        {
            if (!verboseOnly || this.VerboseOutput)
            {
                s_logger.Info(message);
            }
            else
            {
                s_logger.Info(".");
            }
        }
        #endregion
    }
}
