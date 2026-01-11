from __future__ import annotations

CATEGORY_DEFS = [
    {"category_id": "MODELING", "label": "Modeling", "description": "Create and edit model geometry."},
    {"category_id": "FAMILIES_TYPES", "label": "Families & Types", "description": "Family placement and type management."},
    {"category_id": "VIEWS", "label": "Views", "description": "Create and manage project views."},
    {"category_id": "SHEETS_DOCS", "label": "Sheets & Docs", "description": "Sheets, annotations, schedules, and documentation."},
    {"category_id": "PARAMETERS_DATA", "label": "Parameters & Data", "description": "Parameters, schedules, and data operations."},
    {"category_id": "QC_COMPLIANCE", "label": "QC & Compliance", "description": "Warnings, audits, and compliance checks."},
    {"category_id": "EXPORT_DELIVERABLES", "label": "Export & Deliverables", "description": "Export, publish, and render outputs."},
    {"category_id": "UTILITIES", "label": "Utilities", "description": "Document, selection, and model utilities."},
    {"category_id": "ADVANCED_BREAKGLASS", "label": "Advanced (Break Glass)", "description": "High-risk and reflection operations."},
]

CAPABILITY_DEFS = {
    "MODELING": [
        {"capability_id": "create_geometry", "label": "Create geometry", "description": "Walls, floors, roofs, and base geometry.", "keywords": ["wall", "floor", "roof"]},
        {"capability_id": "levels_grids", "label": "Levels & grids", "description": "Levels and grid systems.", "keywords": ["level", "grid"]},
        {"capability_id": "spaces_rooms", "label": "Rooms & spaces", "description": "Room creation and spatial modeling.", "keywords": ["room", "space"]},
        {"capability_id": "structural", "label": "Structural", "description": "Columns, beams, foundations.", "keywords": ["column", "beam", "foundation"]},
        {"capability_id": "mep_systems", "label": "MEP systems", "description": "MEP systems and runs.", "keywords": ["duct", "pipe", "cable_tray", "conduit", "mep"]},
        {"capability_id": "edit_transform", "label": "Edit/transform", "description": "Move, copy, rotate, mirror, pin.", "keywords": ["move", "copy", "rotate", "mirror", "pin", "unpin"]},
        {"capability_id": "materials", "label": "Materials", "description": "Material creation and assignment.", "keywords": ["material"]},
        {"capability_id": "misc", "label": "Misc modeling", "description": "Other modeling tools.", "keywords": []},
    ],
    "FAMILIES_TYPES": [
        {"capability_id": "place_instances", "label": "Place instances", "description": "Place families, doors, and windows.", "keywords": ["place", "door", "window", "family_instance"]},
        {"capability_id": "manage_families", "label": "Manage families", "description": "List and edit families.", "keywords": ["family", "edit_family", "list_families"]},
        {"capability_id": "type_parameters", "label": "Type parameters", "description": "Type-level parameter operations.", "keywords": ["type_parameter", "get_type_parameters"]},
        {"capability_id": "swap_types", "label": "Type swaps", "description": "Swap or update types at scale.", "keywords": ["swap", "change_type"]},
        {"capability_id": "categories_lookup", "label": "Category lookup", "description": "Categories and type discovery.", "keywords": ["categories", "element_type"]},
        {"capability_id": "misc", "label": "Misc families", "description": "Other family tools.", "keywords": []},
    ],
    "VIEWS": [
        {"capability_id": "create_views", "label": "Create views", "description": "Plan, section, and 3D views.", "keywords": ["create_floor_plan_view", "create_section_view", "create_3d_view", "view"]},
        {"capability_id": "templates", "label": "Templates", "description": "View template operations.", "keywords": ["template"]},
        {"capability_id": "annotations", "label": "Annotations", "description": "Tags, text, dimensions, revisions.", "keywords": ["tag", "text_note", "dimension", "revision_cloud"]},
        {"capability_id": "view_listing", "label": "View listing", "description": "List and inspect views.", "keywords": ["list_views", "view_templates"]},
        {"capability_id": "view_settings", "label": "View settings", "description": "Rendering and visual settings.", "keywords": ["render_settings"]},
        {"capability_id": "misc", "label": "Misc views", "description": "Other view tools.", "keywords": []},
    ],
    "SHEETS_DOCS": [
        {"capability_id": "create_sheets", "label": "Create sheets", "description": "Create, duplicate, and renumber sheets.", "keywords": ["sheet", "duplicate_sheet", "create_sheet", "renumber", "batch_create_sheets"]},
        {"capability_id": "place_views", "label": "Place views", "description": "Place viewports on sheets.", "keywords": ["viewport", "place_viewport"]},
        {"capability_id": "titleblocks", "label": "Titleblocks", "description": "Titleblock operations.", "keywords": ["titleblock"]},
        {"capability_id": "schedules", "label": "Schedules", "description": "Schedule creation and data.", "keywords": ["schedule"]},
        {"capability_id": "sheet_info", "label": "Sheet info", "description": "Sheet queries and metadata.", "keywords": ["sheet_info", "list_sheets"]},
        {"capability_id": "misc", "label": "Misc sheets", "description": "Other sheet tools.", "keywords": []},
    ],
    "PARAMETERS_DATA": [
        {"capability_id": "element_parameters", "label": "Element parameters", "description": "Get/set element parameters.", "keywords": ["get_element_parameters", "set_parameter_value", "get_parameter_value"]},
        {"capability_id": "batch_parameters", "label": "Batch parameters", "description": "Batch parameter edits.", "keywords": ["batch_set_parameters"]},
        {"capability_id": "shared_parameters", "label": "Shared parameters", "description": "Shared parameter management.", "keywords": ["shared_parameter"]},
        {"capability_id": "project_parameters", "label": "Project parameters", "description": "Project parameter management.", "keywords": ["project_parameter"]},
        {"capability_id": "type_parameters", "label": "Type parameters", "description": "Type parameter operations.", "keywords": ["type_parameter", "get_type_parameters"]},
        {"capability_id": "quantities", "label": "Quantities", "description": "Material quantities and measurements.", "keywords": ["quantities", "calculate_material_quantities"]},
        {"capability_id": "misc", "label": "Misc parameters", "description": "Other parameter tools.", "keywords": []},
    ],
    "QC_COMPLIANCE": [
        {"capability_id": "warnings", "label": "Warnings", "description": "Project warnings and triage.", "keywords": ["warning", "warnings"]},
        {"capability_id": "clash_detection", "label": "Clash detection", "description": "Clash checks and reports.", "keywords": ["clash"]},
        {"capability_id": "standards_naming", "label": "Standards & naming", "description": "Naming and standards checks.", "keywords": ["naming", "standard"]},
        {"capability_id": "view_template_compliance", "label": "Template compliance", "description": "View template compliance checks.", "keywords": ["template", "compliance"]},
        {"capability_id": "audit_reports", "label": "Audit reports", "description": "Audit and report generation.", "keywords": ["audit", "report", "health"]},
        {"capability_id": "misc", "label": "Misc QC", "description": "Other QC tools.", "keywords": []},
    ],
    "EXPORT_DELIVERABLES": [
        {"capability_id": "export_dwg", "label": "Export DWG", "description": "DWG exports.", "keywords": ["dwg"]},
        {"capability_id": "export_ifc", "label": "Export IFC", "description": "IFC exports.", "keywords": ["ifc"]},
        {"capability_id": "export_navisworks", "label": "Export Navisworks", "description": "NWC exports.", "keywords": ["navisworks", "nwc"]},
        {"capability_id": "export_images", "label": "Export images", "description": "Image exports.", "keywords": ["export_image", "image"]},
        {"capability_id": "renderings", "label": "Renderings", "description": "Render outputs.", "keywords": ["render"]},
        {"capability_id": "misc", "label": "Misc export", "description": "Other export tools.", "keywords": []},
    ],
    "UTILITIES": [
        {"capability_id": "document_management", "label": "Document management", "description": "Save, close, and document info.", "keywords": ["save_document", "close_document", "create_new_document", "document_info"]},
        {"capability_id": "selection", "label": "Selection", "description": "Selection operations.", "keywords": ["selection"]},
        {"capability_id": "worksharing", "label": "Worksharing", "description": "Sync and worksharing tools.", "keywords": ["sync_to_central", "relinquish", "worksets"]},
        {"capability_id": "groups_links", "label": "Groups & links", "description": "Groups and links.", "keywords": ["group", "link"]},
        {"capability_id": "geometry_query", "label": "Geometry query", "description": "Bounding boxes and geometry queries.", "keywords": ["bounding_box", "room_boundary", "project_location"]},
        {"capability_id": "data_query", "label": "Data query", "description": "Lists and lookup operations.", "keywords": ["list_elements", "list_families", "list_categories", "element_type"]},
        {"capability_id": "misc", "label": "Misc utilities", "description": "Other utilities.", "keywords": []},
    ],
    "ADVANCED_BREAKGLASS": [
        {"capability_id": "reflection_api", "label": "Reflection API", "description": "Reflection and dynamic invocation.", "keywords": ["reflect", "invoke_method"]},
        {"capability_id": "mass_delete", "label": "Mass delete", "description": "Delete and destructive operations.", "keywords": ["delete"]},
        {"capability_id": "purge_cleanup", "label": "Purge & cleanup", "description": "Cleanup and purge actions.", "keywords": ["purge", "compact"]},
        {"capability_id": "direct_invocation", "label": "Direct invocation", "description": "Raw API invocation tools.", "keywords": ["invoke"]},
        {"capability_id": "diagnostics", "label": "Diagnostics", "description": "Advanced diagnostics.", "keywords": ["diagnostic", "debug"]},
        {"capability_id": "misc", "label": "Misc advanced", "description": "Other high-risk tools.", "keywords": []},
    ],
}

CATEGORY_RULES = [
    ("ADVANCED_BREAKGLASS", ["invoke_method", "reflect", "reflection"]),
    ("EXPORT_DELIVERABLES", ["export", "render", "ifc", "dwg", "navisworks", "image"]),
    ("QC_COMPLIANCE", ["audit", "warning", "clash", "compliance", "health", "report"]),
    ("SHEETS_DOCS", ["sheet", "titleblock", "schedule", "viewport", "revision"]),
    ("VIEWS", ["view", "template", "section", "plan", "3d"]),
    ("FAMILIES_TYPES", ["family", "type", "door", "window"]),
    ("PARAMETERS_DATA", ["parameter", "quantity", "schedule_data"]),
    ("MODELING", ["wall", "floor", "roof", "level", "grid", "room", "column", "beam", "foundation", "duct", "pipe", "conduit", "cable_tray", "material"]),
]

BREAK_GLASS_KEYWORDS = [
    "invoke_method",
    "reflect",
    "delete",
    "purge",
    "compact",
    "relinquish_all",
    "sync_to_central",
]

HIGH_COST_KEYWORDS = ["export", "render", "clash", "audit", "calculate", "schedule_data"]
MED_COST_KEYWORDS = ["create", "set", "place", "move", "copy", "rotate", "mirror", "apply", "sync", "relinquish", "convert"]
