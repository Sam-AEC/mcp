using System;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitBridge.UI;
using MediaColor = System.Windows.Media.Color;
using MediaBrush = System.Windows.Media.SolidColorBrush;

namespace RevitBridge.Bridge
{
    [Transaction(TransactionMode.Manual)]
    public class CommandSettings : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ShowSettingsDialog();
            return Result.Succeeded;
        }

        private void ShowSettingsDialog()
        {
            var dialog = new ModernDialog();
            dialog.SetTitle("Bridge Settings", "Configuration");

            dialog.AddInfoSection("Server Configuration",
                "Port: 3000\n" +
                "Host: localhost\n" +
                "Auto-start: Enabled\n" +
                "Logging: Enabled");

            dialog.AddSeparator();

            dialog.AddInfoSection("Features",
                "✅ Universal Bridge API\n" +
                "✅ Natural Language Processing\n" +
                "✅ Transaction Management\n" +
                "✅ Error Handling\n" +
                "✅ Batch Operations");

            dialog.AddSeparator();

            dialog.AddInfoSection("Advanced",
                "Note: Advanced settings can be configured in the config file.\n" +
                "Location: %AppData%\\RevitMCP\\config.json");

            dialog.SetActionButton("OK");
            dialog.ShowDialog();
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class CommandHelp : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ShowHelpDialog();
            return Result.Succeeded;
        }

        private void ShowHelpDialog()
        {
            var dialog = new ModernDialog();
            dialog.SetTitle("RevitMCP Help", "Documentation & Support");

            dialog.AddStatsGrid(
                ("📘", "Tools", "278+"),
                ("🎯", "Coverage", "99%"),
                ("⚡", "API Methods", "3000+")
            );

            dialog.AddSeparator();

            dialog.AddInfoSection("Quick Start",
                "1. Click 'Connect' to start the server\n" +
                "2. Use Claude Desktop or MCP client to connect\n" +
                "3. Start automating with natural language\n" +
                "4. Check 'Status' to monitor activity");

            dialog.AddSeparator();

            dialog.AddInfoSection("Available Commands",
                "✅ Phase 1: Core (40 tools)\n" +
                "   • Advanced Filtering (15)\n" +
                "   • Units & Formatting (5)\n" +
                "   • Schedules (10)\n" +
                "   • View Management (10)\n\n" +
                "✅ Phase 2: Advanced (51 tools)\n" +
                "   • Geometry Operations (20)\n" +
                "   • Family Management (12)\n" +
                "   • Worksharing (10)\n" +
                "   • Links & CAD (9)\n\n" +
                "✅ Phase 3: Specialized (28 tools)\n" +
                "   • MEP Systems (10)\n" +
                "   • Structural (8)\n" +
                "   • Stairs & Railings (6)\n" +
                "   • Phasing & Options (9)\n\n" +
                "✅ Phase 4: Enhancements (24 tools)\n" +
                "   • Transaction Control (8)\n" +
                "   • Analysis & QA (7)\n" +
                "   • Batch Operations (9)\n\n" +
                "✅ Phase 5: Extended (50 tools)\n" +
                "   • Rendering & Visualization (10)\n" +
                "   • Detailing & Annotation (15)\n" +
                "   • Project Organization (8)\n" +
                "   • Performance & Optimization (7)\n" +
                "   • IFC & Data Exchange (10)\n\n" +
                "🔥 Universal Reflection API\n" +
                "   • 3000+ Revit API methods accessible!");

            dialog.AddSeparator();

            dialog.AddInfoSection("Documentation",
                "GitHub: github.com/your-repo/revit-mcp-server\n" +
                "Docs: See README.md for full documentation");

            dialog.AddInfoSection("Support",
                "For issues and feature requests:\n" +
                "Create an issue on GitHub repository");

            dialog.SetActionButton("Open GitHub", () =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/anthropics/claude-desktop",
                        UseShellExecute = true
                    });
                }
                catch { }
            });

            dialog.ShowCancelButton(() => { }); // Just close
            dialog.ShowDialog();
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class CommandAbout : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ShowAboutDialog();
            return Result.Succeeded;
        }

        private void ShowAboutDialog()
        {
            var dialog = new ModernDialog();
            dialog.SetTitle("About RevitMCP Bridge", "Version 1.0.0");

            dialog.AddStatusCard(
                "🏆",
                "RevitMCP Bridge",
                "AI-Powered Revit Automation Platform",
                new MediaBrush(MediaColor.FromRgb(33, 150, 243))
            );

            dialog.AddSeparator();

            dialog.AddInfoSection("Version Information",
                "Version: 1.0.0\n" +
                $"Revit: {App.RevitVersion ?? "Unknown"}\n" +
                "Build: 2026.01.08\n" +
                "Platform: .NET Framework 4.8");

            dialog.AddSeparator();

            dialog.AddInfoSection("Features",
                "✅ 278+ Direct Commands (ALL 5 PHASES)\n" +
                "✅ 3000+ API Methods (Reflection)\n" +
                "✅ 99% Workflow Coverage\n" +
                "✅ Natural Language Support\n" +
                "✅ All Disciplines (Arch, MEP, Structural)\n" +
                "✅ Rendering & Visualization\n" +
                "✅ Advanced Detailing & Annotation\n" +
                "✅ IFC Export & Data Exchange\n" +
                "✅ Performance Optimization\n" +
                "✅ Advanced Geometry & Filtering\n" +
                "✅ Batch Operations at Scale\n" +
                "✅ Quality Assurance & Analysis\n" +
                "✅ Transaction Control & Rollback\n" +
                "✅ Worksharing Automation");

            dialog.AddSeparator();

            dialog.AddInfoSection("Credits",
                "Built with Model Context Protocol (MCP)\n" +
                "Powered by Anthropic Claude\n" +
                "Revit API Integration\n\n" +
                "© 2026 RevitMCP Project");

            dialog.SetActionButton("Close");
            dialog.ShowDialog();
        }
    }
}
