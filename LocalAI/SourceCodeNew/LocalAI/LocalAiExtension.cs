using System.Runtime.InteropServices;
using Peakboard.ExtensionKit;

namespace LocalAI
{
    [ExtensionIcon("LocalAI.LocalAI.png")]
    public class LocalAiExtension : ExtensionBase
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        static LocalAiExtension()
        {
            // ONNX Runtime ships native DLLs (onnxruntime.dll, onnxruntime-genai.dll)
            // next to this assembly. The extension host loads us from a directory that
            // is not on the native probing path, so point the loader at ourselves
            // before anything P/Invokes. Without this the first Ask fails with
            // "Unable to load DLL 'onnxruntime-genai'".
            try
            {
                var dir = Path.GetDirectoryName(typeof(LocalAiExtension).Assembly.Location);
                if (!string.IsNullOrEmpty(dir))
                {
                    SetDllDirectory(dir);
                    var path = Environment.GetEnvironmentVariable("PATH") ?? "";
                    if (!path.Contains(dir))
                        Environment.SetEnvironmentVariable("PATH", dir + ";" + path);
                }
            }
            catch
            {
                // Best effort. If it fails the load error below is clear enough.
            }
        }

        public LocalAiExtension() : base() { }

        public LocalAiExtension(IExtensionHost host) : base(host) { }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "LocalAI",
                Name = "Local AI",
                Description = "Runs a small language model on the device itself. "
                            + "No cloud, no API key, no data leaves the machine.",
                // This pair is the compatibility range the Designer enforces on
                // boards, and it is NOT the same number as Extension.xml's - both
                // have to be bumped, which is easy to miss. A board records the
                // version it was built against, and the Designer refuses to open
                // one outside [MinVersion, Version] with "this custom list cannot
                // be edited".
                Version = "2.0",

                // Deliberately left at 1.0 even though 2.0 changed ModelPath from a
                // folder to a file. Refusing 1.x boards outright would force an
                // "upgrade" that discards their saved properties; letting them open
                // means CheckModelFolder gets to say "Since 2.0 it must be the model
                // FILE" against the actual path, which is a far better place to find
                // out.
                MinVersion = "1.0",
                Author = "Peakboard",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new ChatCustomList(),
            };
        }

        protected override void SetupOverride() { }

        protected override void CleanupOverride()
        {
            // Release the model - it is the largest thing this process holds.
            LlmEngine.Unload();
        }
    }
}
