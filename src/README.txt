PicasaWebSync
A command line tool to resize and upload pictures and videos into Picasa Web Albums.
Author: Brady Holt (http://www.GeekyTidBits.com)

Installation and Usage
---

To install and use picasawebsync:

    1. Extract the contents into a dedicated directory 
	2. Modify the picasawebsync.exe.config file and update the values for the picasa.username and picasa.password
	   settings to match your Google username and Password.
	3. Run the main executable 'picasawebsync.exe' with command line paremters and options.  
	   To see a list of all available command line options run 'picasawebsync.exe -help'.

	   Options:
		   -r,                 recursive (include subfolders)
		   -emptyAlbumFirst    delete all images in album before adding photos
		   -addOnly            add only and do not remove anything from online albums (overrides -emptyAlbumFirst)");
		   -v,                 verbose output
		   -help               print help menu

       Example usage:

           picasawebsync.exe "C:\Users\Public\Pictures\My Pictures\" -r -v

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
Microsoft .NET Framework 3.5.
