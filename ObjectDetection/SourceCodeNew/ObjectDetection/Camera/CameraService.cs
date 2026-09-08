using System;
using OpenCvSharp;

namespace PeakboardExtensionObjectDetection.Camera
{
    /// <summary>
    /// Manages a persistent camera connection. Keeps the connection open
    /// between grabs for performance. Reconnects automatically on failure.
    /// </summary>
    public sealed class CameraService : IDisposable
    {
        private VideoCapture _capture;
        private string _source;
        private bool _disposed;
        private readonly object _lock = new object();

        public bool IsOpen
        {
            get { lock (_lock) { return _capture != null && _capture.IsOpened(); } }
        }

        public void Open(string source)
        {
            lock (_lock)
            {
                Close();
                _source = source;

                int deviceIndex;
                if (int.TryParse(source, out deviceIndex))
                {
                    _capture = new VideoCapture(deviceIndex, VideoCaptureAPIs.DSHOW);
                }
                else
                {
                    _capture = new VideoCapture(source);
                    _capture.Set(VideoCaptureProperties.BufferSize, 1);
                }

                if (!_capture.IsOpened())
                {
                    _capture.Dispose();
                    _capture = null;
                    throw new InvalidOperationException($"Failed to open camera: {source}");
                }

                _disposed = false;
            }
        }

        /// <summary>
        /// Grab the latest frame from the persistent connection.
        /// Flushes buffered RTSP frames to get the most recent one.
        /// Returns null on failure. Caller must dispose the returned Mat.
        /// </summary>
        public Mat GrabFrame()
        {
            lock (_lock)
            {
                if (_disposed || _capture == null || !_capture.IsOpened())
                    return null;

                try
                {
                    // Flush buffered RTSP frames to get the latest
                    for (int i = 0; i < 3; i++)
                    {
                        if (!_capture.Grab()) break;
                    }

                    var frame = new Mat();
                    if (_capture.Retrieve(frame) && !frame.Empty())
                    {
                        return frame;
                    }
                    else
                    {
                        frame.Dispose();
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Reconnect to the same source after a connection failure.
        /// </summary>
        public bool Reconnect()
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(_source)) return false;

                try
                {
                    if (_capture != null)
                    {
                        try { _capture.Release(); } catch { }
                        try { _capture.Dispose(); } catch { }
                    }

                    int deviceIndex;
                    if (int.TryParse(_source, out deviceIndex))
                    {
                        _capture = new VideoCapture(deviceIndex, VideoCaptureAPIs.DSHOW);
                    }
                    else
                    {
                        _capture = new VideoCapture(_source);
                        _capture.Set(VideoCaptureProperties.BufferSize, 1);
                    }

                    return _capture.IsOpened();
                }
                catch
                {
                    return false;
                }
            }
        }

        public void Close()
        {
            lock (_lock)
            {
                _disposed = true;
                if (_capture != null)
                {
                    try { _capture.Release(); } catch { }
                    try { _capture.Dispose(); } catch { }
                    _capture = null;
                }
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
