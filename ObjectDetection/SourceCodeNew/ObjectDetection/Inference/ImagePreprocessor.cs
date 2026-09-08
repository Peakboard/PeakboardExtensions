using OpenCvSharp;

namespace PeakboardExtensionObjectDetection.Inference
{
    public static class ImagePreprocessor
    {
        public static float[] Preprocess(Mat image, int targetSize, out float ratioX, out float ratioY, out int padX, out int padY)
        {
            int origW = image.Width;
            int origH = image.Height;

            float scale = (float)targetSize / System.Math.Max(origW, origH);
            int newW = (int)(origW * scale);
            int newH = (int)(origH * scale);

            padX = (targetSize - newW) / 2;
            padY = (targetSize - newH) / 2;

            ratioX = (float)origW / newW;
            ratioY = (float)origH / newH;

            using (var resized = new Mat())
            using (var padded = new Mat())
            {
                Cv2.Resize(image, resized, new Size(newW, newH));
                Cv2.CopyMakeBorder(resized, padded, padY, targetSize - newH - padY, padX, targetSize - newW - padX,
                    BorderTypes.Constant, new Scalar(114, 114, 114));

                using (var rgb = new Mat())
                {
                    Cv2.CvtColor(padded, rgb, ColorConversionCodes.BGR2RGB);
                    return MatToTensor(rgb, targetSize);
                }
            }
        }

        private static float[] MatToTensor(Mat rgb, int size)
        {
            int channelSize = size * size;
            float[] tensor = new float[3 * channelSize];

            var indexer = rgb.GetGenericIndexer<Vec3b>();
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var pixel = indexer[y, x];
                    int idx = y * size + x;
                    tensor[idx] = pixel.Item0 / 255f;                    // R
                    tensor[channelSize + idx] = pixel.Item1 / 255f;      // G
                    tensor[2 * channelSize + idx] = pixel.Item2 / 255f;  // B
                }
            }

            return tensor;
        }
    }
}
