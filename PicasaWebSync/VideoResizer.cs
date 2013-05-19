using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace PicasaWebSync
{
    public static class VideoResizer
    {
        public static string ResizeVideo(FileInfo sourceVideoFile, string resizeCommand)
        {
            string resizedOutputVideoPath = sourceVideoFile.FullName.Replace(sourceVideoFile.Name, "picasawebsync_resized_" + Guid.NewGuid().ToByteArray() + "_" + sourceVideoFile.Name);

            string resizeCommandFull = string.Format(resizeCommand,
                string.Concat("\"", sourceVideoFile.FullName, "\""),
                string.Concat("\"", resizedOutputVideoPath, "\""));
            string commandFileName = resizeCommandFull.Substring(0, resizeCommandFull.IndexOf(" "));
            string commandArguments = resizeCommandFull.Substring(resizeCommandFull.IndexOf(" ") + 1);

            ProcessStartInfo resizeProcessStartInfo = new ProcessStartInfo(commandFileName, commandArguments);
            resizeProcessStartInfo.RedirectStandardOutput = true;
            resizeProcessStartInfo.UseShellExecute = false;

            Process resizeProcess = new Process();
            resizeProcess.StartInfo = resizeProcessStartInfo;
            resizeProcess.Start();
            resizeProcess.WaitForExit();

            string commandOutput = resizeProcess.StandardOutput.ReadToEnd();

            if (!File.Exists(resizedOutputVideoPath) || new FileInfo(resizedOutputVideoPath).Length == 0)
            {
                throw new Exception(string.Concat("Error Resizing Video - ", resizeProcess.StandardOutput.ReadToEnd()));
            }

            return resizedOutputVideoPath;
        }
    }
}
