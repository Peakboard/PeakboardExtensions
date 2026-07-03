using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;

namespace DesktopToolbox
{
    [Serializable]
    [CustomListIcon("DesktopToolbox.DesktopToolbox.png")]
    public class DesktopInformationCustomList : CustomListBase
    {
        // Tracks the in-flight/last-completed OpenFileAsBase64 background operation, so
        // OpenFileAsBase64Start/OpenFileAsBase64Result can hand work off between two fast,
        // non-blocking calls instead of one call blocking until the user responds - the
        // Peakboard host enforces a timeout on ExecuteFunction, so a call that waits for
        // the user inside the function will reliably throw a TimeoutException.
        private readonly object _fileDialogLock = new object();
        private Task<string>? _fileDialogTask;

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "DesktopInformation",
                Name = "Desktop Information",
                Description = "Returns information about the current desktop session and provides utility functions.",
                PropertyInputPossible = false,
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "OpenURLInBrowser",
                        Description = "Opens the given URL in the default browser.",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "url",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The URL to open in the default browser"
                            },
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "WriteTextFile",
                        Description = "Writes the given content to a text file. Creates the file if it does not exist, overwrites it if it does. Returns \"OK\" on success, otherwise the error message.",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "fileName",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "Full path of the file to write, including the folder (e.g. C:\\Temp\\out.txt)"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "content",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The text content to write to the file"
                            },
                        },
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "result",
                                Type = CustomListFunctionParameterTypes.String,
                                Description = "\"OK\" on success, or the error message on failure"
                            },
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "OpenFileAsBase64Start",
                        Description = "Starts a Windows file selection dialog (Explorer) on a background thread and returns " +
                                      "immediately. The Peakboard host enforces a timeout on function calls, so the dialog " +
                                      "cannot block inside a single call - poll OpenFileAsBase64Result afterwards (e.g. every " +
                                      "500ms via a timer) until its status is no longer \"pending\".",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "extensions",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = true,
                                Description = "Comma-separated list of allowed file extensions without wildcards or dots, e.g. \"pdf,jpg,png\". Leave empty to allow all file types."
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "title",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = true,
                                Description = "Optional title text shown in the file dialog window."
                            },
                        },
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "result",
                                Type = CustomListFunctionParameterTypes.String,
                                Description = "JSON object: { \"status\": \"started\" | \"busy\" }. \"busy\" means a dialog from a " +
                                              "previous call is still open - call OpenFileAsBase64Result to check on it, or wait " +
                                              "for the user to close it before starting a new one."
                            },
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "OpenFileAsBase64Result",
                        Description = "Checks on / collects the result of a file dialog previously started with OpenFileAsBase64Start. " +
                                      "Call this repeatedly (e.g. every 500ms via a timer) until \"status\" is no longer \"pending\".",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection(),
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "result",
                                Type = CustomListFunctionParameterTypes.String,
                                Description = "JSON object: { \"status\": string, \"fileName\": string, \"fileNameOnly\": string, " +
                                              "\"base64\": string, \"error\": string }. \"status\" is one of: \"idle\" (nothing " +
                                              "started yet), \"pending\" (still waiting for the user), \"done\" (a file was " +
                                              "picked - fileName/fileNameOnly/base64 are populated), \"cancelled\" (the user " +
                                              "closed the dialog without picking a file), or \"error\" (see the \"error\" field " +
                                              "for details). fileName/fileNameOnly/base64 are only populated when status is \"done\". " +
                                              "A terminal result (done/cancelled/error) is returned exactly once; afterwards the " +
                                              "status reverts to \"idle\" until a new dialog is started."
                            },
                        },
                    }
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("WindowsUserName", CustomListColumnTypes.String),
                new CustomListColumn("OSVersion", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();
            items.Add(new CustomListObjectElement
            {
                { "WindowsUserName", Environment.UserName },
                { "OSVersion", Environment.OSVersion.ToString() },
            });
            return items;
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            if (context.FunctionName.Equals("OpenURLInBrowser", StringComparison.InvariantCultureIgnoreCase))
            {
                var url = context.Values[0].StringValue;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c start \"\" \"{url}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            else if (context.FunctionName.Equals("WriteTextFile", StringComparison.InvariantCultureIgnoreCase))
            {
                var ret = new CustomListExecuteReturnContext();
                ret.Add(WriteTextFile(context.Values[0].StringValue, context.Values[1].StringValue));
                return ret;
            }
            else if (context.FunctionName.Equals("OpenFileAsBase64Start", StringComparison.InvariantCultureIgnoreCase))
            {
                var json = OpenFileAsBase64Start(context.Values[0].StringValue, context.Values[1].StringValue);

                var ret = new CustomListExecuteReturnContext();
                ret.Add(json);
                return ret;
            }
            else if (context.FunctionName.Equals("OpenFileAsBase64Result", StringComparison.InvariantCultureIgnoreCase))
            {
                var json = OpenFileAsBase64Result();

                var ret = new CustomListExecuteReturnContext();
                ret.Add(json);
                return ret;
            }

            return new CustomListExecuteReturnContext();
        }

        /// <summary>
        /// Writes <paramref name="content"/> to <paramref name="fileName"/> as UTF-8 text,
        /// overwriting any existing file. Returns "OK" on success, or the error message
        /// on failure (never throws back into Peakboard). The resolved absolute path is
        /// written to the extension log, and Windows UAC file virtualization is detected
        /// and reported so a redirected write is never silently mistaken for success.
        /// </summary>
        private string WriteTextFile(string fileName, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return "fileName is required.";
                }

                // Resolve to an absolute path. A relative path (no drive/root) would be
                // written relative to the Peakboard process's current directory, which is
                // almost never where the caller is looking - this makes the location explicit.
                var fullPath = Path.GetFullPath(fileName.Trim());
                this.Log.Info($"WriteTextFile: requested '{fileName}', resolved to '{fullPath}'.");

                var writtenAtUtc = DateTime.UtcNow;
                File.WriteAllText(fullPath, content ?? string.Empty);

                // Confirm the file is really on disk at the resolved path. If it is not,
                // something removed it right after the write (e.g. antivirus) - report
                // that instead of a misleading "OK".
                if (!File.Exists(fullPath))
                {
                    this.Log.Warning($"WriteTextFile: write reported success but no file at '{fullPath}'.");
                    return $"Write reported success but the file is not present at '{fullPath}'.";
                }

                // Detect Windows UAC file virtualization. A write to a folder the process
                // user cannot actually write to (commonly anything under C:\) is silently
                // redirected to %LOCALAPPDATA%\VirtualStore. The write "succeeds", but the
                // file is NOT at fullPath for Explorer or any other process - which looks
                // exactly like "returns OK but no file in the destination directory".
                var virtualPath = GetVirtualStorePath(fullPath);
                if (!string.IsNullOrEmpty(virtualPath) && File.Exists(virtualPath)
                    && File.GetLastWriteTimeUtc(virtualPath) >= writtenAtUtc.AddSeconds(-2))
                {
                    this.Log.Warning($"WriteTextFile: Windows redirected the write (UAC virtualization) to '{virtualPath}'.");
                    return $"Windows redirected the file to '{virtualPath}' because the folder " +
                           $"'{Path.GetDirectoryName(fullPath)}' is not writable by the Peakboard process. " +
                           "Use a user-writable folder (e.g. under the user profile or a folder you granted write access to).";
                }

                var bytes = new FileInfo(fullPath).Length;
                this.Log.Info($"WriteTextFile: wrote {bytes} bytes to '{fullPath}'.");
                return "OK";
            }
            catch (Exception ex)
            {
                this.Log.Error($"WriteTextFile failed: {ex.Message}");
                return ex.Message;
            }
        }

        /// <summary>
        /// Computes the %LOCALAPPDATA%\VirtualStore equivalent of an absolute local path,
        /// used to detect UAC file virtualization redirection. Returns an empty string if
        /// the path is not on a local drive (e.g. a UNC path), where virtualization never
        /// applies.
        /// </summary>
        private static string GetVirtualStorePath(string fullPath)
        {
            try
            {
                var root = Path.GetPathRoot(fullPath); // e.g. "C:\"
                if (string.IsNullOrEmpty(root) || root.Length < 2 || root[1] != ':')
                {
                    return string.Empty; // UNC or unexpected - virtualization does not apply
                }

                var relative = fullPath.Substring(root.Length); // e.g. "temp\MeineMeldungen.csv"
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(localAppData))
                {
                    return string.Empty;
                }

                return Path.Combine(localAppData, "VirtualStore", relative);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Starts the file dialog on a dedicated background thread (not the thread pool - the
        /// operation can block for an arbitrarily long time waiting for the user, and we do not
        /// want to risk starving the pool) and returns immediately. The Peakboard host enforces a
        /// timeout on ExecuteFunction calls, so the dialog itself must never run on the calling
        /// thread - see OpenFileAsBase64Result for how the outcome is collected afterwards.
        /// </summary>
        private string OpenFileAsBase64Start(string? extensionsCsv, string? title)
        {
            lock (_fileDialogLock)
            {
                if (_fileDialogTask != null && !_fileDialogTask.IsCompleted)
                {
                    this.Log.Info("OpenFileAsBase64Start: a dialog is already in progress - ignoring.");
                    return BuildResultJson("busy", string.Empty, string.Empty, string.Empty, string.Empty);
                }

                _fileDialogTask = Task.Factory.StartNew(
                    () => RunFileDialog(extensionsCsv, title),
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }

            this.Log.Info("OpenFileAsBase64Start: dialog started on a background thread.");
            return BuildResultJson("started", string.Empty, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Reports on the background operation started by OpenFileAsBase64Start: "idle" if
        /// nothing was ever started, "pending" while still waiting on the user, or the terminal
        /// "done"/"cancelled"/"error" result (consumed exactly once - a second call after a
        /// terminal result reverts to "idle" until a new dialog is started).
        /// </summary>
        private string OpenFileAsBase64Result()
        {
            Task<string>? task;
            lock (_fileDialogLock)
            {
                task = _fileDialogTask;
            }

            if (task == null)
            {
                return BuildResultJson("idle", string.Empty, string.Empty, string.Empty, string.Empty);
            }

            if (!task.IsCompleted)
            {
                return BuildResultJson("pending", string.Empty, string.Empty, string.Empty, string.Empty);
            }

            lock (_fileDialogLock)
            {
                // Only clear it if a newer run has not already replaced it.
                if (ReferenceEquals(_fileDialogTask, task))
                {
                    _fileDialogTask = null;
                }
            }

            if (task.IsFaulted)
            {
                var message = task.Exception?.GetBaseException().Message ?? "Unknown error.";
                this.Log.Error($"OpenFileAsBase64Result: background task failed: {message}");
                return BuildResultJson("error", string.Empty, string.Empty, string.Empty, message);
            }

            return task.Result;
        }

        /// <summary>
        /// Shows the classic Windows "Open File" dialog via comdlg32.dll (GetOpenFileName),
        /// optionally restricted to <paramref name="extensionsCsv"/>, and returns a JSON string
        /// { "status": "done"|"cancelled"|"error", "fileName": ..., "fileNameOnly": ..., "base64": ..., "error": ... }
        /// describing the outcome. Runs entirely on the background thread started by
        /// OpenFileAsBase64Start - never call this directly from ExecuteFunctionOverride, since
        /// it blocks until the user closes the dialog.
        /// This deliberately avoids System.Windows.Forms/WPF: the Extension Kit targets plain
        /// net8.0 (no Windows Desktop runtime dependency), and unlike the modern IFileOpenDialog
        /// COM API, GetOpenFileName does not require an STA thread - it is safe to call directly
        /// from whatever thread invokes this function.
        /// </summary>
        private string RunFileDialog(string? extensionsCsv, string? title)
        {
            const int bufferChars = 4096; // generous - handles long paths, not just MAX_PATH
            var fileBuffer = Marshal.AllocHGlobal(bufferChars * sizeof(char));
            var ownerHwnd = IntPtr.Zero;

            try
            {
                // Zero the buffer: GetOpenFileName expects it to start empty, and this
                // guarantees a NUL terminator exists somewhere even in edge cases.
                var zeros = new byte[bufferChars * sizeof(char)];
                Marshal.Copy(zeros, 0, fileBuffer, zeros.Length);

                // An invisible, always-on-top owner window. Windows keeps an owned window
                // above its owner in the z-order, so giving the dialog a topmost (but
                // invisible) owner reliably brings it to the front - even if the Peakboard
                // host has no window of its own to hand us, or is running full-screen/kiosk.
                ownerHwnd = CreateWindowExW(
                    WsExTopmost | WsExToolWindow,
                    "STATIC",
                    string.Empty,
                    WsPopup,
                    0, 0, 0, 0,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                var ofn = new NativeOpenFileName
                {
                    hwndOwner = ownerHwnd,
                    lpstrFile = fileBuffer,
                    nMaxFile = bufferChars,
                    lpstrFilter = BuildWin32Filter(extensionsCsv),
                    lpstrTitle = string.IsNullOrWhiteSpace(title) ? "Datei auswählen" : title,
                    Flags = OfnFileMustExist | OfnPathMustExist | OfnExplorer | OfnNoChangeDir | OfnHideReadonly,
                };

                if (!GetOpenFileNameW(ofn))
                {
                    var extendedError = CommDlgExtendedError();
                    if (extendedError == 0)
                    {
                        // A zero extended error after a "false" result means the user
                        // simply closed/cancelled the dialog - not a real failure.
                        this.Log.Info("RunFileDialog: dialog was cancelled by the user.");
                        return BuildResultJson("cancelled", string.Empty, string.Empty, string.Empty, "CANCELLED");
                    }

                    this.Log.Warning($"RunFileDialog: GetOpenFileName failed (CommDlgExtendedError {extendedError}).");
                    return BuildResultJson("error", string.Empty, string.Empty, string.Empty, $"File dialog failed (CommDlgExtendedError {extendedError}).");
                }

                var selectedPath = Marshal.PtrToStringUni(fileBuffer) ?? string.Empty;
                var selectedFileNameOnly = Path.GetFileName(selectedPath);

                this.Log.Info($"RunFileDialog: reading '{selectedPath}'.");
                var bytes = File.ReadAllBytes(selectedPath);
                var base64 = Convert.ToBase64String(bytes);
                this.Log.Info($"RunFileDialog: read {bytes.Length} bytes from '{selectedPath}'.");
                return BuildResultJson("done", selectedPath, selectedFileNameOnly, base64, string.Empty);
            }
            catch (Exception ex)
            {
                this.Log.Error($"RunFileDialog failed: {ex.Message}");
                return BuildResultJson("error", string.Empty, string.Empty, string.Empty, ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(fileBuffer);
                if (ownerHwnd != IntPtr.Zero)
                {
                    DestroyWindow(ownerHwnd);
                }
            }
        }

        /// <summary>
        /// Builds the { "status": ..., "fileName": ..., "fileNameOnly": ..., "base64": ..., "error": ... }
        /// JSON payload shared by OpenFileAsBase64Start/Result. Written by hand (rather than via a
        /// serializer) so this stays a single self-contained method with no extra dependency
        /// beyond basic string escaping.
        /// </summary>
        private static string BuildResultJson(string status, string fileName, string fileNameOnly, string base64, string error)
        {
            return "{"
                + "\"status\":" + JsonString(status) + ","
                + "\"fileName\":" + JsonString(fileName) + ","
                + "\"fileNameOnly\":" + JsonString(fileNameOnly) + ","
                + "\"base64\":" + JsonString(base64) + ","
                + "\"error\":" + JsonString(error)
                + "}";
        }

        /// <summary>
        /// Minimal JSON string escaping (quotes, backslashes, control characters). Sufficient here
        /// because the only untrusted input flowing through is a Windows file path.
        /// </summary>
        private static string JsonString(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        /// <summary>
        /// Builds a Win32 GetOpenFileName filter string (NUL-separated pairs of description and
        /// pattern, double-NUL terminated) from a comma/semicolon separated list of extensions
        /// (with or without a leading dot or wildcard, e.g. "pdf, .jpg,*.png"). Always appends an
        /// "all files" choice so a restrictive list never fully blocks the user if they need an
        /// edge case. An empty/null list results in an "all files only" filter.
        /// </summary>
        private static string BuildWin32Filter(string? extensionsCsv)
        {
            var patterns = ParseExtensionPatterns(extensionsCsv);
            var sb = new StringBuilder();

            if (patterns.Count > 0)
            {
                var patternList = string.Join(";", patterns);
                sb.Append("Erlaubte Dateien (").Append(patternList).Append(")\0");
                sb.Append(patternList).Append('\0');
            }

            sb.Append("Alle Dateien (*.*)\0*.*\0\0");
            return sb.ToString();
        }

        /// <summary>
        /// Parses a comma/semicolon separated extension list into "*.ext" wildcard patterns,
        /// stripping any leading dots/wildcards the caller may have included.
        /// </summary>
        private static List<string> ParseExtensionPatterns(string? extensionsCsv)
        {
            if (string.IsNullOrWhiteSpace(extensionsCsv))
            {
                return new List<string>();
            }

            return extensionsCsv
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().TrimStart('*').TrimStart('.').Trim())
                .Where(e => e.Length > 0)
                .Select(e => "*." + e)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // -- Win32 GetOpenFileName interop ---------------------------------------------------

        private const int OfnFileMustExist = 0x00001000;
        private const int OfnPathMustExist = 0x00000800;
        private const int OfnExplorer = 0x00080000;
        private const int OfnNoChangeDir = 0x00000008;
        private const int OfnHideReadonly = 0x00000004;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class NativeOpenFileName
        {
            public int lStructSize = Marshal.SizeOf(typeof(NativeOpenFileName));
            public IntPtr hwndOwner = IntPtr.Zero;
            public IntPtr hInstance = IntPtr.Zero;
            public string? lpstrFilter;
            public string? lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public IntPtr lpstrFile = IntPtr.Zero;
            public int nMaxFile;
            public string? lpstrFileTitle;
            public int nMaxFileTitle;
            public string? lpstrInitialDir;
            public string? lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string? lpstrDefExt;
            public IntPtr lCustData = IntPtr.Zero;
            public IntPtr lpfnHook = IntPtr.Zero;
            public string? lpTemplateName;
            public IntPtr pvReserved = IntPtr.Zero;
            public int dwReserved;
            public int flagsEx;
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetOpenFileNameW([In, Out] NativeOpenFileName ofn);

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
        private static extern int CommDlgExtendedError();

        // -- Invisible topmost owner window, so the dialog always opens in front -------------

        private const int WsExTopmost = 0x00000008;
        private const int WsExToolWindow = 0x00000080;
        private const int WsPopup = unchecked((int)0x80000000);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowExW(
            int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);
    }
}