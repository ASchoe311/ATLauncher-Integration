using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using ATLauncherInstanceImporter.Models;
using PhotoSauce.MagicScaler;

namespace ATLauncherInstanceImporter.Helpers
{
    internal class ImageHelpers
    {

        /// <summary>
        /// Try to save an image from the internet as a bitmap
        /// </summary>
        /// <param name="imageUrl">The URL of the image</param>
        /// <param name="bmp">The output Bitmap object</param>
        /// <returns>A boolean representing success of saving the image</returns>
        private static bool TrySaveWebImage(Uri imageUrl, out Bitmap bmp)
        {
            try
            {
                WebClient client = new WebClient();
                Stream stream = client.OpenRead(imageUrl);
                bmp = new Bitmap(stream);
                stream.Close();
                stream.Dispose();
                return true;
            }
            catch
            {
                bmp = null;
                return false;
            }
        }

        /// <summary>
        /// Gets a local image file of any codec as a bitmap
        /// </summary>
        /// <param name="imagePath">Path to the local image file</param>
        /// <returns>A <c>Bitmap</c> representation of the image</returns>
        public static Bitmap IngestLocalBitmap(string imagePath)
        {
            Bitmap bmp = null;
            BitmapImage bitmapImage = new BitmapImage();
            // Create BitmapImage from file
            using (var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                fs.Seek(0, SeekOrigin.Begin);
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = fs;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }
            // Convert BitmapImage to regular Bitmap
            using (var ms = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
                encoder.Save(ms);
                bmp = new Bitmap(ms);
                bmp.MakeTransparent();
            }
            return bmp;
        }

        /// <summary>
        /// Convert a bitmap image to a byte array
        /// </summary>
        /// <param name="bmp">Bitmap image to convert</param>
        /// <returns>Byte array containing bitmap data</returns>
        private static byte[] BmpToBytes(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                ms.Seek(0, SeekOrigin.Begin);
                bmp.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }

        private static Image BytesToImage(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                Image i = Image.FromStream(ms);
                return i;
            }
        }

        /// <summary>
        /// Gets a byte array from an image
        /// </summary>
        /// <param name="imagePath">Path to the image</param>
        /// <returns><c>byte[]</c> representing the image</returns>
        public static byte[] GetImageBytes(string imagePath)
        {
            Bitmap bmp = IngestLocalBitmap(imagePath);
            return BmpToBytes(bmp);
        }

        /// <summary>
        /// Resizes a bitmap with necessary padding to achieve the given aspect ratio (default 3:4)
        /// </summary>
        /// <param name="sourceBitmap">The bitmap to resize</param>
        /// <param name="targetAspect">The target aspect ratio</param>
        /// <returns>A byte array representing the resized image</returns>
        public static byte[] ResizeWithPadding(Bitmap sourceBitmap, double targetAspect = (3.0/4.0))
        {
            double sourceAspect = (double)sourceBitmap.Width / sourceBitmap.Height;

            // Calculate new dimensions while maintaining original resolution
            int targetWidth, targetHeight;

            if (sourceAspect > targetAspect)
            {
                targetWidth = sourceBitmap.Width;
                targetHeight = (int)(targetWidth * (4.0 / 3.0));
            }
            else
            {
                targetHeight = sourceBitmap.Height;
                targetWidth = (int)(targetHeight * (3.0 / 4.0));
            }

            // Create settings for MagicScaler
            var settings = new ProcessImageSettings
            {
                Width = targetWidth,
                Height = targetHeight,
                ResizeMode = CropScaleMode.Pad,
            };

            MemoryStream inStream = new MemoryStream();
            inStream.Seek(0, SeekOrigin.Begin);
            sourceBitmap.Save(inStream, ImageFormat.Png);
            inStream.Seek(0, SeekOrigin.Begin);
            MemoryStream outStream = new MemoryStream();
            outStream.Seek(0, SeekOrigin.Begin);

            MagicImageProcessor.ProcessImage(inStream, outStream, settings);
            outStream.Seek(0, SeekOrigin.Begin);

            return outStream.ToArray();
        }

        /// <summary>
        /// Attempt to resize an image (with padding) to the target aspect ratio
        /// </summary>
        /// <param name="imgUrl">Web URL of the image</param>
        /// <param name="imgBytes">Byte array to contain resized image</param>
        /// <param name="targetAspect">Target aspect ratio, default 4:3</param>
        /// <returns>True if the image was successfully resized, false otherwise</returns>
        public static bool TryResizeImage(Uri imgUrl, out byte[] imgBytes, double targetAspect = (3.0 / 4.0))
        {
            try
            {
                Bitmap bmp;
                if (TrySaveWebImage(imgUrl, out bmp))
                {
                    imgBytes = ResizeWithPadding(bmp);
                    return true;
                }
                imgBytes = null;
                return false;
            }
            catch
            {
                imgBytes = null;
                return false;
            }
        }

        /// <summary>
        /// Attempt to resize an image (with padding) to the target aspect ratio
        /// </summary>
        /// <param name="filePath">File path of the image</param>
        /// <param name="imgBytes">Byte array to contain resized image</param>
        /// <param name="targetAspect">Target aspect ratio, default 4:3</param>
        /// <returns>True if the image was successfully resized, false otherwise</returns>
        public static bool TryResizeImage(string filePath, out byte[] imgBytes, double targetAspect = (3.0 / 4.0))
        {
            try
            {
                Bitmap bmp = IngestLocalBitmap(filePath);
                imgBytes = ResizeWithPadding(bmp);
                return true;
            }
            catch
            {
                imgBytes = null;
                return false;
            }
        }

        /// <summary>
        /// Save the given byte array to the file system as an image at the specified path
        /// </summary>
        /// <param name="imgBytes">Byte array containing image data</param>
        /// <param name="filePath">Path to save the new image</param>
        public static void SaveBytesToImageFile(byte[] imgBytes, string filePath)
        {
            Image i = BytesToImage(imgBytes);
            i.Save(filePath, ImageFormat.Png);
        }


    }
}
