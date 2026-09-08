using System;
using Peakboard.ExtensionKit;
using PeakboardExtensionObjectDetection.Data;

namespace PeakboardExtensionObjectDetection.Extension
{
    [CustomListIcon("PeakboardExtensionObjectDetection.ObjectDetection.png")]
    public class ModelCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "ObjectDetectionModels",
                Name = "Object Detection - Models",
                Description = "Lists all available YOLO models (pretrained, resources, and custom).",
                PropertyInputPossible = false,
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Name", CustomListColumnTypes.String),
                new CustomListColumn("Source", CustomListColumnTypes.String),
                new CustomListColumn("SizeMB", CustomListColumnTypes.Number),
                new CustomListColumn("OnnxPath", CustomListColumnTypes.String),
                new CustomListColumn("ClassesPath", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();
            var manager = new ModelManager();

            foreach (var model in manager.GetAvailableModels())
            {
                items.Add(new CustomListObjectElement
                {
                    { "Name", model.Name },
                    { "Source", model.Source },
                    { "SizeMB", Math.Round(model.SizeBytes / (1024.0 * 1024.0), 2) },
                    { "OnnxPath", model.OnnxPath },
                    { "ClassesPath", model.ClassesPath },
                });
            }

            return items;
        }

        protected override void SetupOverride(CustomListData data) { }
        protected override void CleanupOverride(CustomListData data) { }
    }
}
