using System;
using System.Net; 
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Diagnostics;

//source https://metadataconsulting.blogspot.com/2020/09/CSharp-dotNET-How-to-get-image-dimensions-from-header-of-webP-for-all-formats-lossy-lossless-extended-partially-load-image.html
namespace ConvertIMG
{
    



public class WebpDetection
{
	//https://stackoverflow.com/questions/111345/getting-image-dimensions-without-reading-the-entire-file/60667939#60667939
    //DecodeWebP reads only lossless :( 
	
	//My version improves DecodeWebP to read all webp formats, lossy, lossless and extended! 
	//http://metadataconsulting.blogspot.com/2020/09/CSharp-dotNET-How-to-get-image-dimensions-from-header-of-webP-for-all-formats-lossy-lossless-extended-partially-load-image.html
                
	internal static class ImageHelper
    {
        const string errorMessage = "Could not recognise image format.";

        private static Dictionary<byte[], Func<BinaryReader, Size>> imageFormatDecoders = new Dictionary<byte[], Func<BinaryReader, Size>>()
        {
            { new byte[] { 0x42, 0x4D }, DecodeBitmap },
            { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, DecodeGif },
            { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, DecodeGif },
            { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, DecodePng },
            { new byte[] { 0xff, 0xd8 }, DecodeJfif },
            { new byte[] { 0x52, 0x49, 0x46, 0x46 }, DecodeWebP },
        };

        /// <summary>        
        /// Gets the dimensions of an image.        
        /// </summary>        
        /// <param name="path">The path of the image to get the dimensions of.</param>        
        /// <returns>The dimensions of the specified image.</returns>        
        /// <exception cref="ArgumentException">The image was of an unrecognised format.</exception>            
        public static Size GetDimensions(BinaryReader binaryReader)
        {
            int maxMagicBytesLength = imageFormatDecoders.Keys.OrderByDescending(x => x.Length).First().Length;
            byte[] magicBytes = new byte[maxMagicBytesLength];
            for (int i = 0; i < maxMagicBytesLength; i += 1)
            {
                magicBytes[i] = binaryReader.ReadByte();
                foreach (var kvPair in imageFormatDecoders)
                {
                    if (StartsWith(magicBytes, kvPair.Key))
                    {
                        return kvPair.Value(binaryReader);
                    }
                }
            }

            throw new ArgumentException(errorMessage, "binaryReader");
        }
        
        /// <summary>
        /// Gets the dimensions of an image.
        /// </summary>
        /// <param name="path">The path of the image to get the dimensions of.</param>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognized format.</exception>
        public static Size GetDimensions(string path)
        {
            using (BinaryReader binaryReader = new BinaryReader(File.OpenRead(path)))
            {
                try
                {
                    return GetDimensions(binaryReader);
                }
                catch (ArgumentException e)
                {
                    if (e.Message.StartsWith(errorMessage))
                    {
                        throw new ArgumentException(errorMessage, "path", e);
                    }
                    else
                    {
                        throw e;
                    }
                }
            }
        }
		
		 /// <summary>
        /// Gets the dimensions of an image.
        /// </summary>
        /// <param name="path">The path of the image to get the dimensions of.</param>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognized format.</exception>
        public static Size GetDimensions(MemoryStream ms)
        {
            using (BinaryReader binaryReader = new BinaryReader(ms))
            {
                try
                {
                    return GetDimensions(binaryReader);
                }
                catch (ArgumentException e)
                {
                    if (e.Message.StartsWith(errorMessage))
                    {
                        throw new ArgumentException(errorMessage, "path", e);
                    }
                    else
                    {
                        throw e;
                    }
                }
            }
        }


        private static bool StartsWith(byte[] thisBytes, byte[] thatBytes)
        {
            for (int i = 0; i < thatBytes.Length; i += 1)
            {
                if (thisBytes[i] != thatBytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static short ReadLittleEndianInt16(BinaryReader binaryReader)
        {
            byte[] bytes = new byte[sizeof(short)];

            for (int i = 0; i < sizeof(short); i += 1)
            {
                bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt16(bytes, 0);
        }

        private static int ReadLittleEndianInt32(BinaryReader binaryReader)
        {
            byte[] bytes = new byte[sizeof(int)];
            for (int i = 0; i < sizeof(int); i += 1)
            {
                bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        private static Size DecodeBitmap(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(16);
            int width = binaryReader.ReadInt32();
            int height = binaryReader.ReadInt32();
            return new Size(width, height);
        }

        private static Size DecodeGif(BinaryReader binaryReader)
        {
            int width = binaryReader.ReadInt16();
            int height = binaryReader.ReadInt16();
            return new Size(width, height);
        }

        private static Size DecodePng(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(8);
            int width = ReadLittleEndianInt32(binaryReader);
            int height = ReadLittleEndianInt32(binaryReader);
            return new Size(width, height);
        }

        private static Size DecodeJfif(BinaryReader binaryReader)
        {
            while (binaryReader.ReadByte() == 0xff)
            {
                byte marker = binaryReader.ReadByte();
                short chunkLength = ReadLittleEndianInt16(binaryReader);
                if (marker == 0xc0 || marker == 0xc2) // c2: progressive
                {
                    binaryReader.ReadByte();
                    int height = ReadLittleEndianInt16(binaryReader);
                    int width = ReadLittleEndianInt16(binaryReader);
                    return new Size(width, height);
                }

                if (chunkLength < 0)
                {
                    ushort uchunkLength = (ushort)chunkLength;
                    binaryReader.ReadBytes(uchunkLength - 2);
                }
                else
                {
                    binaryReader.ReadBytes(chunkLength - 2);
                }
            }

            throw new ArgumentException(errorMessage);
        }
        
        //Other libs did not do it 
        //https://github.com/JosePineiro/WebP-wrapper/blob/master/WebPTest/WebPWrapper.cs
        //https://github.com/JimBobSquarePants/ImageProcessor/blob/6092da59e9aa4975e564002ef3c782a8f6bf3384/src/Plugins/ImageProcessor/ImageProcessor.Plugins.WebP/Imaging/Formats/WebPFormat.cs
      
		//fast
        private static Size DecodeWebP(BinaryReader binaryReader)
        {
            //https://developers.google.com/speed/webp/docs/riff_container
            //var riffseg = binaryReader.ReadBytes(4); //already offset 4 bytes 
            //var sizebytes = binaryReader.ReadBytes(4); // Size

            var size = binaryReader.ReadUInt32(); // Size - start at offset 4 
            
            var webp = binaryReader.ReadBytes(4); // start 8 offset

            var type = binaryReader.ReadBytes(4); // start 12 offset - VP8[ ] determination

            string VP8Type = System.Text.Encoding.UTF8.GetString(type);
			
			Console.WriteLine("VP8Type=\""+VP8Type+"\""); 

            int x = 0; 
            int y = 0;

            if (VP8Type == "VP8X") //Extra format - https://developers.google.com/speed/webp/docs/riff_container#extended_file_format
            { 
                //skip 32 bits or 8 bytes for Alpha, Exif, XMP flags... in VP8X header
                binaryReader.ReadBytes(8);

                byte[] w = binaryReader.ReadBytes(3); //24bits for width

                x = 1 + (w[2] << 16 | w[1] << 8 | w[0]); //little endian

                byte[] h = binaryReader.ReadBytes(3); //24bits for height

                y = 1 + (h[2] << 16 | h[1] << 8 | h[0]); 

				return new Size(x, y);

            }
            else if (VP8Type == "VP8L") //Lossless - https://developers.google.com/speed/webp/docs/webp_lossless_bitstream_specification#2_riff_header 
            {
                
                binaryReader.ReadBytes(4); //size
                byte[] sig = binaryReader.ReadBytes(1); //0x2f->47 1 byte signature
                if (sig[0] != 47) new Size(0, 0); 

                byte[] wh = binaryReader.ReadBytes(4); //width and height in 1 read
                x = 1 + (((wh[1] & 0x3F) << 8) | wh[0]); //{1 + ((($b1 & 0x3F) << 8) | $b0)} - https://blog.tcl.tk/38137  
                y = 1 + (((wh[3] & 0xF) << 10) | (wh[2] << 2) | ((wh[1] & 0xC0) >> 6)); //{1 + ((($b3 & 0xF) << 10) | ($b2 << 2) | (($b1 & 0xC0) >> 6))}]

				return new Size(x, y);

            }
            else if (VP8Type == "VP8 ") //Lossy - https://tools.ietf.org/html/rfc6386#section-9.1
            {

                //Lossy - https://tools.ietf.org/html/rfc6386#section-9.1 hard to decipher
                //pc->Width      = swap2(*(unsigned short*)(c+3))&0x3fff;  0x3fff -> 16383 decimal  swap2 - big or little indian depending on machine 
                //pc->Height     = swap2(*(unsigned short*)(c+5))&0x3fff;
              
				//https://blog.tcl.tk/38137 - much better
                  
                binaryReader.ReadBytes(7); //move to offset 23 or 0x17, 23-12+4=7 - open webp lossy file https://developers.google.com/speed/webp/gallery1

                byte[] frameTag = binaryReader.ReadBytes(3); //$b0 != 0x9d->157  || $b1 != 0x01>1 || $b2 != 0x2a->	42 
                if (frameTag[0] != 157 && frameTag[0] != 1 && frameTag[0] != 42) return new Size(0, 0); //invalid webp file

				//reads 2-bytes which is 16-bits, but we want only 14bits, so and it to 14 bits
                x = binaryReader.ReadUInt16() & 0x3fff;    //$width & 0x3fff -> & 0b00_11111111111111 c#7.0 above only
                y = binaryReader.ReadUInt16() & 0x3fff;    //$height & 0x3fff
            
				return new Size(x, y);
            }
            
            return new Size(0, 0);
            
        }

    }
	
	public static string GetFileNameFromURL(string hrefLink) //hack
	{
		string[] parts = hrefLink.Split('/');
		string fileName = string.Empty;

		if (parts.Length > 0)
			fileName = parts[parts.Length - 1];
		else
			fileName = hrefLink;

		return fileName;
	}
        /*
	public static void Main()
	{
	    Stopwatch sw = new Stopwatch(); 
		//string webpURL = "https://www.gstatic.com/webp/gallery/1.sm.webp";//Lossy
		//string webpURL = "https://www.gstatic.com/webp/gallery3/2_webp_ll.webp";//Lossless
		//string webpURL = "https://www.gstatic.com/webp/gallery3/1_webp_a.webp";//Extended with alpha channel
		//string webpURL = "https://mathiasbynens.be/demo/animated-webp-supported.webp"; //animated
		//string webpURL = "http://blog.mindworkshop.com/image/webp003.webp"; //animal resource was removed
		string webpURL ="https://1.bp.blogspot.com/-Q2kBYUdM6fc/X2TQpg0fxkI/AAAAAAAAMog/Tqr64JOmRbYgK2Avr36xx-Lg7q8Qu7KNgCNcBGAsYHQ/w320-h240/GenevaDrive.webp";
		
	    string webpfile = GetFileNameFromURL(webpURL); 
		
		Size webpSize = new Size(); 
		Stopwatch stp = new Stopwatch();
        		
	    WebClient wc = new WebClient();
		using (MemoryStream stream = new MemoryStream(wc.DownloadData(webpURL)))
		{
         	stp.Start(); 
			webpSize = ImageHelper.GetDimensions(stream); 
	     	stp.Stop(); 
	     
		}
		Console.WriteLine("WebP file \"{0}\" has dimensions [{1}w X {2}h] in {3}ms.", webpfile, webpSize.Width, webpSize.Height, stp.ElapsedMilliseconds);
		
		stp.Reset(); 	
		using (MemoryStream stream = new MemoryStream(wc.DownloadData(webpURL)))
		{
        	stp.Start(); 
		 	Image imageS = Image.FromStream(stream);	
		 	stp.Stop(); 
		 	Console.WriteLine("WebP file \"{0}\" has dimensions [{1}w X {2}h] in {3}ms, image obj from full stream.", webpfile, imageS.Width, imageS.Height, stp.ElapsedMilliseconds);
		 
		}
		
		
	}*/
}
}