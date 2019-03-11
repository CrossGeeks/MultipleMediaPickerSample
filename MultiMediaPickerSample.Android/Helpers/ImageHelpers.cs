using System;
using System.IO;
using Android.Graphics;
using Android.Media;

namespace MultiMediaPickerSample.Droid.Helpers
{
    public static class ImageHelpers
    {

        public static byte[] RotateImage(string path,float scaleFactor,int quality = 90)
        {
            byte[] imageBytes;

            var originalImage = BitmapFactory.DecodeFile(path);
            var rotation = GetRotation(path);
            var width = (originalImage.Width * scaleFactor);
            var height = (originalImage.Height *scaleFactor);
            var scaledImage = Bitmap.CreateScaledBitmap(originalImage, (int)width, (int)height, true);

            Bitmap rotatedImage = scaledImage;
            if (rotation != 0)
            {
                var matrix = new Matrix();
                matrix.PostRotate(rotation);
                rotatedImage = Bitmap.CreateBitmap(scaledImage, 0, 0, scaledImage.Width, scaledImage.Height, matrix, true);
                scaledImage.Recycle();
                scaledImage.Dispose();
            }

            using (var ms = new MemoryStream())
            {
                rotatedImage.Compress(Bitmap.CompressFormat.Jpeg, quality, ms);
                imageBytes = ms.ToArray();
            }
      
           
            originalImage?.Dispose();
            rotatedImage?.Dispose();
            GC.Collect();

            return imageBytes;
        }

        static int GetRotation(string filePath)
        {
            using (var ei = new ExifInterface(filePath))
            {
                var orientation = (Android.Media.Orientation)ei.GetAttributeInt(ExifInterface.TagOrientation, (int)Android.Media.Orientation.Normal);

                switch (orientation)
                {
                    case Android.Media.Orientation.Rotate90:
                        return 90;
                    case Android.Media.Orientation.Rotate180:
                        return 180;
                    case Android.Media.Orientation.Rotate270:
                        return 270;
                    default:
                        return 0;
                }
            }
        }
    }
}
