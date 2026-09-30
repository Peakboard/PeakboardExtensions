using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace PeakboardExtensionObjectDetection.Annotation
{
    /// <summary>
    /// What the operator drew around one object, as the Drawing Area hands it to Lua with
    /// getstrokes(): {"width":w,"height":h,"strokes":[[[x,y],...],...]} in the drawing
    /// area's own pixels.
    ///
    /// The bounding box is the rectangle through the topmost, leftmost, rightmost and
    /// bottommost point of ALL strokes together - an outline is often drawn in two or three
    /// goes, and it is the whole outline that encloses the object.
    /// </summary>
    public sealed class DrawingOutline
    {
        public double AreaWidth { get; private set; }
        public double AreaHeight { get; private set; }
        public int PointCount { get; private set; }
        public double Left { get; private set; }
        public double Top { get; private set; }
        public double Right { get; private set; }
        public double Bottom { get; private set; }

        /// <summary>The JSON exactly as it came from the Drawing Area - kept and saved as is.</summary>
        public string RawJson { get; private set; } = "";

        /// <summary>Parses the getstrokes() JSON. Returns null and an explanation when it cannot be used.</summary>
        public static DrawingOutline Parse(string json, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Nothing was drawn.";
                return null;
            }

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch (JsonException)
            {
                error = "The drawing is not in the format getstrokes() returns.";
                return null;
            }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("strokes", out var strokes)
                    || strokes.ValueKind != JsonValueKind.Array)
                {
                    error = "The drawing is not in the format getstrokes() returns.";
                    return null;
                }

                var outline = new DrawingOutline
                {
                    AreaWidth = Number(root, "width"),
                    AreaHeight = Number(root, "height"),
                    Left = double.MaxValue,
                    Top = double.MaxValue,
                    Right = double.MinValue,
                    Bottom = double.MinValue,
                    RawJson = json,
                };

                foreach (var stroke in strokes.EnumerateArray())
                {
                    if (stroke.ValueKind != JsonValueKind.Array) continue;
                    foreach (var point in stroke.EnumerateArray())
                    {
                        if (point.ValueKind != JsonValueKind.Array || point.GetArrayLength() < 2) continue;
                        // A coordinate that is not a number means it did not come from getstrokes().
                        // GetDouble() would throw, and the operator would read a .NET message.
                        if (point[0].ValueKind != JsonValueKind.Number || point[1].ValueKind != JsonValueKind.Number)
                        {
                            error = "The drawing is not in the format getstrokes() returns.";
                            return null;
                        }
                        var x = point[0].GetDouble();
                        var y = point[1].GetDouble();
                        outline.Left = Math.Min(outline.Left, x);
                        outline.Top = Math.Min(outline.Top, y);
                        outline.Right = Math.Max(outline.Right, x);
                        outline.Bottom = Math.Max(outline.Bottom, y);
                        outline.PointCount++;
                    }
                }

                if (outline.PointCount == 0)
                {
                    error = "Nothing was drawn.";
                    return null;
                }

                if (outline.AreaWidth <= 0 || outline.AreaHeight <= 0)
                {
                    error = "The drawing area reported no size.";
                    return null;
                }

                return outline;
            }
        }

        /// <summary>
        /// The outline's extreme points, mapped from the drawing area onto the frame's pixels.
        ///
        /// The frozen frame is shown in an image control of the same position and size as the
        /// drawing area. With Stretch "Uniform" the frame is scaled to fit and centred, leaving
        /// bars on two sides; with "Fill" it is stretched to the area. Everything outside the
        /// frame is clamped to its edge.
        /// </summary>
        public PixelBox ToFrame(int frameWidth, int frameHeight, string imageStretch)
        {
            double scaleX, scaleY, offsetX = 0, offsetY = 0;
            if (string.Equals(imageStretch, "Fill", StringComparison.OrdinalIgnoreCase))
            {
                scaleX = AreaWidth / frameWidth;
                scaleY = AreaHeight / frameHeight;
            }
            else
            {
                scaleX = scaleY = Math.Min(AreaWidth / frameWidth, AreaHeight / frameHeight);
                offsetX = (AreaWidth - frameWidth * scaleX) / 2;
                offsetY = (AreaHeight - frameHeight * scaleY) / 2;
            }

            var left = Clamp((Left - offsetX) / scaleX, frameWidth);
            var top = Clamp((Top - offsetY) / scaleY, frameHeight);
            var right = Clamp((Right - offsetX) / scaleX, frameWidth);
            var bottom = Clamp((Bottom - offsetY) / scaleY, frameHeight);

            return new PixelBox(Math.Round(left), Math.Round(top),
                Math.Round(right - left), Math.Round(bottom - top));
        }

        private static double Clamp(double value, int max) => Math.Max(0, Math.Min(max, value));

        private static double Number(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var value)) return 0;
            if (value.ValueKind == JsonValueKind.Number) return value.GetDouble();
            return double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
        }
    }

    /// <summary>A box in frame pixels, origin top-left - the same convention as the Detections list.</summary>
    public readonly struct PixelBox
    {
        public PixelBox(double x, double y, double width, double height)
        {
            X = x; Y = y; Width = width; Height = height;
        }

        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
    }
}
