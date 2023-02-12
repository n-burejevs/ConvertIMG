using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WebPWrapper;
namespace ConvertIMG
{
    internal class ConverterIMG
    {
        public ConverterIMG()
        {

        }
        //collect file names with low resolution
        string low_resolution_file_names;
        public void VaryQualityLevel(string path)
        {
            // Get a bitmap.
      
            Bitmap original_Img;
            
           //To Do: how to do this the righ way
            if (path.Contains(".webp"))
            {
                //find a new way to convert webp to bitmap
                try
                {

                    var webP = new WebP();
                    //webp is converted in load function
                    Bitmap webP_Img = webP.Load(path);
                    // webP.Decode();
                    original_Img = webP_Img;
                }
                catch
                {
                    original_Img = new Bitmap(path);
                }
            }
            else
            {

                original_Img = new Bitmap(path);

            }

            //png has transparency and because of that background gets black and crop function cant do its thing

            if (original_Img.RawFormat.Equals(ImageFormat.Png)) original_Img = ReplaceTransparency(original_Img, Color.White);

            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

            // Create an Encoder object based on the GUID
            // for the Quality parameter category.
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object.
            // An EncoderParameters object has an array of EncoderParameter
            // objects. In this case, there is only one
            // EncoderParameter object in the array.

            EncoderParameters myEncoderParameters = new EncoderParameters(1);

            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 100L);
            myEncoderParameters.Param[0] = myEncoderParameter;

            //debug
            //original_Img.Save(path + ".jpg", jpgEncoder, myEncoderParameters);
                            
            var croppedImage = CropToContent(original_Img);
                              
            //resize and keep aspect ratio, 1000x1000 is max size
            //2 - Maintain aspect ratio, and specify the desired image Width.
            //  >> Call the function with fixed FinalWidth and set FinalHeight to 0
            //3 - Maintain aspect ratio, and specify the desired image Height.
            //  >> Call the function with fixed FinalHeight and set FinalWidth to 0


            if (croppedImage.Width > 1000) croppedImage = Resize_Picture(croppedImage, 1000, 0);
            if (croppedImage.Height > 1000) croppedImage = Resize_Picture(croppedImage, 0, 1000);

            
            //remove png/jpg/jiff/jpeg from name
            //ChangeName(path, original_Img.RawFormat) 
            var new_img_path =  ChangeName(path);
            
            //small image
            if (croppedImage.Height < 400 || croppedImage.Width < 400)
            {
                          
                low_resolution_file_names += "LOW RES: " + new_img_path + "\n";
             
            }
            //dispose here, because i delete original img later
            original_Img.Dispose();

            //delete original image

            try
            {
                    //try closing file first?
                    
                    File.Delete(path);
                }//??????? use different catch exception??????????
                catch (FileNotFoundException dirNotFound)
                {
                    Console.WriteLine(dirNotFound.Message);
                }
          
             
            //save(convert) to jpEg cuz its not, better be sure
            croppedImage.Save(new_img_path + ".jpeg", jpgEncoder, myEncoderParameters);

            
            croppedImage.Dispose();
        }
        //crop image
        //https://stackoverflow.com/questions/46022742/c-sharp-winforms-gdi-crop-an-image-to-its-contents
        
        
        
        Bitmap CropToContent(Bitmap oldBmp)
        {
            Rectangle currentRect = new Rectangle();
            bool IsFirstOne = true;

            // Get a base color

            for (int y = 0; y < oldBmp.Height; y++)
            {
                for (int x = 0; x < oldBmp.Width; x++)
                {
                    Color debug = oldBmp.GetPixel(x, y);
                    // 255 255 255 is white
                    //ignore near pure white colors, because it does not crop to content
                    
                   // if (oldBmp.GetPixel(x, y) != Color.FromArgb(255, 255, 255, 255 ) & oldBmp.GetPixel(x, y) != Color.FromArgb(255, 254, 254, 254))
                        if (debug != Color.FromArgb(255, 255, 255, 255) & debug != Color.FromArgb(255, 254, 254, 254)
                                & debug != Color.FromArgb(255, 253, 253, 253))          

                        {
                            // We need to interpret this!

                            // Check if it is the first one!

                            if (IsFirstOne)
                        {
                            currentRect.X = x;
                            currentRect.Y = y;
                            currentRect.Width = 1;
                            currentRect.Height = 1;
                            IsFirstOne = false;
                        }
                     
                        else
                        {

                            if (!currentRect.Contains(new Point(x, y)))
                            {
                                // This will run if this is out of the current rectangle

                                if (x > (currentRect.X + currentRect.Width)) currentRect.Width = x - currentRect.X;
                                if (x < (currentRect.X))
                                {
                                    // Move the rectangle over there and extend it's width to make the right the same!
                                    int oldRectLeft = currentRect.Left;

                                    currentRect.X = x;
                                    currentRect.Width += oldRectLeft - x;
                                }

                                if (y > (currentRect.Y + currentRect.Height)) currentRect.Height = y - currentRect.Y;

                                if (y < (currentRect.Y + currentRect.Height))
                                {
                                    int oldRectTop = currentRect.Top;

                                    currentRect.Y = y;
                                    currentRect.Height += oldRectTop - y;
                                }
                            }
                        }
                    }
                
                }
                if (currentRect.Width  <= 0  || currentRect.Height <= 0 ) return oldBmp;
            }
            return CropImage(oldBmp, currentRect.X, currentRect.Y, currentRect.Width, currentRect.Height);
        }
        Bitmap CropImage(Image source, int x, int y, int width, int height)
        {
            Rectangle crop = new Rectangle(x, y, width, height);

            var bmp = new Bitmap(crop.Width, crop.Height);
            using (var gr = Graphics.FromImage(bmp))
            {
                gr.DrawImage(source, new Rectangle(0, 0, bmp.Width, bmp.Height), crop, GraphicsUnit.Pixel);
            }
            return bmp;
        }


        public void WalkDirectoryTree(System.IO.DirectoryInfo root)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try
            {
                files = root.GetFiles("*.*");
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e)
            {
                // This code just writes out the message and continues to recurse.
                // You may decide to do something different here. For example, you
                // can try to elevate your privileges and access the file again.
                Console.WriteLine(e.Message);
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (files != null)
            {
                //lets see whats in "files". maybe we can divide files and process images with 2 threads
                int size = files.Length;

                foreach (System.IO.FileInfo fi in files)
                {
                    // In this example, we only access the existing FileInfo object. If we
                    // want to open, delete or modify the file, then
                    // a try-catch block is required here to handle the case
                    // where the file has been deleted since the call to TraverseTree().
                    Console.WriteLine(fi.FullName);
                    try
                    {
                        VaryQualityLevel(fi.FullName);
                    }
                    catch (System.Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }


                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo);
                }
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        //use switch/case and pass case parameter based on bitmap rawformat
        //how to pass rawformat as an function argument?
        //public string ChangeName(string input, System.Drawing.Imaging.ImageFormat format) { }
        public string ChangeName(string input = "Test txt C:\\Users\\shark\\Desktop\\atteli\\12345\\1.png")
        {

            var pattern = ".png";
            var JPG_pattern = ".jpg";
            var JPEG_pattern = ".jpeg";
            var JFIF_pattern = ".jfif";
            var WEBP_pattern = ".webp";

            //remove .png
            var rgx = new Regex(pattern);
            var result = rgx.Replace(input, "", 1);

            //remove .jpg
            rgx = new Regex(JPG_pattern);
            result = rgx.Replace(result, "", 1);

            //remove .jpeg
            rgx = new Regex(JPEG_pattern);
            result = rgx.Replace(result, "", 1);

            //remove .jiff
            rgx = new Regex(JFIF_pattern);
            result = rgx.Replace(result, "", 1);
            //webp
            rgx = new Regex(WEBP_pattern);
            result = rgx.Replace(result, "", 1);

            return result;
        }
        //https://stackoverflow.com/questions/772388/c-sharp-how-can-i-test-a-file-is-a-jpeg
        public static bool HasJpegHeader(string filename)
        {
            try
            {
                // 0000000: ffd8 ffe0 0010 4a46 4946 0001 0101 0048  ......JFIF.....H
                // 0000000: ffd8 ffe1 14f8 4578 6966 0000 4d4d 002a  ......Exif..MM.*    
                using (BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.ReadWrite)))
                {
                    UInt16 soi = br.ReadUInt16();  // Start of Image (SOI) marker (FFD8)
                    UInt16 marker = br.ReadUInt16(); // JFIF marker (FFE0) EXIF marker (FFE1)
                    UInt16 markerSize = br.ReadUInt16(); // size of marker data (incl. marker)
                    UInt32 four = br.ReadUInt32(); // JFIF 0x4649464a or Exif  0x66697845

                    Boolean isJpeg = soi == 0xd8ff && (marker & 0xe0ff) == 0xe0ff;
                    Boolean isExif = isJpeg && four == 0x66697845;
                    Boolean isJfif = isJpeg && four == 0x4649464a;

                    if (isJpeg)
                    {
                        if (isExif)
                            Console.WriteLine("EXIF: {0}", filename);
                        else if (isJfif)
                            Console.WriteLine("JFIF: {0}", filename);
                        else
                            Console.WriteLine("JPEG: {0}", filename);

                    }

                    return isJpeg;
                    //return isJfif;
                    //return isExif;
                }
            }
            catch
            {
                return false;
            }
        }

    //can we detect the color of the background? and clear any background color?

        //https://stackoverflow.com/questions/618259/remove-transparency-in-images-with-c-sharp
        public static Bitmap ReplaceTransparency(string file, Color background)
        {
            return ReplaceTransparency(Image.FromFile(file), background);
        }

        public static Bitmap ReplaceTransparency(Image image, Color background)
        {
            return ReplaceTransparency((Bitmap)image, background);
        }

        public static Bitmap ReplaceTransparency(Bitmap bitmap, Color background)
        {
            /* Important: you have to set the PixelFormat to remove the alpha channel.
             * Otherwise you'll still have a transparent image - just without transparent areas */
            var result = new Bitmap(bitmap.Size.Width, bitmap.Size.Height, PixelFormat.Format24bppRgb);
            var g = Graphics.FromImage(result);

            g.Clear(background);
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            g.DrawImage(bitmap, 0, 0, bitmap.Size.Width, bitmap.Size.Height);
            
            //This bitmap was still in memory. The program could not delete, because it was "used by another process" 
            bitmap.Dispose();
            
            return result;
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        //https://www.codicode.com/art/resize_images_and_keep_aspect_ra.aspx
        //1- Set the destination Width and Height(but this can result in skewed images when the aspect ratio is not identical)
        //  >> Call the function and set a fixed FinalWidth and a fixed FinalHeight
        //2 - Maintain aspect ratio, and specify the desired image Width.
        //  >> Call the function with fixed FinalWidth and set FinalHeight to 0
        //3 - Maintain aspect ratio, and specify the desired image Height.
        //  >> Call the function with fixed FinalHeight and set FinalWidth to 0
        //the Org parameter is the full original file path on the disk, and Des is the destination file path, 
        //also with ImageQuality you can specify the quality of the generated image(Jpg), lower quality means smaller file size.
        public static Bitmap Resize_Picture(Bitmap Img_to_resize, /*string Des,*/ int FinalWidth, int FinalHeight/*, int ImageQuality*/)
        {
            System.Drawing.Bitmap NewBMP;
            System.Drawing.Graphics graphicTemp;
            System.Drawing.Bitmap bmp = Img_to_resize;

            int iWidth;
            int iHeight;
            if ((FinalHeight == 0) && (FinalWidth != 0))
            {
                iWidth = FinalWidth;
                iHeight = (bmp.Size.Height * iWidth / bmp.Size.Width);
            }
            else if ((FinalHeight != 0) && (FinalWidth == 0))
            {
                iHeight = FinalHeight;
                iWidth = (bmp.Size.Width * iHeight / bmp.Size.Height);
            }
            else
            {
                iWidth = FinalWidth;
                iHeight = FinalHeight;
            }

            NewBMP = new System.Drawing.Bitmap(iWidth, iHeight);
           graphicTemp = System.Drawing.Graphics.FromImage(NewBMP);
            graphicTemp.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            graphicTemp.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphicTemp.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphicTemp.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphicTemp.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphicTemp.DrawImage(bmp, 0, 0, iWidth, iHeight);
            graphicTemp.Dispose();
           
            bmp.Dispose();
            return NewBMP;
        }

        //write all file that are lower than 400x400px to textBox in Mainwindow.xaml
        //since i dont put "_small" in file names,
        //i cant think of otherways to inform user that images are smaller than needed
       
        public string printSmallimageNames()
        {
            return low_resolution_file_names;
        }

       
       

    }


}