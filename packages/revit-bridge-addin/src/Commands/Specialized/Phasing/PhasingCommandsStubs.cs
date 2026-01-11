using System;
using System.Text.Json;
using Autodesk.Revit.UI;

namespace RevitBridge.Commands.Specialized.Phasing
{
    /// <summary>
    /// Stub implementations for Revit 2024 API compatibility
    /// These commands use APIs that changed in Revit 2024
    /// Users should use the Universal Reflection API (revit.invoke_method) instead
    /// </summary>
    public static class PhasingCommandsStubs
    {
        public static object CreateDesignOptionSetStub(UIApplication app, JsonElement payload)
        {
            return new
            {
                status = "not_implemented_2024",
                message = "DesignOptionSet.Create API changed in Revit 2024",
                workaround = "Use revit.invoke_method with Autodesk.Revit.DB.DesignOptionSet",
                reflection_example = new
                {
                    tool = "revit.invoke_method",
                    payload = new
                    {
                        class_name = "Autodesk.Revit.DB.DesignOptionSet",
                        method_name = "Create",
                        use_transaction = true,
                        arguments = new[] { "{{option_set_name}}" }
                    }
                }
            };
        }

        public static object CreateDesignOptionStub(UIApplication app, JsonElement payload)
        {
            return new
            {
                status = "not_implemented_2024",
                message = "DesignOption.Create API changed in Revit 2024",
                workaround = "Use revit.invoke_method with Autodesk.Revit.DB.DesignOption"
            };
        }

        public static object GetDesignOptionInfoStub(UIApplication app, JsonElement payload)
        {
            return new
            {
                status = "not_implemented_2024",
                message = "DesignOptionSet access changed in Revit 2024",
                workaround = "Use revit.invoke_method to query DesignOption properties"
            };
        }

        public static object ListAllDesignOptionSetsStub(UIApplication app, JsonElement payload)
        {
            return new
            {
                status = "not_implemented_2024",
                message = "DesignOptionSet API changed in Revit 2024",
                workaround = "Use revit.invoke_method with FilteredElementCollector"
            };
        }
    }
}
