using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;

namespace PicasaWebSync
{
    public static class ImageResizer
    {
        /// <summary>
        /// Returns a stream containing a resized image with original aspect ratio.
        /// </summary>
        /// <param name="originalImageStream">The image to resize.</param>
        /// <param name="format">The image format.</param>
        /// <param name="maxSizePixels">The maximum allowed height or width in pixels.</param>
        /// <returns>A resized image.</returns>
        public static MemoryStream ResizeImage(Stream originalImageStream, ImageFormat format, int maxSizePixels)
        {
            Bitmap originalImage = null;
            Bitmap resizedImage = null;
            MemoryStream resizedImageStream = null;

            try
            {
                originalImage = new Bitmap(originalImageStream);
                resizedImage = ResizeImage(ref originalImage, maxSizePixels, maxSizePixels);

                resizedImageStream = new MemoryStream();
                resizedImage.Save(resizedImageStream, format);
            }
            finally
            {
                //explicit cleanup b/c we are using GDI+
                originalImage.Dispose();
                resizedImage.Dispose();
                originalImage = null;
                resizedImage = null;
            }

            //reset position so caller can read access from beginning of stream
            resizedImageStream.Position = 0;

            return resizedImageStream;
        }

        /// <summary>
        /// Returns a new bitmap resized to fix a maximum size 
        /// while still maintaining the correct aspect ratio.
        /// </summary>
        /// <param name="originalImage">The image to resize.</param>
        /// <param name="maxWidth">The maximum allowed width in pixels.</param>
        /// <param name="maxHeight">The maximum allowed height in pixels.</param>
        /// <returns>A resized image.</returns>
        public static Bitmap ResizeImage(ref Bitmap originalImage, int maxWidthPixels, int maxHeightPixels)
        {
            Bitmap resizedImage = null;

            //make sure resize is needed first
            if (originalImage.Width > maxWidthPixels || originalImage.Height > maxHeightPixels)
            {
                // properly constrain proportions of the image
                decimal imgRatio = (decimal)originalImage.Width / (decimal)originalImage.Height;
                decimal maxRatio = (decimal)maxWidthPixels / (decimal)maxHeightPixels;

                // first, try to scale image down to perfectly fit the max height and max width.
                if (imgRatio == maxRatio)
                {
                    resizedImage = new Bitmap(originalImage, maxWidthPixels, maxHeightPixels);
                }
                else if (imgRatio < maxRatio)
                {
                    // adjust the width to match the maximum height.
                    decimal newRatio = (decimal)maxHeightPixels / (decimal)originalImage.Height;
                    int newWidth = (int)decimal.Round(newRatio * originalImage.Width);

                    // if new image is a thumbnail (small) then use the GetThumbnailImage() method as it produces better thumbnail results.
                    if (maxWidthPixels <= 150 && maxHeightPixels <= 150)
                    {
                        resizedImage = (Bitmap)originalImage.GetThumbnailImage(newWidth, maxHeightPixels, null, new IntPtr());
                    }
                    else
                    {
                        resizedImage = new Bitmap(originalImage, newWidth, maxHeightPixels);
                    }
                }
                else
                {
                    // adjust the height to match the maximum width.
                    decimal newRatio = (decimal)maxWidthPixels / (decimal)originalImage.Width;
                    int newHeight = (int)decimal.Round(newRatio * originalImage.Height);

                    // if new image is a thumbnail (small) then use the GetThumbnailImage() method as it produces better thumbnail results.
                    if (maxWidthPixels <= 150 && maxHeightPixels <= 150)
                    {
                        resizedImage = (Bitmap)originalImage.GetThumbnailImage(maxWidthPixels, newHeight, null, new IntPtr());
                    }
                    else
                    {
                        resizedImage = new Bitmap(originalImage, maxWidthPixels, newHeight);
                    }
                }
                
                // For now I only tested Jpeg for Exif properties copy
    			if (originalImage.RawFormat == ImageFormat.Jpeg)
				{
					foreach (PropertyItem originalItem in originalImage.PropertyItems)
						resizedImage.SetPropertyItem (originalItem);
				}
            }
            else
            {
                // return original because it is is already smaller than maxWidth / maxHeight
                resizedImage = new Bitmap(originalImage);
            }

            return resizedImage;
        }
    }
}
