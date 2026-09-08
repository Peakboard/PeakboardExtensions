using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace PeakboardExtensionObjectDetection.Camera
{
    public sealed class CameraDevice
    {
        /// <summary>The number to put in CameraSource.</summary>
        public int Index { get; set; }
        public string Name { get; set; }
        public string DevicePath { get; set; }
    }

    /// <summary>
    /// Lists the video capture devices attached to *this* machine.
    ///
    /// Deliberately enumerated through DirectShow's video input category rather
    /// than WMI. OpenCV's VideoCapture(index, DSHOW) walks that same category in
    /// that same order, so position N here is the number to type into
    /// CameraSource. A WMI query returns the right devices in an order that is
    /// not guaranteed to agree, which would produce a list that looks correct
    /// and selects the wrong camera.
    ///
    /// Nothing here opens a device, so enumerating is safe while the detection
    /// engine is streaming from one of them.
    /// </summary>
    public static class CameraEnumerator
    {
        private static readonly Guid CLSID_SystemDeviceEnum =
            new Guid("62BE5D10-60EB-11d0-BD3B-00A0C911CE86");
        private static readonly Guid CLSID_VideoInputDeviceCategory =
            new Guid("860BB310-5D01-11d0-BD3B-00A0C911CE86");

        [ComImport, Guid("29840822-5B84-11D0-BD3B-00A0C911CE86"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ICreateDevEnum
        {
            [PreserveSig]
            int CreateClassEnumerator([In] ref Guid pType, out IEnumMoniker ppEnumMoniker, int dwFlags);
        }

        [ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyBag
        {
            [PreserveSig]
            int Read([MarshalAs(UnmanagedType.LPWStr)] string pszPropName,
                     [In, Out, MarshalAs(UnmanagedType.Struct)] ref object pVar,
                     IntPtr pErrorLog);

            [PreserveSig]
            int Write([MarshalAs(UnmanagedType.LPWStr)] string pszPropName,
                      [In, MarshalAs(UnmanagedType.Struct)] ref object pVar);
        }

        /// <summary>
        /// Never throws. A machine with no camera, or no DirectShow at all,
        /// returns an empty list -- which is a legitimate answer and must not
        /// take the data source down with it.
        /// </summary>
        public static List<CameraDevice> List(out string error)
        {
            error = "";
            var devices = new List<CameraDevice>();
            object devEnumObj = null;
            IEnumMoniker enumMoniker = null;

            try
            {
                var t = Type.GetTypeFromCLSID(CLSID_SystemDeviceEnum);
                if (t == null) { error = "DirectShow device enumerator is not registered."; return devices; }

                devEnumObj = Activator.CreateInstance(t);
                var devEnum = devEnumObj as ICreateDevEnum;
                if (devEnum == null) { error = "DirectShow device enumerator is unavailable."; return devices; }

                var category = CLSID_VideoInputDeviceCategory;
                // Returns S_FALSE (1) with a null enumerator when the category is
                // empty -- that is "no cameras", not a failure.
                int hr = devEnum.CreateClassEnumerator(ref category, out enumMoniker, 0);
                if (hr != 0 || enumMoniker == null) return devices;

                var batch = new IMoniker[1];
                int index = 0;

                while (enumMoniker.Next(1, batch, IntPtr.Zero) == 0)
                {
                    var moniker = batch[0];
                    if (moniker == null) continue;

                    try
                    {
                        devices.Add(new CameraDevice
                        {
                            Index = index,
                            Name = ReadProperty(moniker, "FriendlyName") ?? $"Camera {index}",
                            DevicePath = ReadProperty(moniker, "DevicePath") ?? "",
                        });
                    }
                    finally
                    {
                        Marshal.ReleaseComObject(moniker);
                        batch[0] = null;
                    }

                    // Incremented for every device DirectShow reports, including
                    // any whose properties could not be read: skipping one would
                    // shift every later index and hand back numbers that open the
                    // wrong camera.
                    index++;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            finally
            {
                if (enumMoniker != null) Marshal.ReleaseComObject(enumMoniker);
                if (devEnumObj != null) Marshal.ReleaseComObject(devEnumObj);
            }

            return devices;
        }

        private static string ReadProperty(IMoniker moniker, string name)
        {
            object bagObj = null;
            try
            {
                var bagId = typeof(IPropertyBag).GUID;
                moniker.BindToStorage(null, null, ref bagId, out bagObj);
                var bag = bagObj as IPropertyBag;
                if (bag == null) return null;

                object value = null;
                return bag.Read(name, ref value, IntPtr.Zero) == 0 ? value as string : null;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (bagObj != null) Marshal.ReleaseComObject(bagObj);
            }
        }
    }
}
