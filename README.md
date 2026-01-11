<div align="center">

<img src="https://cdn.simpleicons.org/autodeskrevit/0696D7" alt="Revit" height="80"/>

# Revit MCP Server

**Model Context Protocol server for Autodesk Revit**

[![License: MIT](https://img.shields.io/badge/License-MIT-0078D4?style=flat-square)](LICENSE)
[![Python](https://img.shields.io/badge/Python-3.11+-3776AB?style=flat-square&logo=python&logoColor=white)](https://python.org)
[![.NET](https://img.shields.io/badge/.NET-4.8-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![Revit](https://img.shields.io/badge/Revit-2024%20%7C%202025-0696D7?style=flat-square)](https://autodesk.com/products/revit)

[Quick Start](#quick-start) · [Documentation](docs/) · [API Reference](docs/tools.md)

</div>

---

## Overview

Production-ready MCP server enabling AI agents to control Autodesk Revit through natural language. Integrates with Claude Desktop, Microsoft Copilot, and custom MCP clients.

**Key Features:**
- **278+ Revit API tools** (ALL 5 phases enabled!)
- **3000+ methods** via Universal Reflection API
- Rendering, detailing, IFC export, performance optimization
- Advanced filtering, geometry, MEP, structural, batch operations
- Localhost-only bridge with <100ms latency
- Thread-safe ExternalEvent architecture
- OAuth2 and audit logging support

### Demo

![Revit MCP Demo](assets/demo.gif)

---

## Quick Start

### Prerequisites

- Windows 10/11
- Autodesk Revit 2024 or 2025
- Python 3.11+
- .NET Framework 4.8

### Installation

```powershell
# Clone repository
git clone https://github.com/Sam-AEC/Autodesk-Revit-MCP-Server.git
cd Autodesk-Revit-MCP-Server

# Build Revit add-in
.\scripts\build-addin.ps1 -RevitVersion 2024

# Install Python package
pip install -e packages/mcp-server-revit
```

### Configure Claude Desktop

Edit `%APPDATA%\Claude\claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "revit": {
      "command": "python",
      "args": ["-m", "revit_mcp_server.mcp_server"],
      "env": {
        "MCP_REVIT_BRIDGE_URL": "http://127.0.0.1:3000",
        "MCP_REVIT_MODE": "bridge"
      }
    }
  }
}
```

### Verify Installation

Start Revit, then:

```powershell
curl http://localhost:3000/health
```

Expected response:
```json
{
  "status": "healthy",
  "revit_version": "2024"
}
```

---

## Architecture

```
┌─────────────────────────────────┐
│ AI Client (Claude/Copilot)     │
└───────────────┬─────────────────┘
                │ MCP Protocol
┌───────────────▼─────────────────┐
│ MCP Server (Python)             │
│ - Tool Registry                 │
│ - Security/Audit                │
└───────────────┬─────────────────┘
                │ HTTP :3000
┌───────────────▼─────────────────┐
│ Revit Bridge Add-in (C#)        │
│ - HTTP Server                   │
│ - ExternalEvent Handler         │
└───────────────┬─────────────────┘
                │ Revit API
┌───────────────▼─────────────────┐
│ Autodesk Revit 2024/2025        │
└─────────────────────────────────┘
```

**Threading Model:** HTTP requests are queued and executed on Revit's main thread via `ExternalEvent` for thread safety.

---

## Ribbon Mode (Progressive Disclosure)

### How the Ribbon system works

- Enable with `RIBBON_MODE=true` (or `MCP_REVIT_RIBBON_MODE=true`) to expose only the Ribbon meta-API.
- The LLM navigates `Category -> Capability -> Command` and calls `nav.execute(command_id, params)` for work.
- All 278+ underlying tools remain server-side; commands map to internal plans of those tools.
- Results default to `summary`; large payloads are stored server-side and returned as `report_id` (retrieve via `report.get`).
- Escape hatch discovery uses `nav.search_tools(category_id, query)` and advanced tool access (`nav.tool_open` / `nav.tool_run`) scoped to the selected category. These advanced endpoints are available but not listed by default to keep the Ribbon tool list <= 10.

### How to add a new command

1. Add a curated command in `packages/mcp-server-revit/src/revit_mcp_server/ribbon/registry.py` under `_curated_commands`.
2. Provide a stable `command_id`, short `label`, `description`, minimal schema, examples, and an `underlying_plan`.
3. If the command is high-risk, set `risk_level="break_glass"` and require `confirm=true` plus a `reason`.

### How to categorize a tool

1. Update keyword rules in `packages/mcp-server-revit/src/revit_mcp_server/ribbon/catalog.py`.
2. Tools are auto-classified by name/description; unmatched tools fall back to `UTILITIES`.
3. For curated commands, you can override `category_id` and `capability_id` explicitly.

### Example LLM call sequences

1. Create a permit drawing set:
   - `nav.search_commands("SHEETS_DOCS", "permit set")`
   - `nav.command_help("sheets.create_from_csv")`
   - `nav.execute("sheets.create_from_csv", { "csv_path": "C:/sheets.csv" })`
2. Swap window supplier A to B:
   - `nav.search_commands("FAMILIES_TYPES", "swap type")`
   - `nav.command_help("families.swap_type")`
   - `nav.execute("families.swap_type", { "element_ids": [101, 102], "value": "Supplier B - Type 02", "confirm": true, "reason": "Client swap" })`
3. Run QC and get report:
   - `nav.search_commands("QC_COMPLIANCE", "warnings report")`
   - `nav.command_help("qc.warnings_report")`
   - `nav.execute("qc.warnings_report", {})` -> returns `report_id`

### Benchmarks

- Script: `scripts/benchmark_ribbon.py`
- Latest sample numbers: `docs/benchmarks/ribbon_benchmark.json`

---

## Available Tools

### Document Management
- `revit.create_new_document` - Create blank project
- `revit.open_document` - Open RVT/RFA file
- `revit.save_document` - Save active document
- `revit.close_document` - Close document
- `revit.get_document_info` - Get project metadata

### Levels & Grids
- `revit.create_level` - Create level at elevation
- `revit.list_levels` - List all levels
- `revit.create_grid` - Create grid line

### Model Elements
- `revit.create_wall` - Create wall from curve
- `revit.create_floor` - Create floor from profile
- `revit.create_roof` - Create roof from profile
- `revit.create_column` - Create structural column
- `revit.create_beam` - Create structural beam
- `revit.create_foundation` - Create foundation element
- `revit.create_room` - Create room boundary

### MEP Systems
- `revit.create_duct` - Create HVAC duct
- `revit.create_pipe` - Create plumbing pipe
- `revit.create_cable_tray` - Create cable tray
- `revit.create_conduit` - Create electrical conduit
- `revit.get_mep_systems` - List MEP systems

### Families & Components
- `revit.place_family_instance` - Place family instance
- `revit.place_door` - Place door in wall
- `revit.place_window` - Place window in wall
- `revit.list_families` - List loaded families
- `revit.edit_family` - Edit family document

### Element Operations
- `revit.delete_element` - Delete element
- `revit.copy_element` - Copy element with translation
- `revit.move_element` - Move element by vector
- `revit.rotate_element` - Rotate element around axis
- `revit.mirror_element` - Mirror element across plane
- `revit.pin_element` - Pin element in place
- `revit.unpin_element` - Unpin element

### Parameters
- `revit.get_element_parameters` - Read element parameters
- `revit.set_parameter_value` - Write parameter value
- `revit.get_parameter_value` - Read single parameter
- `revit.batch_set_parameters` - Batch update parameters
- `revit.get_type_parameters` - Get type parameters
- `revit.set_type_parameter` - Set type parameter

### Views
- `revit.create_3d_view` - Create isometric 3D view
- `revit.create_section_view` - Create section view
- `revit.create_floor_plan_view` - Create floor plan
- `revit.duplicate_view` - Duplicate view
- `revit.apply_view_template` - Apply view template
- `revit.get_view_templates` - List view templates
- `revit.list_views` - List all views

### Sheets & Documentation
- `revit.create_sheet` - Create sheet with titleblock
- `revit.place_viewport_on_sheet` - Place viewport on sheet
- `revit.list_sheets` - List all sheets
- `revit.delete_sheet` - Delete sheet
- `revit.duplicate_sheet` - Duplicate sheet
- `revit.get_sheet_info` - Get sheet information
- `revit.list_titleblocks` - List titleblock types
- `revit.populate_titleblock` - Fill titleblock parameters
- `revit.renumber_sheets` - Renumber sheets
- `revit.batch_create_sheets_from_csv` - Create sheets from CSV

### Annotation
- `revit.create_tag` - Create element tag
- `revit.create_dimension` - Create dimension
- `revit.create_text_note` - Create text annotation
- `revit.create_text_type` - Create text type
- `revit.tag_all_in_view` - Tag all elements in view
- `revit.create_revision_cloud` - Create revision cloud

### Selection & Query
- `revit.get_selection` - Get current selection
- `revit.set_selection` - Set element selection
- `revit.list_elements_by_category` - Query by category
- `revit.get_element_type` - Get element type
- `revit.get_element_bounding_box` - Get element bounds
- `revit.get_categories` - List all categories

### Groups & Links
- `revit.create_group` - Create model group
- `revit.ungroup` - Ungroup elements
- `revit.convert_to_group` - Convert to group
- `revit.get_group_members` - Get group members
- `revit.get_link_instances` - List link instances
- `revit.get_rvt_links` - List Revit links

### Schedules & Data
- `revit.create_schedule` - Create schedule view
- `revit.get_schedule_data` - Extract schedule data
- `revit.export_schedules` - Export schedules to CSV
- `revit.calculate_material_quantities` - Calculate quantities

### Export Operations
- `revit.export_pdf_by_sheet_set` - Export to PDF
- `revit.export_dwg_by_view` - Export to DWG
- `revit.export_ifc_with_settings` - Export to IFC
- `revit.export_navisworks` - Export to Navisworks
- `revit.export_image` - Export view as image
- `revit.render_3d_view` - Render 3D view

### Project Information
- `revit.get_phases` - List project phases
- `revit.get_phase_filters` - List phase filters
- `revit.get_design_options` - List design options
- `revit.get_worksets` - List worksets
- `revit.get_warnings` - Get model warnings
- `revit.get_project_location` - Get project location

### Worksharing
- `revit.sync_to_central` - Synchronize with central
- `revit.relinquish_all` - Relinquish all elements

### Materials & Rendering
- `revit.create_material` - Create material
- `revit.set_element_material` - Assign material
- `revit.get_render_settings` - Get render settings

### Analysis & Validation
- `revit.check_clashes` - Run clash detection
- `revit.get_room_boundary` - Get room boundary

### Advanced Features
- `revit.invoke_method` - Universal Revit API method invocation
- `revit.reflect_get` - Get property via reflection
- `revit.reflect_set` - Set property via reflection

### Project Parameters
- `revit.list_shared_parameters` - List shared parameters
- `revit.create_shared_parameter` - Create shared parameter
- `revit.list_project_parameters` - List project parameters
- `revit.create_project_parameter` - Create project parameter

### Revisions
- `revit.get_revision_sequences` - Get revision sequences

### Rendering & Visualization (Phase 5)
- `revit.get_render_settings` - Get rendering settings
- `revit.set_render_quality` - Set render quality
- `revit.get_camera_settings` - Get camera settings
- `revit.set_camera_position` - Set camera position
- `revit.get_sun_settings` - Get sun settings
- `revit.set_sun_position` - Set sun position
- `revit.get_lighting_scheme` - Get lighting scheme
- `revit.set_visual_style` - Set visual style
- `revit.get_material_appearance` - Get material appearance
- `revit.set_material_appearance` - Set material appearance

### Detailing & Annotation (Phase 5)
- `revit.create_detail_line` - Create detail line
- `revit.create_detail_arc` - Create detail arc
- `revit.create_filled_region` - Create filled region
- `revit.create_masking_region` - Create masking region
- `revit.list_filled_region_types` - List filled region types
- `revit.create_detail_component` - Create detail component
- `revit.list_detail_components` - List detail components
- `revit.create_insulation` - Create insulation
- `revit.list_line_styles` - List line styles
- `revit.set_detail_line_style` - Set detail line style
- `revit.get_detail_item_bounding_box` - Get detail item bounding box
- `revit.create_repeating_detail` - Create repeating detail
- `revit.create_breakline` - Create breakline
- `revit.list_detailing_symbols` - List detailing symbols

### Project Organization (Phase 5)
- `revit.organize_browser_by_parameter` - Organize browser by parameter
- `revit.create_parameter_filter` - Create parameter filter
- `revit.list_view_filters` - List view filters
- `revit.apply_filter_to_view` - Apply filter to view
- `revit.get_project_parameter_groups` - Get project parameter groups
- `revit.organize_sheets_by_parameter` - Organize sheets by parameter
- `revit.get_keynote_table` - Get keynote table
- `revit.get_view_organization_structure` - Get view organization structure

### Performance & Optimization (Phase 5)
- `revit.purge_unused_elements` - Purge unused elements
- `revit.compact_file` - Compact file
- `revit.get_model_statistics` - Get model statistics
- `revit.analyze_model_performance` - Analyze model performance
- `revit.get_warnings_summary` - Get warnings summary
- `revit.audit_model` - Audit model
- `revit.optimize_view_performance` - Optimize view performance

### Advanced IFC & Data Exchange (Phase 5)
- `revit.get_ifc_export_configurations` - Get IFC export configurations
- `revit.export_ifc_with_custom_settings` - Export IFC with custom settings
- `revit.get_ifc_property_sets` - Get IFC property sets
- `revit.set_ifc_properties` - Set IFC properties
- `revit.get_classification_systems` - Get classification systems
- `revit.export_cobie_data` - Export COBie data
- `revit.get_bcf_topics` - Get BCF topics
- `revit.map_parameters_to_ifc` - Map parameters to IFC
- `revit.get_data_exchange_settings` - Get data exchange settings
- `revit.validate_ifc_export` - Validate IFC export

**Total: 278+ tools** • **Full API Reference:** [docs/tools.md](docs/tools.md) • **Upgrade Details:** [docs/API_UPGRADE_COMPLETE.md](docs/API_UPGRADE_COMPLETE.md)

---

## Security

**Default:** Localhost-only (`127.0.0.1:3000`), no authentication required.

**Enterprise:** Optional HTTPS, OAuth2 (Entra ID), audit logging, and rate limiting.

See [docs/security.md](docs/security.md) for details.

---

## Project Structure

```
Autodesk-Revit-MCP-Server/
├── packages/
│   ├── revit-bridge-addin/       # C# Revit Add-in
│   │   └── src/Bridge/
│   │       ├── App.cs            # IExternalApplication
│   │       ├── BridgeServer.cs   # HTTP server
│   │       └── BridgeCommandFactory.cs
│   └── mcp-server-revit/         # Python MCP Server
│       ├── src/revit_mcp_server/
│       │   ├── mcp_server.py     # MCP protocol
│       │   ├── bridge/client.py  # HTTP client
│       │   └── tools/            # Tool handlers
│       └── tests/
├── scripts/
│   ├── build-addin.ps1           # Build C# DLL
│   └── install.ps1               # Deploy to Revit
├── docs/
│   ├── tools.md                  # API reference
│   ├── architecture.md           # System design
│   └── security.md               # Security model
└── README.md
```

---

## Documentation

- [Installation Guide](docs/install.md)
- [API Reference](docs/tools.md)
- [Architecture](docs/architecture.md)
- [Security Model](docs/security.md)
- [Claude Desktop Setup](CLAUDE_DESKTOP_SETUP.md)
- [Contributing](CONTRIBUTING.md)

---

## License

[MIT License](LICENSE) - Copyright (c) 2025

---

## Links

- [Issues](https://github.com/Sam-AEC/Autodesk-Revit-MCP-Server/issues)
- [Discussions](https://github.com/Sam-AEC/Autodesk-Revit-MCP-Server/discussions)
- [Changelog](CHANGELOG.md)
- [Model Context Protocol](https://modelcontextprotocol.io)

---

<div align="center">

**[⬆ Back to Top](#revit-mcp-server)**

</div>
