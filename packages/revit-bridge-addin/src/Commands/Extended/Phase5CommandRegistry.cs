using System;
using System.Text.Json;
using Autodesk.Revit.UI;
using RevitBridge.Commands.Extended.Rendering;
using RevitBridge.Commands.Extended.Detailing;
using RevitBridge.Commands.Extended.Organization;
using RevitBridge.Commands.Extended.Performance;
using RevitBridge.Commands.Extended.DataExchange;

namespace RevitBridge.Commands.Extended
{
    /// <summary>
    /// Registry for Phase 5 commands (50 extended tools)
    /// These are advanced professional tools for specialized workflows
    /// </summary>
    public static class Phase5CommandRegistry
    {
        public static object Execute(UIApplication app, string tool, JsonElement payload)
        {
            return tool switch
            {
                // === RENDERING & VISUALIZATION (10 tools) ===
                "revit.get_render_settings" => RenderingCommands.GetRenderSettings(app, payload),
                "revit.set_render_quality" => RenderingCommands.SetRenderQuality(app, payload),
                "revit.get_camera_settings" => RenderingCommands.GetCameraSettings(app, payload),
                "revit.set_camera_position" => RenderingCommands.SetCameraPosition(app, payload),
                "revit.get_sun_settings" => RenderingCommands.GetSunSettings(app, payload),
                "revit.set_sun_position" => RenderingCommands.SetSunPosition(app, payload),
                "revit.get_lighting_scheme" => RenderingCommands.GetLightingScheme(app, payload),
                "revit.set_visual_style" => RenderingCommands.SetVisualStyle(app, payload),
                "revit.get_material_appearance" => RenderingCommands.GetMaterialAppearance(app, payload),
                "revit.set_material_appearance" => RenderingCommands.SetMaterialAppearance(app, payload),

                // === DETAILING & ANNOTATION (15 tools) ===
                "revit.create_detail_line" => DetailingCommands.CreateDetailLine(app, payload),
                "revit.create_detail_arc" => DetailingCommands.CreateDetailArc(app, payload),
                "revit.create_filled_region" => DetailingCommands.CreateFilledRegion(app, payload),
                "revit.create_masking_region" => DetailingCommands.CreateMaskingRegion(app, payload),
                "revit.list_filled_region_types" => DetailingCommands.ListFilledRegionTypes(app, payload),
                "revit.create_detail_component" => DetailingCommands.CreateDetailComponent(app, payload),
                "revit.list_detail_components" => DetailingCommands.ListDetailComponents(app, payload),
                "revit.create_insulation" => DetailingCommands.CreateInsulation(app, payload),
                "revit.list_line_styles" => DetailingCommands.ListLineStyles(app, payload),
                "revit.set_detail_line_style" => DetailingCommands.SetDetailLineStyle(app, payload),
                "revit.get_detail_item_bounding_box" => DetailingCommands.GetDetailItemBoundingBox(app, payload),
                "revit.create_repeating_detail" => DetailingCommands.CreateRepeatingDetail(app, payload),
                "revit.create_breakline" => DetailingCommands.CreateBreakline(app, payload),
                "revit.list_detailing_symbols" => DetailingCommands.ListDetailingSymbols(app, payload),

                // === PROJECT ORGANIZATION (8 tools) ===
                "revit.organize_browser_by_parameter" => OrganizationCommands.OrganizeBrowserByParameter(app, payload),
                "revit.create_parameter_filter" => OrganizationCommands.CreateParameterFilter(app, payload),
                "revit.list_view_filters" => OrganizationCommands.ListViewFilters(app, payload),
                "revit.apply_filter_to_view" => OrganizationCommands.ApplyFilterToView(app, payload),
                "revit.get_project_parameter_groups" => OrganizationCommands.GetProjectParameterGroups(app, payload),
                "revit.organize_sheets_by_parameter" => OrganizationCommands.OrganizeSheetsByParameter(app, payload),
                "revit.get_keynote_table" => OrganizationCommands.GetKeynoteTable(app, payload),
                "revit.get_view_organization_structure" => OrganizationCommands.GetViewOrganizationStructure(app, payload),

                // === PERFORMANCE & OPTIMIZATION (7 tools) ===
                "revit.purge_unused_elements" => PerformanceCommands.PurgeUnusedElements(app, payload),
                "revit.compact_file" => PerformanceCommands.CompactFile(app, payload),
                "revit.get_model_statistics" => PerformanceCommands.GetModelStatistics(app, payload),
                "revit.analyze_model_performance" => PerformanceCommands.AnalyzeModelPerformance(app, payload),
                "revit.get_warnings_summary" => PerformanceCommands.GetWarningsSummary(app, payload),
                "revit.audit_model" => PerformanceCommands.AuditModel(app, payload),
                "revit.optimize_view_performance" => PerformanceCommands.OptimizeViewPerformance(app, payload),

                // === ADVANCED IFC & DATA EXCHANGE (10 tools) ===
                "revit.get_ifc_export_configurations" => DataExchangeCommands.GetIFCExportConfigurations(app, payload),
                "revit.export_ifc_with_custom_settings" => DataExchangeCommands.ExportIFCWithCustomSettings(app, payload),
                "revit.get_ifc_property_sets" => DataExchangeCommands.GetIFCPropertySets(app, payload),
                "revit.set_ifc_properties" => DataExchangeCommands.SetIFCProperties(app, payload),
                "revit.get_classification_systems" => DataExchangeCommands.GetClassificationSystems(app, payload),
                "revit.export_cobie_data" => DataExchangeCommands.ExportCOBieData(app, payload),
                "revit.get_bcf_topics" => DataExchangeCommands.GetBCFTopics(app, payload),
                "revit.map_parameters_to_ifc" => DataExchangeCommands.MapParametersToIFC(app, payload),
                "revit.get_data_exchange_settings" => DataExchangeCommands.GetDataExchangeSettings(app, payload),
                "revit.validate_ifc_export" => DataExchangeCommands.ValidateIFCExport(app, payload),

                _ => null // Return null to let main factory handle unknown commands
            };
        }

        /// <summary>
        /// Get list of all Phase 5 command names for documentation/validation
        /// </summary>
        public static string[] GetCommandNames()
        {
            return new[]
            {
                // Rendering (10)
                "revit.get_render_settings",
                "revit.set_render_quality",
                "revit.get_camera_settings",
                "revit.set_camera_position",
                "revit.get_sun_settings",
                "revit.set_sun_position",
                "revit.get_lighting_scheme",
                "revit.set_visual_style",
                "revit.get_material_appearance",
                "revit.set_material_appearance",

                // Detailing (14 - one reserved for future)
                "revit.create_detail_line",
                "revit.create_detail_arc",
                "revit.create_filled_region",
                "revit.create_masking_region",
                "revit.list_filled_region_types",
                "revit.create_detail_component",
                "revit.list_detail_components",
                "revit.create_insulation",
                "revit.list_line_styles",
                "revit.set_detail_line_style",
                "revit.get_detail_item_bounding_box",
                "revit.create_repeating_detail",
                "revit.create_breakline",
                "revit.list_detailing_symbols",

                // Organization (8)
                "revit.organize_browser_by_parameter",
                "revit.create_parameter_filter",
                "revit.list_view_filters",
                "revit.apply_filter_to_view",
                "revit.get_project_parameter_groups",
                "revit.organize_sheets_by_parameter",
                "revit.get_keynote_table",
                "revit.get_view_organization_structure",

                // Performance (7)
                "revit.purge_unused_elements",
                "revit.compact_file",
                "revit.get_model_statistics",
                "revit.analyze_model_performance",
                "revit.get_warnings_summary",
                "revit.audit_model",
                "revit.optimize_view_performance",

                // Data Exchange (10)
                "revit.get_ifc_export_configurations",
                "revit.export_ifc_with_custom_settings",
                "revit.get_ifc_property_sets",
                "revit.set_ifc_properties",
                "revit.get_classification_systems",
                "revit.export_cobie_data",
                "revit.get_bcf_topics",
                "revit.map_parameters_to_ifc",
                "revit.get_data_exchange_settings",
                "revit.validate_ifc_export"
            };
        }
    }
}
