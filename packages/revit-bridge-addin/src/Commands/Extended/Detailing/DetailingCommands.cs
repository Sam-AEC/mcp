using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitBridge.Commands.ApiCompatibility;

namespace RevitBridge.Commands.Extended.Detailing
{
    /// <summary>
    /// Detailing and annotation commands - STUB IMPLEMENTATIONS
    /// Many detailing APIs changed in Revit 2024. Use Universal Reflection API instead.
    /// </summary>
    public static class DetailingCommands
    {
        #region 1. Create Detail Line

        public static object CreateDetailLine(UIApplication app, JsonElement payload)
        {
            return ApiVersionHelper.GetUnavailableFeatureResponse(
                "Detail Line Creation",
                "Use revit.invoke_method with DetailCurve.Create()");
        }

        #endregion

        #region 2. Create Detail Arc

        public static object CreateDetailArc(UIApplication app, JsonElement payload)
        {
            return ApiVersionHelper.GetUnavailableFeatureResponse(
                "Detail Arc Creation",
                "Use revit.invoke_method with DetailCurve.Create()");
        }

        #endregion

        #region 3. Create Filled Region

        public static object CreateFilledRegion(UIApplication app, JsonElement payload)
        {
            return ApiVersionHelper.GetUnavailableFeatureResponse(
                "Filled Region",
                "Use revit.invoke_method with FilledRegion.Create()");
        }

        #endregion

        #region 4. Create Masking Region

        public static object CreateMaskingRegion(UIApplication app, JsonElement payload)
        {
            return ApiVersionHelper.GetUnavailableFeatureResponse(
                "Masking Region",
                "Use revit.invoke_method with MaskingRegion.Create()");
        }

        #endregion

        #region 5. List Filled Region Types

        public static object ListFilledRegionTypes(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            try
            {
                var filledRegionTypes = new FilteredElementCollector(doc)
                    .OfClass(typeof(FilledRegionType))
                    .Cast<FilledRegionType>()
                    .Select(frt => new
                    {
                        id = (int)frt.Id.Value,
                        name = frt.Name,
                        familyName = frt.FamilyName
                    })
                    .ToList();

                return new
                {
                    count = filledRegionTypes.Count,
                    filledRegionTypes = filledRegionTypes
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }

        #endregion

        #region 6. Create Detail Component

        public static object CreateDetailComponent(UIApplication app, JsonElement payload)
        {
            return ApiVersionHelper.GetUnavailableFeatureResponse(
                "Detail Component",
                "Use revit.place_family_instance for detail components");
        }

        #endregion

        #region 7. List Detail Components

        public static object ListDetailComponents(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            var detailComponents = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_DetailComponents)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Select(fs => new
                {
                    id = (int)fs.Id.Value,
                    name = fs.Name,
                    familyName = fs.FamilyName
                })
                .ToList();

            return new
            {
                count = detailComponents.Count,
                detailComponents = detailComponents
            };
        }

        #endregion

        #region 8. Create Insulation

        public static object CreateInsulation(UIApplication app, JsonElement payload)
        {
            return ApiVersionHelper.GetUnavailableFeatureResponse(
                "Insulation Creation",
                "Use revit.invoke_method with Insulation.Create()");
        }

        #endregion

        #region 9. List Line Styles

        public static object ListLineStyles(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            var lineStyles = new FilteredElementCollector(doc)
                .OfClass(typeof(GraphicsStyle))
                .Cast<GraphicsStyle>()
                .Where(gs => gs.GraphicsStyleCategory?.Parent != null &&
                            gs.GraphicsStyleCategory.Parent.Name == "Lines")
                .Select(gs => new
                {
                    id = (int)gs.Id.Value,
                    name = gs.Name,
                    category = gs.GraphicsStyleCategory?.Name
                })
                .ToList();

            return new
            {
                count = lineStyles.Count,
                lineStyles = lineStyles
            };
        }

        #endregion

        #region 10. Set Detail Line Style

        public static object SetDetailLineStyle(UIApplication app, JsonElement payload)
        {
            return ApiVersionHelper.GetUnavailableFeatureResponse(
                "Set Detail Line Style",
                "Use revit.set_parameter_value to change line style");
        }

        #endregion

        #region 11. Get Detail Item Bounding Box

        public static object GetDetailItemBoundingBox(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int elementId = payload.GetProperty("element_id").GetInt32();

            Element element = doc.GetElement(new ElementId(elementId));
            if (element == null)
            {
                throw new Exception($"Element {elementId} not found");
            }

            BoundingBoxXYZ bbox = element.get_BoundingBox(null);
            if (bbox == null)
            {
                return new
                {
                    elementId = elementId,
                    hasBoundingBox = false,
                    message = "Element has no bounding box"
                };
            }

            return new
            {
                elementId = elementId,
                hasBoundingBox = true,
                min = new { x = bbox.Min.X, y = bbox.Min.Y, z = bbox.Min.Z },
                max = new { x = bbox.Max.X, y = bbox.Max.Y, z = bbox.Max.Z }
            };
        }

        #endregion

        #region 12. Create Repeating Detail

        public static object CreateRepeatingDetail(UIApplication app, JsonElement payload)
        {
            return ApiVersionHelper.GetUnavailableFeatureResponse(
                "Repeating Detail",
                "Use revit.invoke_method with RepeatingDetailCurve.Create()");
        }

        #endregion

        #region 13. Create Breakline

        public static object CreateBreakline(UIApplication app, JsonElement payload)
        {
            return ApiVersionHelper.GetUnavailableFeatureResponse(
                "Breakline",
                "Use revit.create_detail_line for breaklines");
        }

        #endregion

        #region 14. List Detailing Symbols

        public static object ListDetailingSymbols(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            var detailSymbols = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericAnnotation)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Select(fs => new
                {
                    id = (int)fs.Id.Value,
                    name = fs.Name,
                    familyName = fs.FamilyName
                })
                .ToList();

            return new
            {
                count = detailSymbols.Count,
                symbols = detailSymbols
            };
        }

        #endregion
    }
}
