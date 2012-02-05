# PicasaWebSync
A command line tool to resize and upload pictures and videos into Picasa Web Albums.  
Author: Brady Holt (http://www.GeekyTidBits.com)

Installation and Usage
---

To install and use picasawebsync:

1. Obtain a build.  Either download the source of this repository and build the solution (src/PicasaWebSync.sln) or download the latest release from [Downloads](https://github.com/bradyholt/PicasaWebSync/downloads) page.
2. Place the build output into a dedicated directory. 
3. Modify the picasawebsync.exe.config file and update the values for the picasa.username and picasa.password settings to match your Google username and Password.
4. Run the main executable **picasawebsync.exe** with appropriate command line paremters and options.  To see a list of available command line options run 'picasawebsync.exe -help'.
	   
	   Options:
		   -r,                 recursive (include subfolders)
		   -emptyAlbumFirst    delete all images in album before adding photos
		   -addOnly            add only and do not remove anything from online albums (overrides -emptyAlbumFirst)");
		   -v,                 verbose output
		   -help               print help menu

    Example usage:  
    picasawebsync.exe "C:\Users\Public\Pictures\My Pictures\" -r -v


Repository Directories
---

- **bin** - Contains main executable 'picasawebsync.exe' along with other required runtime files/assemblies.
- **src** - The source solution/project for picasawebsync.
- **lib** - Contains Google Data API libraries used by picasawebsync.  The files in this folder are referenced by the source project file in /src.  This folder can be generally ignored unless you are building the source.

Hint Files
---
Hint files can be placed in local folders which will instruct PicasaWebSync to mark an album with Public / Private access or exclude
the folder entirely from being uploaded.  Refer to the /hint_files for sample files.  The hint files do not have to have any contents.

Requirements
---
Microsoft .NET Framework 3.5.
