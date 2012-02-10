# PicasaWebSync
A command line tool to resize and upload pictures and videos into Picasa Web Albums.  
Author: Brady Holt (http://www.GeekyTidBits.com)  
License: The MIT License (MIT) (http://www.opensource.org/licenses/mit-license.php)

Overview
---
PicasaWebSync is a command-line tool to synchronize local photos and videos to online Picasa Web Albums. It is flexible with a configuration file / run-time options and optionally resizes* photos before uploading them.

**Features**

- Resizes photos before uploading.
- Resizes videos before uploading (requires external tool like ffmpeg).
- Allows folders to be excluded by name or hint file included in folder.
- Allows album access (i.e. Public / Private) to be set by hint files in source folders.
- Allows excluding files over a specified size.
- Removes photos/videos/albums from Picasa Web Albums that have been removed locally (can prevent this with -addOnly command line option)
- Updates files on Picasa which have been updated locally since the last time they were uploaded.
- Supports these file types: jpg, jpeg, gif, tiff, tif, png, bmp, avi, wmv, mpg, asf, mov, mp4

Installation and Usage
---

To install and use picasawebsync:

1. Obtain a build.  Either download the source of this repository and build the solution (src/PicasaWebSync.sln) or download the latest release from [Downloads](https://github.com/bradyholt/PicasaWebSync/downloads) page.
2. Place the build output into a dedicated directory. 
3. Modify the picasawebsync.exe.config file and update the values for the picasa.username and picasa.password settings to match your Google username and Password.
4. Run the main executable **picasawebsync.exe** with appropriate command line paremters and options.  To see a list of available command line options run 'picasawebsync.exe -help'.

**Usage** 
picasawebsync.exe folderPath [options]
                                               
**Options**

		-u:USERNAME,        Picasa Username (can also be specified in picasawebsync.exe.config)
		-p:PASSWORD,        Picasa Password (can also be specified in picasawebsync.exe.config)
		-r,                 recursive (include subfolders)
		-emptyAlbumFirst    delete all images in album before adding photos
		-addOnly            add only and do not remove anything from online albums (overrides -emptyAlbumFirst)");
		-v,                 verbose output
		-help               print help menu
	
**Example Usage**

		picasawebsync.exe "C:\Users\Public\Pictures\My Pictures\" -u:john.doe@gmail.com -p:Secr@tPwd -r -v


Repository Directories
---

- **src** - The source solution/project for picasawebsync.
- **lib** - Contains Google Data API libraries used by picasawebsync.  The files in this folder are referenced by the source project file in /src.  This folder can be generally ignored unless you are building the source.

Video Resizing
---
By default, video resizing is disabled (key="video.resize" value="false" in picasawebsync.exe.config) but to enable it you should 
install a command-line utility that can handle video resizing and specify the command-line in the video.resize.command config value in
picasawebsync.exe.config.  I highly recommend installing and utilizing FFmpeg (http://www.ffmpeg.org/) for video resizing.  Make sure the 
video.resize.command setting contains the path to the ffmpeg executable or your PATH environment include the directory where this executable
resizes so that PicasaWebSync can locate it at runtime.

Hint Files
---
Hint files can be placed in local folders which will instruct PicasaWebSync to mark an album with Public / Private access or exclude
the folder entirely from being uploaded.  Refer to the /hint_files for sample files.  The hint files do not have to have any contents.

Requirements
---
**Windows**  
.NET Framework 3.5 (Windows 7, Windows Vista, Windows Server 2008, Windows XP, Windows Server 2003)

**Linux**  
Mono version supporting .NET Framework 3.5
