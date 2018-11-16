using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace LibraryFramework
{
    public static class Imager
    {
        /// <summary>
        /// Save image as jpeg
        /// </summary>
        /// <param name="path">path where to save</param>
        /// <param name="img">image to save</param>
        public static void SaveJpeg(string path, Image img)
        {
            var qualityParam = new EncoderParameter(Encoder.Quality, 100L);
            var jpegCodec = GetEncoderInfo("image/jpeg");

            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;
            img.Save(path, jpegCodec, encoderParams);
        }

        /// <summary>
        /// Save image
        /// </summary>
        /// <param name="path">path where to save</param>
        /// <param name="img">image to save</param>
        /// <param name="imageCodecInfo">codec info</param>
        public static void Save(string path, Image img, ImageCodecInfo imageCodecInfo)
        {
            var qualityParam = new EncoderParameter(Encoder.Quality, 100L);

            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;
            img.Save(path, imageCodecInfo, encoderParams);
        }

        /// <summary>
        /// get codec info by mime type
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(t => t.MimeType == mimeType);
        }

        /// <summary>
        /// the image remains the same size, and it is placed in the middle of the new canvas
        /// </summary>
        /// <param name="image">image to put on canvas</param>
        /// <param name="width">canvas width</param>
        /// <param name="height">canvas height</param>
        /// <param name="canvasColor">canvas color</param>
        /// <returns></returns>
        public static Image PutOnCanvas(Image image, int width, int height, Color canvasColor)
        {
            var res = new Bitmap(width, height);
            using (var g = Graphics.FromImage(res))
            {
                g.Clear(canvasColor);
                var x = (width - image.Width) / 2;
                var y = (height - image.Height) / 2;
                g.DrawImageUnscaled(image, x, y, image.Width, image.Height);
            }

            return res;
        }

        /// <summary>
        /// the image remains the same size, and it is placed in the middle of the new canvas
        /// </summary>
        /// <param name="image">image to put on canvas</param>
        /// <param name="width">canvas width</param>
        /// <param name="height">canvas height</param>
        /// <returns></returns>
        public static Image PutOnWhiteCanvas(Image image, int width, int height)
        {
            return PutOnCanvas(image, width, height, Color.White);
        }

        /// <summary>
        /// resize an image and maintain aspect ratio
        /// </summary>
        /// <param name="image">image to resize</param>
        /// <param name="newWidth">desired width</param>
        /// <param name="maxHeight">max height</param>
        /// <param name="onlyResizeIfWider">if image width is smaller than newWidth use image width</param>
        /// <returns>resized image</returns>
        public static Image Resize(Image image, int newWidth, int maxHeight, bool onlyResizeIfWider)
        {
            if (onlyResizeIfWider && image.Width <= newWidth) newWidth = image.Width;

            var newHeight = image.Height * newWidth / image.Width;
            if (newHeight > maxHeight)
            {
                // Resize with height instead
                newWidth = image.Width * maxHeight / image.Height;
                newHeight = maxHeight;
            }

            var res = new Bitmap(newWidth, newHeight);

            using (var graphic = Graphics.FromImage(res))
            {
                graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphic.SmoothingMode = SmoothingMode.HighQuality;
                graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphic.CompositingQuality = CompositingQuality.HighQuality;
                graphic.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return res;
        }

        /// <summary>
        /// Crop an image 
        /// </summary>
        /// <param name="img">image to crop</param>
        /// <param name="cropArea">rectangle to crop</param>
        /// <returns>resulting image</returns>
        public static Image Crop(Image img, Rectangle cropArea)
        {
            var bmpImage = new Bitmap(img);
            var bmpCrop = bmpImage.Clone(cropArea, bmpImage.PixelFormat);
            return bmpCrop;
        }

        public static byte[] imageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            return ms.ToArray();
        }

        public static Image byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            Image returnImage = Image.FromStream(ms);
            return returnImage;
        }

        //The actual converting function
        public static string GetImage(object img)
        {
            return "data:image/jpg;base64," + Convert.ToBase64String((byte[])img);
        }


        public static void PerformImageResizeAndPutOnCanvas(string pFilePath, string pFileName, int pWidth, int pHeight, string pOutputFileName)
        {

            Image imgBef;
            imgBef = Image.FromFile(pFilePath + pFileName);


            Image _imgR;
            _imgR = Resize(imgBef, pWidth, pHeight, true);


            Image _img2;
            _img2 = PutOnCanvas(_imgR, pWidth, pHeight, Color.White);

            //Save JPEG
            SaveJpeg(pFilePath + pOutputFileName, _img2);

        }
    }
}
