using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitBridge.Commands.Extended.Rendering
{
    /// <summary>
    /// Rendering and visualization commands - advanced visual quality control
    /// RenderingSettings (#550, Score: 130), SunAndShadowSettings (#551, Score: 130)
    /// </summary>
    public static class RenderingCommands
    {
        #region 1. Get Render Settings

        public static object GetRenderSettings(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();

            View view = doc.GetElement(new ElementId(viewId)) as View;
            if (view == null)
            {
                throw new Exception($"View {viewId} not found");
            }

            var settings = RenderingSettings.Create(doc, view.Id);

            return new
            {
                viewId = viewId,
                viewName = view.Name,
                renderQuality = settings.Quality.ToString(),
                renderQualityValue = (int)settings.Quality,
                resolution = settings.Resolution.ToString(),
                lighting = settings.LightingScheme.ToString(),
                backgroundStyle = settings.BackgroundStyle.ToString()
            };
        }

        #endregion

        #region 2. Set Render Quality

        public static object SetRenderQuality(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            string quality = payload.GetProperty("quality").GetString(); // Draft, Medium, High, Best

            using (var trans = new Transaction(doc, "Set Render Quality"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                var settings = RenderingSettings.Create(doc, view.Id);

                settings.Quality = quality switch
                {
                    "Draft" => RenderQualitySetting.Draft,
                    "Medium" => RenderQualitySetting.Medium,
                    "High" => RenderQualitySetting.High,
                    "Best" => RenderQualitySetting.Best,
                    _ => RenderQualitySetting.Medium
                };

                trans.Commit();

                return new
                {
                    success = true,
                    viewId = viewId,
                    quality = settings.Quality.ToString()
                };
            }
        }

        #endregion

        #region 3. Get Camera Settings

        public static object GetCameraSettings(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();

            View3D view = doc.GetElement(new ElementId(viewId)) as View3D;
            if (view == null)
            {
                throw new Exception($"3D View {viewId} not found");
            }

            if (view.IsPerspective)
            {
                ViewOrientation3D orientation = view.GetOrientation();

                return new
                {
                    viewId = viewId,
                    isPerspective = true,
                    eyePosition = new { x = orientation.EyePosition.X, y = orientation.EyePosition.Y, z = orientation.EyePosition.Z },
                    forwardDirection = new { x = orientation.ForwardDirection.X, y = orientation.ForwardDirection.Y, z = orientation.ForwardDirection.Z },
                    upDirection = new { x = orientation.UpDirection.X, y = orientation.UpDirection.Y, z = orientation.UpDirection.Z }
                };
            }
            else
            {
                return new
                {
                    viewId = viewId,
                    isPerspective = false,
                    message = "View is not perspective"
                };
            }
        }

        #endregion

        #region 4. Set Camera Position

        public static object SetCameraPosition(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            var eyePos = payload.GetProperty("eye_position");
            var targetPos = payload.GetProperty("target_position");

            using (var trans = new Transaction(doc, "Set Camera Position"))
            {
                trans.Start();

                View3D view = doc.GetElement(new ElementId(viewId)) as View3D;
                if (view == null)
                {
                    throw new Exception($"3D View {viewId} not found");
                }

                XYZ eye = new XYZ(
                    eyePos.GetProperty("x").GetDouble(),
                    eyePos.GetProperty("y").GetDouble(),
                    eyePos.GetProperty("z").GetDouble()
                );

                XYZ target = new XYZ(
                    targetPos.GetProperty("x").GetDouble(),
                    targetPos.GetProperty("y").GetDouble(),
                    targetPos.GetProperty("z").GetDouble()
                );

                XYZ forward = (target - eye).Normalize();
                XYZ up = XYZ.BasisZ;

                ViewOrientation3D orientation = new ViewOrientation3D(eye, up, forward);
                view.SetOrientation(orientation);
                view.SaveOrientationAndLock();

                trans.Commit();

                return new
                {
                    success = true,
                    viewId = viewId,
                    eyePosition = new { eye.X, eye.Y, eye.Z },
                    targetPosition = new { target.X, target.Y, target.Z }
                };
            }
        }

        #endregion

        #region 5. Get Sun Settings

        public static object GetSunSettings(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();

            View view = doc.GetElement(new ElementId(viewId)) as View;
            if (view == null)
            {
                throw new Exception($"View {viewId} not found");
            }

            SunAndShadowSettings sunSettings = view.SunAndShadowSettings;

            return new
            {
                viewId = viewId,
                sunEnabled = sunSettings.StartDateAndTime != null,
                azimuth = sunSettings.Azimuth,
                altitude = sunSettings.Altitude,
                intensity = sunSettings.SunIntensity,
                startDate = sunSettings.StartDateAndTime?.ToString(),
                timeOfDay = sunSettings.StartDateAndTime?.TimeOfDay.ToString()
            };
        }

        #endregion

        #region 6. Set Sun Position

        public static object SetSunPosition(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            double azimuth = payload.GetProperty("azimuth").GetDouble();
            double altitude = payload.GetProperty("altitude").GetDouble();

            using (var trans = new Transaction(doc, "Set Sun Position"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                SunAndShadowSettings sunSettings = view.SunAndShadowSettings;
                sunSettings.Azimuth = azimuth;
                sunSettings.Altitude = altitude;

                trans.Commit();

                return new
                {
                    success = true,
                    viewId = viewId,
                    azimuth = azimuth,
                    altitude = altitude
                };
            }
        }

        #endregion

        #region 7. Get Lighting Scheme

        public static object GetLightingScheme(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();

            View view = doc.GetElement(new ElementId(viewId)) as View;
            if (view == null)
            {
                throw new Exception($"View {viewId} not found");
            }

            return new
            {
                viewId = viewId,
                displayStyle = view.DisplayStyle.ToString(),
                detailLevel = view.DetailLevel.ToString()
            };
        }

        #endregion

        #region 8. Set Visual Style

        public static object SetVisualStyle(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int viewId = payload.GetProperty("view_id").GetInt32();
            string displayStyle = payload.GetProperty("display_style").GetString(); // Wireframe, Hidden, Shaded, Realistic

            using (var trans = new Transaction(doc, "Set Visual Style"))
            {
                trans.Start();

                View view = doc.GetElement(new ElementId(viewId)) as View;
                if (view == null)
                {
                    throw new Exception($"View {viewId} not found");
                }

                view.DisplayStyle = displayStyle switch
                {
                    "Wireframe" => DisplayStyle.Wireframe,
                    "Hidden" => DisplayStyle.HLR,
                    "Shaded" => DisplayStyle.Shading,
                    "Realistic" => DisplayStyle.Realistic,
                    _ => DisplayStyle.Shading
                };

                trans.Commit();

                return new
                {
                    success = true,
                    viewId = viewId,
                    displayStyle = view.DisplayStyle.ToString()
                };
            }
        }

        #endregion

        #region 9. Get Material Appearance

        public static object GetMaterialAppearance(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int materialId = payload.GetProperty("material_id").GetInt32();

            Material material = doc.GetElement(new ElementId(materialId)) as Material;
            if (material == null)
            {
                throw new Exception($"Material {materialId} not found");
            }

            ElementId appearanceId = material.AppearanceAssetId;
            AppearanceAssetElement appearance = appearanceId != ElementId.InvalidElementId
                ? doc.GetElement(appearanceId) as AppearanceAssetElement
                : null;

            return new
            {
                materialId = materialId,
                materialName = material.Name,
                hasAppearance = appearance != null,
                appearanceName = appearance?.Name,
                color = new
                {
                    r = material.Color.Red,
                    g = material.Color.Green,
                    b = material.Color.Blue
                },
                transparency = material.Transparency,
                shininess = material.Shininess,
                smoothness = material.Smoothness
            };
        }

        #endregion

        #region 10. Set Material Appearance

        public static object SetMaterialAppearance(UIApplication app, JsonElement payload)
        {
            var doc = app.ActiveUIDocument.Document;
            int materialId = payload.GetProperty("material_id").GetInt32();
            var color = payload.TryGetProperty("color", out var c) ? c : (JsonElement?)null;
            int? transparency = payload.TryGetProperty("transparency", out var t) ? t.GetInt32() : null;
            int? shininess = payload.TryGetProperty("shininess", out var sh) ? sh.GetInt32() : null;

            using (var trans = new Transaction(doc, "Set Material Appearance"))
            {
                trans.Start();

                Material material = doc.GetElement(new ElementId(materialId)) as Material;
                if (material == null)
                {
                    throw new Exception($"Material {materialId} not found");
                }

                if (color.HasValue)
                {
                    material.Color = new Color(
                        (byte)color.Value.GetProperty("r").GetInt32(),
                        (byte)color.Value.GetProperty("g").GetInt32(),
                        (byte)color.Value.GetProperty("b").GetInt32()
                    );
                }

                if (transparency.HasValue)
                {
                    material.Transparency = transparency.Value;
                }

                if (shininess.HasValue)
                {
                    material.Shininess = shininess.Value;
                }

                trans.Commit();

                return new
                {
                    success = true,
                    materialId = materialId,
                    materialName = material.Name,
                    color = new { material.Color.Red, material.Color.Green, material.Color.Blue },
                    transparency = material.Transparency,
                    shininess = material.Shininess
                };
            }
        }

        #endregion
    }
}
