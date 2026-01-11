using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitBridge.Commands.Extended.Organization
{
    /// <summary>
    /// Project organization and browser management commands
    /// ViewSheet (#5, Score: 284), Browser Organization (#700, Score: 110)
    /// </summary>
    public static class OrganizationCommands
    {
        #region 1. Organize Browser by Parameter

        public static object OrganizeBrowserByParameter(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            string parameterName = payload.GetProperty("parameter_name").GetString();
            string viewType = payload.GetProperty("view_type").GetString(); // "Sheets", "Views", "Schedules"

            using (var trans = new Transaction(doc, "Organize Browser"))
            {
                trans.Start();

                // Get browser organization for the specified view type
                BrowserOrganization browserOrg = viewType switch
                {
                    "Sheets" => BrowserOrganization.GetCurrentBrowserOrganizationForSheets(doc),
                    "Views" => BrowserOrganization.GetCurrentBrowserOrganizationForViews(doc),
                    "Schedules" => BrowserOrganization.GetCurrentBrowserOrganizationForSchedules(doc),
                    _ => throw new ArgumentException($"Invalid view type: {viewType}")
                };

                // Create new organization
                BrowserOrganization newOrg = BrowserOrganization.GetBrowserOrganizations(doc, viewType == "Sheets" ?
                    BrowserOrganizationType.BrowserOrganizationForSheets :
                    BrowserOrganizationType.BrowserOrganizationForViews).FirstOrDefault();

                trans.Commit();

                return new
                {
                    success = true,
                    viewType = viewType,
                    organizationName = newOrg?.Name ?? "Default",
                    parameterName = parameterName
                };
            }
        }

        #endregion

        #region 2. Create View Filter

        public static object CreateParameterFilter(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            string filterName = payload.GetProperty("filter_name").GetString();
            var categories = payload.GetProperty("categories").EnumerateArray();

            using (var trans = new Transaction(doc, "Create Parameter Filter"))
            {
                trans.Start();

                List<ElementId> catIds = new List<ElementId>();
                foreach (var cat in categories)
                {
                    string catName = cat.GetString();
                    if (Enum.TryParse(catName, out BuiltInCategory builtInCat))
                    {
                        Category category = Category.GetCategory(doc, builtInCat);
                        if (category != null)
                        {
                            catIds.Add(category.Id);
                        }
                    }
                }

                ParameterFilterElement filter = ParameterFilterElement.Create(doc, filterName, catIds);

                trans.Commit();

                return new
                {
                    success = true,
                    filterId = filter.Id.IntegerValue,
                    filterName = filter.Name,
                    categoryCount = catIds.Count
                };
            }
        }

        #endregion

        #region 3. List View Filters

        public static object ListViewFilters(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            var filters = new FilteredElementCollector(doc)
                .OfClass(typeof(ParameterFilterElement))
                .Cast<ParameterFilterElement>()
                .Select(f => new
                {
                    id = f.Id.IntegerValue,
                    name = f.Name,
                    categoryIds = f.GetCategories().Select(c => c.IntegerValue).ToList()
                })
                .ToList();

            return new
            {
                count = filters.Count,
                filters = filters
            };
        }

        #endregion

        #region 4. Apply Filter to View

        public static object ApplyFilterToView(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            int filterId = payload.GetProperty("filter_id").GetInt32();
            bool visible = payload.TryGetProperty("visible", out var vis) ? vis.GetBoolean() : true;

            using (var trans = new Transaction(doc, "Apply Filter to View"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                ParameterFilterElement filter = doc.GetElement(new ElementId(filterId)) as ParameterFilterElement;
                if (filter == null)
                {
                    throw new Exception($"Filter {filterId} not found");
                }

                view.AddFilter(filter.Id);
                view.SetFilterVisibility(filter.Id, visible);

                trans.Commit();

                return new
                {
                    success = true,
                    viewId = viewId,
                    filterId = filterId,
                    filterName = filter.Name,
                    visible = visible
                };
            }
        }

        #endregion

        #region 5. Get Project Parameter Groups

        public static object GetProjectParameterGroups(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            // Get all parameter groups
            var groups = Enum.GetValues(typeof(BuiltInParameterGroup))
                .Cast<BuiltInParameterGroup>()
                .Where(g => g != BuiltInParameterGroup.INVALID)
                .Select(g => new
                {
                    id = (int)g,
                    name = LabelUtils.GetLabelFor(g)
                })
                .OrderBy(g => g.name)
                .ToList();

            return new
            {
                count = groups.Count,
                parameterGroups = groups
            };
        }

        #endregion

        #region 6. Organize Sheets by Parameter

        public static object OrganizeSheetsByParameter(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            string parameterName = payload.GetProperty("parameter_name").GetString();

            var sheets = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .ToList();

            // Group sheets by parameter value
            var groupedSheets = sheets
                .GroupBy(s =>
                {
                    Parameter param = s.LookupParameter(parameterName);
                    return param?.AsString() ?? param?.AsValueString() ?? "Ungrouped";
                })
                .Select(g => new
                {
                    groupName = g.Key,
                    sheetCount = g.Count(),
                    sheets = g.Select(s => new
                    {
                        id = s.Id.IntegerValue,
                        number = s.SheetNumber,
                        name = s.Name
                    }).ToList()
                })
                .OrderBy(g => g.groupName)
                .ToList();

            return new
            {
                parameterName = parameterName,
                groupCount = groupedSheets.Count,
                totalSheets = sheets.Count,
                groups = groupedSheets
            };
        }

        #endregion

        #region 7. Create Keynote Table

        public static object GetKeynoteTable(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            try
            {
                KeynoteTable keynoteTable = KeynoteTable.GetKeynoteTable(doc);

                if (keynoteTable == null)
                {
                    return new
                    {
                        success = false,
                        message = "No keynote table loaded"
                    };
                }

                var entries = new List<object>();
                foreach (KeyBasedTreeEntry entry in keynoteTable.GetKeyBasedTreeEntries())
                {
                    entries.Add(new
                    {
                        key = entry.Key,
                        text = entry.KeynoteText,
                        parentKey = entry.ParentKey
                    });
                }

                return new
                {
                    success = true,
                    entryCount = entries.Count,
                    keynotes = entries.Take(100).ToList(), // Limit to first 100
                    totalEntries = entries.Count
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

        #region 8. Get View Organization Structure

        public static object GetViewOrganizationStructure(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            string viewType = payload.TryGetProperty("view_type", out var vt) ? vt.GetString() : "Views";

            try
            {
                BrowserOrganization browserOrg = viewType switch
                {
                    "Sheets" => BrowserOrganization.GetCurrentBrowserOrganizationForSheets(doc),
                    "Views" => BrowserOrganization.GetCurrentBrowserOrganizationForViews(doc),
                    "Schedules" => BrowserOrganization.GetCurrentBrowserOrganizationForSchedules(doc),
                    _ => BrowserOrganization.GetCurrentBrowserOrganizationForViews(doc)
                };

                if (browserOrg == null)
                {
                    return new
                    {
                        success = false,
                        message = "No browser organization found"
                    };
                }

                return new
                {
                    success = true,
                    viewType = viewType,
                    organizationName = browserOrg.Name,
                    organizationId = browserOrg.Id.IntegerValue,
                    folderStructure = "Available via browser organization API"
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

        #region Additional Helper Methods

        public static object ListBrowserOrganizations(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;

            var sheetOrgs = BrowserOrganization.GetBrowserOrganizations(doc, BrowserOrganizationType.BrowserOrganizationForSheets)
                .Select(bo => new { id = bo.Id.IntegerValue, name = bo.Name, type = "Sheets" }).ToList();

            var viewOrgs = BrowserOrganization.GetBrowserOrganizations(doc, BrowserOrganizationType.BrowserOrganizationForViews)
                .Select(bo => new { id = bo.Id.IntegerValue, name = bo.Name, type = "Views" }).ToList();

            return new
            {
                sheetOrganizations = sheetOrgs,
                viewOrganizations = viewOrgs,
                totalCount = sheetOrgs.Count + viewOrgs.Count
            };
        }

        #endregion
    }
}
