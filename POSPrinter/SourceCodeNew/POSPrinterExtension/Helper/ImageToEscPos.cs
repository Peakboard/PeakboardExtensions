using System.Drawing;
using System.IO;

namespace POSPrinter.Helper
{
    public class ImageToEscPos
    {
        public byte[] Image(string imageName)
        {
            Bitmap bmp;
            string path = Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location
            );
            string image = Path.GetFullPath(Path.Combine(path, @"..\..\Resources\")) + imageName;

            try
            {
                bmp = new Bitmap(image);
            }
            catch
            {
                return null;
            }

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(new byte[] { 0x1B, 0x33, 0x00 }); // Set line spacing

            int width = bmp.Width;
            int height = bmp.Height;

            if (width > 300)
                return null;

            for (int y = 0; y < height; y += 24)
            {
                writer.Write(new byte[] { 0x1B, 0x2A, 0x21 }); // ESC * Select bit image mode
                writer.Write((byte)(width % 256)); // width LSB
                writer.Write((byte)(width / 256)); // width MSB

                for (int x = 0; x < width; x++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        byte slice = 0;
                        for (int b = 0; b < 8; b++)
                        {
                            int pixelY = y + k * 8 + b;
                            if (pixelY < height)
                            {
                                System.Drawing.Color pixelColor = bmp.GetPixel(x, pixelY);
                                if (pixelColor.GetBrightness() < 0.5) // Black pixel
                                {
                                    slice |= (byte)(1 << (7 - b));
                                }
                            }
                        }
                        writer.Write(slice);
                    }
                }
                writer.Write(new byte[] { 0x0A }); // Line feed
            }

            writer.Write(new byte[] { 0x1B, 0x32 }); // Reset line spacing
            return stream.ToArray();

            //return image;
        }
    }
}
