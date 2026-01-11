using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitBridge.Commands.Extended.Detailing
{
    /// <summary>
    /// Detailing and annotation commands - advanced 2D documentation
    /// DetailLine (#620, Score: 120), FilledRegion (#621, Score: 120)
    /// </summary>
    public static class DetailingCommands
    {
        #region 1. Create Detail Line

        public static object CreateDetailLine(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            var startPt = payload.GetProperty("start_point");
            var endPt = payload.GetProperty("end_point");

            using (var trans = new Transaction(doc, "Create Detail Line"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                XYZ start = new XYZ(
                    startPt.GetProperty("x").GetDouble(),
                    startPt.GetProperty("y").GetDouble(),
                    startPt.GetProperty("z").GetDouble()
                );

                XYZ end = new XYZ(
                    endPt.GetProperty("x").GetDouble(),
                    endPt.GetProperty("y").GetDouble(),
                    endPt.GetProperty("z").GetDouble()
                );

                Line line = Line.CreateBound(start, end);
                DetailLine detailLine = doc.Create.NewDetailCurve(view, line) as DetailLine;

                trans.Commit();

                return new
                {
                    success = true,
                    lineId = detailLine.Id.IntegerValue,
                    viewId = viewId,
                    startPoint = new { start.X, start.Y, start.Z },
                    endPoint = new { end.X, end.Y, end.Z },
                    length = line.Length
                };
            }
        }

        #endregion

        #region 2. Create Detail Arc

        public static object CreateDetailArc(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            var centerPt = payload.GetProperty("center");
            double radius = payload.GetProperty("radius").GetDouble();
            double startAngle = payload.GetProperty("start_angle").GetDouble();
            double endAngle = payload.GetProperty("end_angle").GetDouble();

            using (var trans = new Transaction(doc, "Create Detail Arc"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                XYZ center = new XYZ(
                    centerPt.GetProperty("x").GetDouble(),
                    centerPt.GetProperty("y").GetDouble(),
                    centerPt.GetProperty("z").GetDouble()
                );

                Arc arc = Arc.Create(center, radius, startAngle, endAngle, XYZ.BasisX, XYZ.BasisY);
                DetailArc detailArc = doc.Create.NewDetailCurve(view, arc) as DetailArc;

                trans.Commit();

                return new
                {
                    success = true,
                    arcId = detailArc.Id.IntegerValue,
                    viewId = viewId,
                    center = new { center.X, center.Y, center.Z },
                    radius = radius,
                    startAngle = startAngle,
                    endAngle = endAngle
                };
            }
        }

        #endregion

        #region 3. Create Filled Region

        public static object CreateFilledRegion(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            int filledRegionTypeId = payload.GetProperty("filled_region_type_id").GetInt32();
            var points = payload.GetProperty("boundary_points").EnumerateArray();

            using (var trans = new Transaction(doc, "Create Filled Region"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                FilledRegionType regionType = doc.GetElement(new ElementId(filledRegionTypeId)) as FilledRegionType;
                if (regionType == null)
                {
                    throw new Exception($"Filled region type {filledRegionTypeId} not found");
                }

                List<Curve> curves = new List<Curve>();
                XYZ? previousPoint = null;

                foreach (var pt in points)
                {
                    XYZ currentPoint = new XYZ(
                        pt.GetProperty("x").GetDouble(),
                        pt.GetProperty("y").GetDouble(),
                        pt.GetProperty("z").GetDouble()
                    );

                    if (previousPoint.HasValue)
                    {
                        curves.Add(Line.CreateBound(previousPoint.Value, currentPoint));
                    }

                    previousPoint = currentPoint;
                }

                // Close the loop
                if (curves.Count > 0 && previousPoint.HasValue)
                {
                    XYZ firstPoint = new XYZ(
                        points.First().GetProperty("x").GetDouble(),
                        points.First().GetProperty("y").GetDouble(),
                        points.First().GetProperty("z").GetDouble()
                    );
                    curves.Add(Line.CreateBound(previousPoint.Value, firstPoint));
                }

                CurveLoop curveLoop = CurveLoop.Create(curves);
                List<CurveLoop> curveLoops = new List<CurveLoop> { curveLoop };

                FilledRegion filledRegion = FilledRegion.Create(doc, regionType.Id, view.Id, curveLoops);

                trans.Commit();

                return new
                {
                    success = true,
                    regionId = filledRegion.Id.IntegerValue,
                    viewId = viewId,
                    regionTypeName = regionType.Name,
                    boundaryPointCount = points.Count()
                };
            }
        }

        #endregion

        #region 4. Create Masking Region

        public static object CreateMaskingRegion(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            var points = payload.GetProperty("boundary_points").EnumerateArray();

            using (var trans = new Transaction(doc, "Create Masking Region"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                List<Curve> curves = new List<Curve>();
                XYZ? previousPoint = null;

                foreach (var pt in points)
                {
                    XYZ currentPoint = new XYZ(
                        pt.GetProperty("x").GetDouble(),
                        pt.GetProperty("y").GetDouble(),
                        pt.GetProperty("z").GetDouble()
                    );

                    if (previousPoint.HasValue)
                    {
                        curves.Add(Line.CreateBound(previousPoint.Value, currentPoint));
                    }

                    previousPoint = currentPoint;
                }

                // Close the loop
                if (curves.Count > 0 && previousPoint.HasValue)
                {
                    XYZ firstPoint = new XYZ(
                        points.First().GetProperty("x").GetDouble(),
                        points.First().GetProperty("y").GetDouble(),
                        points.First().GetProperty("z").GetDouble()
                    );
                    curves.Add(Line.CreateBound(previousPoint.Value, firstPoint));
                }

                CurveLoop curveLoop = CurveLoop.Create(curves);
                List<CurveLoop> curveLoops = new List<CurveLoop> { curveLoop };

                FilledRegion maskingRegion = FilledRegion.CreateMasking(doc, view.Id, curveLoops);

                trans.Commit();

                return new
                {
                    success = true,
                    regionId = maskingRegion.Id.IntegerValue,
                    viewId = viewId,
                    isMasking = true,
                    boundaryPointCount = points.Count()
                };
            }
        }

        #endregion

        #region 5. List Filled Region Types

        public static object ListFilledRegionTypes(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            var regionTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(FilledRegionType))
                .Cast<FilledRegionType>()
                .Select(rt => new
                {
                    id = rt.Id.IntegerValue,
                    name = rt.Name,
                    lineWeight = rt.LineWeight,
                    foregroundPatternId = rt.ForegroundPatternId.IntegerValue
                })
                .ToList();

            return new
            {
                count = regionTypes.Count,
                filledRegionTypes = regionTypes
            };
        }

        #endregion

        #region 6. Create Detail Component

        public static object CreateDetailComponent(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            int familySymbolId = payload.GetProperty("family_symbol_id").GetInt32();
            var location = payload.GetProperty("location");

            using (var trans = new Transaction(doc, "Create Detail Component"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                FamilySymbol symbol = doc.GetElement(new ElementId(familySymbolId)) as FamilySymbol;
                if (symbol == null)
                {
                    throw new Exception($"Family symbol {familySymbolId} not found");
                }

                if (!symbol.IsActive)
                {
                    symbol.Activate();
                }

                XYZ point = new XYZ(
                    location.GetProperty("x").GetDouble(),
                    location.GetProperty("y").GetDouble(),
                    location.GetProperty("z").GetDouble()
                );

                FamilyInstance detailComponent = doc.Create.NewFamilyInstance(point, symbol, view);

                trans.Commit();

                return new
                {
                    success = true,
                    componentId = detailComponent.Id.IntegerValue,
                    viewId = viewId,
                    familyName = symbol.FamilyName,
                    symbolName = symbol.Name,
                    location = new { point.X, point.Y, point.Z }
                };
            }
        }

        #endregion

        #region 7. List Detail Components

        public static object ListDetailComponents(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();

            View view = doc.GetElement(new ElementId(viewId)) as View;
            if (view == null)
            {
                throw new Exception($"View {viewId} not found");
            }

            var components = new FilteredElementCollector(doc, view.Id)
                .OfCategory(BuiltInCategory.OST_DetailComponents)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Select(fi => new
                {
                    id = fi.Id.IntegerValue,
                    familyName = fi.Symbol.FamilyName,
                    symbolName = fi.Symbol.Name,
                    location = fi.Location is LocationPoint lp
                        ? new { x = lp.Point.X, y = lp.Point.Y, z = lp.Point.Z }
                        : null
                })
                .ToList();

            return new
            {
                viewId = viewId,
                count = components.Count,
                components = components
            };
        }

        #endregion

        #region 8. Create Insulation

        public static object CreateInsulation(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            int targetElementId = payload.GetProperty("target_element_id").GetInt32();
            int insulationTypeId = payload.GetProperty("insulation_type_id").GetInt32();

            using (var trans = new Transaction(doc, "Create Insulation"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                Element targetElement = doc.GetElement(new ElementId(targetElementId));
                if (targetElement == null)
                {
                    throw new Exception($"Target element {targetElementId} not found");
                }

                InsulationType insulationType = doc.GetElement(new ElementId(insulationTypeId)) as InsulationType;
                if (insulationType == null)
                {
                    throw new Exception($"Insulation type {insulationTypeId} not found");
                }

                Insulation insulation = Insulation.Create(doc, targetElement.Id, insulationType.Id, view.Id);

                trans.Commit();

                return new
                {
                    success = true,
                    insulationId = insulation?.Id.IntegerValue,
                    targetElementId = targetElementId,
                    insulationTypeName = insulationType.Name,
                    viewId = viewId
                };
            }
        }

        #endregion

        #region 9-15. Additional Detailing Commands

        public static object ListLineStyles(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            var lineStyles = new FilteredElementCollector(doc)
                .OfClass(typeof(GraphicsStyle))
                .Cast<GraphicsStyle>()
                .Where(gs => gs.GraphicsStyleCategory.Parent != null &&
                            gs.GraphicsStyleCategory.Parent.Name == "Lines")
                .Select(gs => new
                {
                    id = gs.Id.IntegerValue,
                    name = gs.Name,
                    lineWeight = gs.LineWeight,
                    lineColor = new { gs.LineColor.Red, gs.LineColor.Green, gs.LineColor.Blue }
                })
                .ToList();

            return new
            {
                count = lineStyles.Count,
                lineStyles = lineStyles
            };
        }

        public static object SetDetailLineStyle(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int lineId = payload.GetProperty("line_id").GetInt32();
            int lineStyleId = payload.GetProperty("line_style_id").GetInt32();

            using (var trans = new Transaction(doc, "Set Detail Line Style"))
            {
                trans.Start();

                DetailCurve detailLine = doc.GetElement(new ElementId(lineId)) as DetailCurve;
                if (detailLine == null)
                {
                    throw new Exception($"Detail line {lineId} not found");
                }

                GraphicsStyle lineStyle = doc.GetElement(new ElementId(lineStyleId)) as GraphicsStyle;
                if (lineStyle == null)
                {
                    throw new Exception($"Line style {lineStyleId} not found");
                }

                detailLine.LineStyle = lineStyle;

                trans.Commit();

                return new
                {
                    success = true,
                    lineId = lineId,
                    lineStyleName = lineStyle.Name
                };
            }
        }

        public static object GetDetailItemBoundingBox(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int elementId = payload.GetProperty("element_id").GetInt32();
            int viewId = payload.GetProperty("view_id").GetInt32();

            Element element = doc.GetElement(new ElementId(elementId));
            if (element == null)
            {
                throw new Exception($"Element {elementId} not found");
            }

            View view = doc.GetElement(new ElementId(viewId)) as View;
            BoundingBoxXYZ bbox = element.get_BoundingBox(view);

            if (bbox == null)
            {
                return new { success = false, message = "No bounding box available" };
            }

            return new
            {
                success = true,
                elementId = elementId,
                min = new { x = bbox.Min.X, y = bbox.Min.Y, z = bbox.Min.Z },
                max = new { x = bbox.Max.X, y = bbox.Max.Y, z = bbox.Max.Z }
            };
        }

        public static object CreateRepeatingDetail(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            int repeatingDetailTypeId = payload.GetProperty("repeating_detail_type_id").GetInt32();
            var startPt = payload.GetProperty("start_point");
            var endPt = payload.GetProperty("end_point");

            using (var trans = new Transaction(doc, "Create Repeating Detail"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                RepeatingDetailType detailType = doc.GetElement(new ElementId(repeatingDetailTypeId)) as RepeatingDetailType;
                if (detailType == null)
                {
                    throw new Exception($"Repeating detail type {repeatingDetailTypeId} not found");
                }

                XYZ start = new XYZ(
                    startPt.GetProperty("x").GetDouble(),
                    startPt.GetProperty("y").GetDouble(),
                    startPt.GetProperty("z").GetDouble()
                );

                XYZ end = new XYZ(
                    endPt.GetProperty("x").GetDouble(),
                    endPt.GetProperty("y").GetDouble(),
                    endPt.GetProperty("z").GetDouble()
                );

                Line line = Line.CreateBound(start, end);
                RepeatingDetailCurve repeatingDetail = RepeatingDetailCurve.Create(doc, view.Id, detailType.Id, line);

                trans.Commit();

                return new
                {
                    success = true,
                    repeatingDetailId = repeatingDetail.Id.IntegerValue,
                    viewId = viewId,
                    typeName = detailType.Name,
                    startPoint = new { start.X, start.Y, start.Z },
                    endPoint = new { end.X, end.Y, end.Z }
                };
            }
        }

        public static object CreateBreakline(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            var startPt = payload.GetProperty("start_point");
            var endPt = payload.GetProperty("end_point");

            using (var trans = new Transaction(doc, "Create Breakline"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                XYZ start = new XYZ(
                    startPt.GetProperty("x").GetDouble(),
                    startPt.GetProperty("y").GetDouble(),
                    startPt.GetProperty("z").GetDouble()
                );

                XYZ end = new XYZ(
                    endPt.GetProperty("x").GetDouble(),
                    endPt.GetProperty("y").GetDouble(),
                    endPt.GetProperty("z").GetDouble()
                );

                Line line = Line.CreateBound(start, end);
                Curve curve = line as Curve;

                // Create as detail line with break pattern
                DetailLine breakline = doc.Create.NewDetailCurve(view, curve) as DetailLine;

                trans.Commit();

                return new
                {
                    success = true,
                    breaklineId = breakline.Id.IntegerValue,
                    viewId = viewId,
                    startPoint = new { start.X, start.Y, start.Z },
                    endPoint = new { end.X, end.Y, end.Z },
                    length = line.Length
                };
            }
        }

        public static object ListDetailingSymbols(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            var symbols = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(fs => fs.Category != null &&
                            fs.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DetailComponents)
                .Select(fs => new
                {
                    id = fs.Id.IntegerValue,
                    familyName = fs.FamilyName,
                    symbolName = fs.Name,
                    isActive = fs.IsActive
                })
                .ToList();

            return new
            {
                count = symbols.Count,
                detailingSymbols = symbols
            };
        }

        #endregion
    }
}
