using System;
using System.Collections.Generic;
using System.Linq;

namespace PeakboardExtensionObjectDetection.Inference
{
    public static class PostProcessor
    {
        public static List<Detection> Process(float[] output, int numClasses, int numDetections,
            string[] classNames, float confidenceThreshold, float nmsThreshold,
            float ratioX, float ratioY, int padX, int padY, bool boxesAreXyxy = false)
        {
            // YOLO output shape: [1, numClasses+4, numDetections] = [1, 84, 8400] for COCO
            // Rows 0-3: cx, cy, w, h  (Ultralytics YOLOv8)
            //        or x1, y1, x2, y2 (LibreYOLO YOLOv9 export) when boxesAreXyxy
            // Rows 4+: class scores

            var candidates = new List<Detection>();

            for (int i = 0; i < numDetections; i++)
            {
                float maxScore = 0;
                int maxClassId = 0;

                for (int c = 0; c < numClasses; c++)
                {
                    float score = output[(4 + c) * numDetections + i];
                    if (score > maxScore)
                    {
                        maxScore = score;
                        maxClassId = c;
                    }
                }

                if (maxScore < confidenceThreshold) continue;

                float r0 = output[0 * numDetections + i];
                float r1 = output[1 * numDetections + i];
                float r2 = output[2 * numDetections + i];
                float r3 = output[3 * numDetections + i];

                // Normalize both box conventions to corner coordinates
                float left, top, right, bottom;
                if (boxesAreXyxy)
                {
                    left = r0; top = r1; right = r2; bottom = r3;
                }
                else
                {
                    left = r0 - r2 / 2; top = r1 - r3 / 2;
                    right = r0 + r2 / 2; bottom = r1 + r3 / 2;
                }

                // Convert from padded coordinates to original image coordinates
                float x1 = (left - padX) * ratioX;
                float y1 = (top - padY) * ratioY;
                float bw = (right - left) * ratioX;
                float bh = (bottom - top) * ratioY;

                // Skip invalid boxes
                if (bw <= 1 || bh <= 1) continue;

                candidates.Add(new Detection
                {
                    ClassId = maxClassId,
                    ClassName = maxClassId < classNames.Length ? classNames[maxClassId] : $"class_{maxClassId}",
                    Confidence = maxScore,
                    X = Math.Max(0, x1),
                    Y = Math.Max(0, y1),
                    Width = bw,
                    Height = bh
                });
            }

            return ApplyNms(candidates, nmsThreshold);
        }

        private static List<Detection> ApplyNms(List<Detection> detections, float threshold)
        {
            var result = new List<Detection>();
            var grouped = detections.GroupBy(d => d.ClassId);

            foreach (var group in grouped)
            {
                var sorted = group.OrderByDescending(d => d.Confidence).ToList();
                var keep = new bool[sorted.Count];
                for (int i = 0; i < keep.Length; i++) keep[i] = true;

                for (int i = 0; i < sorted.Count; i++)
                {
                    if (!keep[i]) continue;
                    for (int j = i + 1; j < sorted.Count; j++)
                    {
                        if (!keep[j]) continue;

                        // Standard IoU check
                        if (IoU(sorted[i], sorted[j]) > threshold)
                        {
                            keep[j] = false;
                            continue;
                        }

                        // Containment check: if the smaller box is mostly inside the larger box,
                        // suppress it. This catches cases where a large detection contains
                        // a smaller duplicate with low IoU due to size difference.
                        if (Containment(sorted[i], sorted[j]) > 0.7f)
                        {
                            keep[j] = false;
                        }
                    }
                }

                for (int i = 0; i < sorted.Count; i++)
                {
                    if (keep[i]) result.Add(sorted[i]);
                }
            }

            return result;
        }

        private static float IoU(Detection a, Detection b)
        {
            float intersection = IntersectionArea(a, b);
            float areaA = a.Width * a.Height;
            float areaB = b.Width * b.Height;
            float union = areaA + areaB - intersection;

            return union > 0 ? intersection / union : 0;
        }

        /// <summary>
        /// Returns the fraction of b's area that is inside a.
        /// If b is fully contained in a, returns 1.0.
        /// </summary>
        private static float Containment(Detection a, Detection b)
        {
            float intersection = IntersectionArea(a, b);
            float areaB = b.Width * b.Height;
            return areaB > 0 ? intersection / areaB : 0;
        }

        private static float IntersectionArea(Detection a, Detection b)
        {
            float x1 = Math.Max(a.X, b.X);
            float y1 = Math.Max(a.Y, b.Y);
            float x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            float y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            return Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        }
    }
}
