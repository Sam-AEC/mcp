using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitBridge.Commands.Extended.Performance
{
    /// <summary>
    /// Performance optimization and file management commands
    /// Document (#1, Score: 295), Performance Advisor (#800, Score: 100)
    /// </summary>
    public static class PerformanceCommands
    {
        #region 1. Purge Unused Elements

        public static object PurgeUnusedElements(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            bool purgeAll = payload.TryGetProperty("purge_all", out var pa) ? pa.GetBoolean() : false;

            using (var trans = new Transaction(doc, "Purge Unused"))
            {
                trans.Start();

                int purgedCount = 0;

                // Get all element ids that can be deleted
                ICollection<ElementId> unusedElementIds = new List<ElementId>();

                // Purge unused families
                var unusedFamilies = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .Where(f =>
                    {
                        var symbols = f.GetFamilySymbolIds();
                        return symbols.All(sid =>
                        {
                            var collector = new FilteredElementCollector(doc)
                                .OfClass(typeof(FamilyInstance))
                                .Where(fi => ((FamilyInstance)fi).Symbol.Id == sid);
                            return !collector.Any();
                        });
                    })
                    .ToList();

                foreach (var family in unusedFamilies)
                {
                    try
                    {
                        doc.Delete(family.Id);
                        purgedCount++;
                    }
                    catch { /* Ignore if can't delete */ }
                }

                // Purge unused views (if requested)
                if (purgeAll)
                {
                    var unusedViews = new FilteredElementCollector(doc)
                        .OfClass(typeof(View))
                        .Cast<View>()
                        .Where(v => !v.IsTemplate && v.ViewType != ViewType.ProjectBrowser)
                        .ToList();

                    // Only delete views not on sheets
                    foreach (var view in unusedViews)
                    {
                        try
                        {
                            Viewport vp = doc.GetViewports(view.Id).FirstOrDefault();
                            if (vp == null) // Not on a sheet
                            {
                                doc.Delete(view.Id);
                                purgedCount++;
                            }
                        }
                        catch { /* Ignore */ }
                    }
                }

                trans.Commit();

                return new
                {
                    success = true,
                    purgedCount = purgedCount,
                    purgeAll = purgeAll
                };
            }
        }

        #endregion

        #region 2. Compact File

        public static object CompactFile(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            string outputPath = payload.TryGetProperty("output_path", out var op) ? op.GetString() : null;

            try
            {
                if (doc.IsModified)
                {
                    return new
                    {
                        success = false,
                        message = "Document has unsaved changes. Save before compacting."
                    };
                }

                string originalPath = doc.PathName;
                long originalSize = new FileInfo(originalPath).Length;

                // Compact by saving
                if (!string.IsNullOrEmpty(outputPath))
                {
                    doc.SaveAs(outputPath, new SaveAsOptions { Compact = true });
                }
                else
                {
                    doc.Save(new SaveOptions { Compact = true });
                }

                long newSize = new FileInfo(string.IsNullOrEmpty(outputPath) ? originalPath : outputPath).Length;
                double savings = (1 - ((double)newSize / originalSize)) * 100;

                return new
                {
                    success = true,
                    originalSize = originalSize,
                    newSize = newSize,
                    savingsPercent = Math.Round(savings, 2),
                    outputPath = string.IsNullOrEmpty(outputPath) ? originalPath : outputPath
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    error = ex.Message
                };
            }
        }

        #endregion

        #region 3. Get Model Statistics

        public static object GetModelStatistics(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            var statistics = new
            {
                // Document info
                filePath = doc.PathName,
                title = doc.Title,
                isWorkshared = doc.IsWorkshared,
                isModified = doc.IsModified,
                fileSize = !string.IsNullOrEmpty(doc.PathName) ? new FileInfo(doc.PathName).Length : 0,

                // Element counts
                totalElements = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .GetElementCount(),

                elementTypes = new FilteredElementCollector(doc)
                    .WhereElementIsElementType()
                    .GetElementCount(),

                families = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .GetElementCount(),

                views = new FilteredElementCollector(doc)
                    .OfClass(typeof(View))
                    .GetElementCount(),

                sheets = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSheet))
                    .GetElementCount(),

                materials = new FilteredElementCollector(doc)
                    .OfClass(typeof(Material))
                    .GetElementCount(),

                // Category breakdown
                categoryBreakdown = GetCategoryBreakdown(doc)
            };

            return statistics;
        }

        private static object GetCategoryBreakdown(Document doc)
        {
            var categories = doc.Settings.Categories;
            var breakdown = new Dictionary<string, int>();

            foreach (Category cat in categories)
            {
                if (cat.CategoryType == CategoryType.Model)
                {
                    int count = new FilteredElementCollector(doc)
                        .OfCategoryId(cat.Id)
                        .WhereElementIsNotElementType()
                        .GetElementCount();

                    if (count > 0)
                    {
                        breakdown[cat.Name] = count;
                    }
                }
            }

            return breakdown.OrderByDescending(kvp => kvp.Value).Take(10).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        #endregion

        #region 4. Analyze Model Performance

        public static object AnalyzeModelPerformance(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            var issues = new List<object>();

            // Check for heavy views
            var views = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate)
                .ToList();

            int heavyViews = 0;
            foreach (var view in views)
            {
                try
                {
                    var elementsInView = new FilteredElementCollector(doc, view.Id)
                        .WhereElementIsNotElementType()
                        .GetElementCount();

                    if (elementsInView > 5000)
                    {
                        heavyViews++;
                    }
                }
                catch { /* View might not support filtering */ }
            }

            if (heavyViews > 0)
            {
                issues.Add(new
                {
                    type = "Heavy Views",
                    count = heavyViews,
                    severity = "Warning",
                    recommendation = "Consider simplifying views or using view templates"
                });
            }

            // Check for unused families
            var totalFamilies = new FilteredElementCollector(doc)
                .OfClass(typeof(Family))
                .GetElementCount();

            if (totalFamilies > 500)
            {
                issues.Add(new
                {
                    type = "High Family Count",
                    count = totalFamilies,
                    severity = "Info",
                    recommendation = "Consider purging unused families"
                });
            }

            // Check for warnings
            var warnings = doc.GetWarnings();
            if (warnings.Count > 100)
            {
                issues.Add(new
                {
                    type = "Excessive Warnings",
                    count = warnings.Count,
                    severity = "Warning",
                    recommendation = "Review and resolve model warnings"
                });
            }

            // Check file size
            if (!string.IsNullOrEmpty(doc.PathName))
            {
                long fileSizeMB = new FileInfo(doc.PathName).Length / (1024 * 1024);
                if (fileSizeMB > 500)
                {
                    issues.Add(new
                    {
                        type = "Large File Size",
                        count = fileSizeMB,
                        severity = "Warning",
                        recommendation = "Consider compacting file or splitting model"
                    });
                }
            }

            return new
            {
                issuesFound = issues.Count,
                issues = issues,
                overallHealth = issues.Count == 0 ? "Good" : issues.Count < 3 ? "Fair" : "Needs Attention"
            };
        }

        #endregion

        #region 5. Get Warnings Summary

        public static object GetWarningsSummary(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            var warnings = doc.GetWarnings();

            var summary = warnings
                .GroupBy(w => w.GetDescriptionText())
                .Select(g => new
                {
                    description = g.Key,
                    count = g.Count(),
                    severity = g.First().GetSeverity().ToString(),
                    affectedElements = g.SelectMany(w => w.GetFailingElements()).Select(id => id.IntegerValue).Distinct().Take(10).ToList()
                })
                .OrderByDescending(w => w.count)
                .ToList();

            return new
            {
                totalWarnings = warnings.Count,
                uniqueWarningTypes = summary.Count,
                warnings = summary
            };
        }

        #endregion

        #region 6. Audit Model

        public static object AuditModel(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            return new
            {
                status = "audit_not_supported",
                message = "Audit requires file to be opened with audit flag. Use revit.invoke_method for advanced audit operations.",
                workaround = new
                {
                    tool = "revit.invoke_method",
                    info = "Audit is performed during file open with ModelPathUtils.OpenOptions"
                },
                alternativeInfo = "Use revit.get_warnings_summary and revit.analyze_model_performance instead"
            };
        }

        #endregion

        #region 7. Optimize View Performance

        public static object OptimizeViewPerformance(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();

            using (var trans = new Transaction(doc, "Optimize View"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                var optimizations = new List<string>();

                // Set detail level to Coarse if not already
                if (view.DetailLevel != ViewDetailLevel.Coarse)
                {
                    view.DetailLevel = ViewDetailLevel.Coarse;
                    optimizations.Add("Set detail level to Coarse");
                }

                // Disable shadows if enabled
                if (view.DisplayStyle == DisplayStyle.Realistic || view.DisplayStyle == DisplayStyle.Shading)
                {
                    view.DisplayStyle = DisplayStyle.Shading;
                    optimizations.Add("Simplified display style");
                }

                // Apply view template for consistency
                // This would require a template ID

                trans.Commit();

                return new
                {
                    success = true,
                    viewId = viewId,
                    viewName = view.Name,
                    optimizationsApplied = optimizations,
                    elementCount = new FilteredElementCollector(doc, view.Id)
                        .WhereElementIsNotElementType()
                        .GetElementCount()
                };
            }
        }

        #endregion
    }
}
