using System.Runtime.Versioning;

// The extension is Windows-only by construction: Peakboard Runtime is Windows,
// the OpenCV and ONNX Runtime natives are win-x64, and camera enumeration goes
// through DirectShow COM. Declaring it here is what lets the project target
// plain net8.0 (keeping the 24 MB Windows SDK projection out of the package)
// without the platform-compatibility analyser flagging every COM call.
[assembly: SupportedOSPlatform("windows")]
