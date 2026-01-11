using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace RevitBridge.Commands.ApiCompatibility
{
    /// <summary>
    /// Helper class for handling API compatibility across Revit versions
    /// Provides version-specific implementations and fallbacks
    /// </summary>
    public static class ApiVersionHelper
    {
        /// <summary>
        /// Create ElementId compatible with both Revit 2024+ and earlier versions
        /// </summary>
        public static ElementId CreateElementId(int id)
        {
#if REVIT_2024 || REVIT_2025
            return new ElementId((long)id);
#else
            return new ElementId(id);
#endif
        }

        /// <summary>
        /// Create ElementId from long (Revit 2024+)
        /// </summary>
        public static ElementId CreateElementId(long id)
        {
            return new ElementId(id);
        }

        /// <summary>
        /// Get integer value from ElementId safely
        /// </summary>
        public static int GetIntegerValue(ElementId id)
        {
#if REVIT_2024 || REVIT_2025
            return (int)id.Value;
#else
            return id.IntegerValue;
#endif
        }

        /// <summary>
        /// Get long value from ElementId (Revit 2024+)
        /// </summary>
        public static long GetLongValue(ElementId id)
        {
            return id.Value;
        }

        /// <summary>
        /// Check if design options API is available
        /// </summary>
        public static bool IsDesignOptionsAvailable()
        {
#if REVIT_2024 || REVIT_2025
            return false; // API changed in 2024
#else
            return true;
#endif
        }

        /// <summary>
        /// Get stub response for unavailable features
        /// </summary>
        public static object GetUnavailableFeatureResponse(string featureName, string reflectionExample = null)
        {
            return new
            {
                status = "api_unavailable",
                feature = featureName,
                message = $"{featureName} API changed in Revit 2024. Use Universal Reflection API instead.",
                workaround = "Use revit.invoke_method",
                reflection_example = reflectionExample ?? new
                {
                    tool = "revit.invoke_method",
                    payload = new
                    {
                        class_name = "Autodesk.Revit.DB.DesignOption",
                        method_name = "GetRelevantMethod",
                        arguments = new object[] { }
                    }
                }
            };
        }

        /// <summary>
        /// Check if Phase.SequenceNumber is available
        /// </summary>
        public static bool IsPhaseSequenceNumberAvailable()
        {
#if REVIT_2024 || REVIT_2025
            return false;
#else
            return true;
#endif
        }

        /// <summary>
        /// Safe cast to Room with proper namespace handling
        /// </summary>
        public static Room SafeCastToRoom(Element element)
        {
            return element as Room;
        }

        /// <summary>
        /// Check if ViewSchedule.IsMaterialTakeoff is available
        /// </summary>
        public static bool IsViewScheduleMaterialTakeoffAvailable()
        {
#if REVIT_2024 || REVIT_2025
            return false;
#else
            return true;
#endif
        }
    }
}
