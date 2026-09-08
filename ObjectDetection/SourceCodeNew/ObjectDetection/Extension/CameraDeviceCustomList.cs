using System;
using Peakboard.ExtensionKit;
using PeakboardExtensionObjectDetection.Camera;
using PeakboardExtensionObjectDetection.Inference;

namespace PeakboardExtensionObjectDetection.Extension
{
    /// <summary>
    /// The cameras attached to the machine that is running the board.
    ///
    /// CameraSource stays a number on purpose: a board is built in Designer on
    /// one machine and deployed to a Box or a BYOD device whose cameras nobody
    /// has seen yet, so a name chosen at design time would mean nothing at the
    /// far end. The number is the only portable contract.
    ///
    /// This list is how that number stops being a guess. Put it on a
    /// commissioning screen to see what the Box actually has, match a name to an
    /// index in Lua if the order shifts after a device is replugged, or read it
    /// over support to find out why camera 1 is not there.
    ///
    /// In Designer it lists the Designer PC's cameras. That is the wrong machine
    /// for a deployed board, which is exactly why the list exists at runtime.
    ///
    /// It is a passive observer: it never opens a device and never touches the
    /// detection engine, so it is safe to refresh while detection is running.
    /// </summary>
    [CustomListIcon("PeakboardExtensionObjectDetection.ObjectDetection.png")]
    public class CameraDeviceCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "ObjectDetectionCameras",
                Name = "Object Detection - Cameras",
                Description = "Video capture devices on the machine running the board. Index is the number to put in CameraSource.",
                PropertyInputPossible = false,
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Index", CustomListColumnTypes.Number),
                new CustomListColumn("Name", CustomListColumnTypes.String),
                new CustomListColumn("DevicePath", CustomListColumnTypes.String),
                new CustomListColumn("IsInUse", CustomListColumnTypes.Boolean),
                new CustomListColumn("Error", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();

            string error;
            var devices = CameraEnumerator.List(out error);

            if (!string.IsNullOrEmpty(error))
                Log?.Warning($"[ObjectDetection] Camera enumeration failed: {error}");

            // Which one the engine is streaming from, so a commissioning screen can
            // mark it. Only meaningful when CameraSource is an index rather than an
            // RTSP URL or a file.
            int running = -1;
            if (DetectionEngine.IsRunning)
                int.TryParse(DetectionEngine.CameraSourceName, out running);

            foreach (var d in devices)
            {
                items.Add(new CustomListObjectElement
                {
                    { "Index", (double)d.Index },
                    { "Name", d.Name ?? "" },
                    { "DevicePath", d.DevicePath ?? "" },
                    { "IsInUse", d.Index == running },
                    { "Error", "" },
                });
            }

            // A machine with no camera is a legitimate answer, but an empty table
            // is indistinguishable from "the list is broken". Say which it is.
            if (devices.Count == 0)
            {
                items.Add(new CustomListObjectElement
                {
                    { "Index", -1.0 },
                    { "Name", "" },
                    { "DevicePath", "" },
                    { "IsInUse", false },
                    { "Error", string.IsNullOrEmpty(error)
                        ? "No video capture devices found on this machine."
                        : error },
                });
            }

            return items;
        }

        protected override void SetupOverride(CustomListData data) { }
        protected override void CleanupOverride(CustomListData data) { }
    }
}
