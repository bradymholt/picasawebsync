# PicasaWebSync
Description: A command line tool to resize and upload pictures and videos into Picasa Web Albums.
Author: Brady Holt (http://www.GeekyTidBits.com)
Version: 1.1
Release Date: July 5th, 2011
-----------------------------------------

Installation and Usage
----------------------

To install and use picasawebsync:

    1. Extract the contents of the /bin folder into a directory 
	2. Modify the picasawebsync.exe.config file and update the values for the picasa.username and picasa.password
	   settings to match your Google username and Password.
	3. Run the main executable 'picasawebsync.exe' with command line paremters and options.  
	   To see a list of available command line options run 'picasawebsync.exe -help'.

           Example usage:

           picasawebsync.exe "C:\Users\Public\Pictures\My Pictures\" -r -v


Note: PicasaWebSync Requires Microsoft .NET Framework 3.5


Package Directories:
-------------------

bin:
    Contains main executable 'picasawebsync.exe' along with other required runtime files/assemblies.


src:
    The source solution/project for picasawebsync.


lib:
    Contains Google Data API libraries used by picasawebsync.  The files in this folder are referenced by the 
    source project file in /src.  This folder can be generally ignored unless you are building the source.


hint_files:
    Contains files which picasawebsync uses to override configuration at runtime.  Refer to the configuration file (picasawebsync.exe.config)
    for more information.


Hint Files:
-------------------
Hint files can be placed in local folders which will instruct PicasaWebSync to mark an album with Public / Private access or exclude
the folder entirely from being uploaded.  Refer to the /hint_files for sample files.  The hint files do not have to have any contents.


Requirements:
-------------------
Microsoft .NET Framework 3.5.
