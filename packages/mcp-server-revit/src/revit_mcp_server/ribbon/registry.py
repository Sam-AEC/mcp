from __future__ import annotations

from typing import Any

from .catalog import (
    BREAK_GLASS_KEYWORDS,
    CAPABILITY_DEFS,
    CATEGORY_DEFS,
    CATEGORY_RULES,
    HIGH_COST_KEYWORDS,
    MED_COST_KEYWORDS,
)


def build_registry(tool_defs: list[dict[str, Any]]) -> dict[str, Any]:
    categories = CATEGORY_DEFS
    capabilities = CAPABILITY_DEFS
    tool_registry = build_tool_registry(tool_defs)
    command_registry = build_command_registry(tool_registry)
    return {
        "categories": categories,
        "capabilities": capabilities,
        "tools": tool_registry,
        "commands": command_registry,
    }


def build_tool_registry(tool_defs: list[dict[str, Any]]) -> dict[str, dict[str, Any]]:
    registry: dict[str, dict[str, Any]] = {}
    for tool in tool_defs:
        tool_id = tool["tool_id"]
        description = tool.get("description", "")
        input_schema = tool.get("input_schema", {}) or {}
        text = _normalize_text(f"{tool_id} {description}")
        category_id = _classify_category(text)
        capability_id = _classify_capability(category_id, text)
        tags = _build_tags(tool_id, description)
        risk_level = "break_glass" if _is_break_glass(text, category_id) else "normal"
        cost_hint = _estimate_cost(text)
        registry[tool_id] = {
            "tool_id": tool_id,
            "name": tool_id,
            "description": description,
            "input_schema": input_schema,
            "required_params": list(input_schema.get("required", []) or []),
            "category_id": category_id,
            "capability_id": capability_id,
            "tags": tags,
            "risk_level": risk_level,
            "cost_hint": cost_hint,
        }
    return registry


def build_command_registry(tool_registry: dict[str, dict[str, Any]]) -> dict[str, dict[str, Any]]:
    commands: dict[str, dict[str, Any]] = {}

    for command in _curated_commands(tool_registry):
        commands[command["command_id"]] = command

    for tool_id, tool in tool_registry.items():
        command_id = f"tool.{tool_id}"
        if command_id in commands:
            continue
        commands[command_id] = _auto_command(tool)

    return commands


def _curated_commands(tool_registry: dict[str, dict[str, Any]]) -> list[dict[str, Any]]:
    curated_specs = [
        {"command_id": "modeling.create_wall", "label": "Create wall", "description": "Create a wall between two points.", "tool_id": "revit_create_wall"},
        {"command_id": "modeling.create_floor", "label": "Create floor", "description": "Create a floor from boundary points.", "tool_id": "revit_create_floor"},
        {"command_id": "modeling.create_roof", "label": "Create roof", "description": "Create a roof from boundary points.", "tool_id": "revit_create_roof"},
        {"command_id": "modeling.create_level", "label": "Create level", "description": "Create a new level at elevation.", "tool_id": "revit_create_level"},
        {"command_id": "modeling.create_grid", "label": "Create grid", "description": "Create a grid line.", "tool_id": "revit_create_grid"},
        {"command_id": "modeling.create_room", "label": "Create room", "description": "Create a room on a level.", "tool_id": "revit_create_room"},
        {"command_id": "modeling.create_column", "label": "Create column", "description": "Create a structural column.", "tool_id": "revit_create_column"},
        {"command_id": "modeling.create_beam", "label": "Create beam", "description": "Create a structural beam.", "tool_id": "revit_create_beam"},
        {"command_id": "modeling.create_foundation", "label": "Create foundation", "description": "Create a foundation element.", "tool_id": "revit_create_foundation"},
        {"command_id": "modeling.move_element", "label": "Move element", "description": "Move an element by vector.", "tool_id": "revit_move_element"},
        {"command_id": "families.place_instance", "label": "Place family instance", "description": "Place a family instance at coordinates.", "tool_id": "revit_place_family_instance"},
        {"command_id": "families.place_door", "label": "Place door", "description": "Place a door in a wall.", "tool_id": "revit_place_door"},
        {"command_id": "families.place_window", "label": "Place window", "description": "Place a window in a wall.", "tool_id": "revit_place_window"},
        {"command_id": "families.edit_family", "label": "Edit family", "description": "Open a family for editing.", "tool_id": "revit_edit_family"},
        {"command_id": "views.create_floor_plan", "label": "Create floor plan", "description": "Create a floor plan view for a level.", "tool_id": "revit_create_floor_plan_view"},
        {"command_id": "views.create_3d", "label": "Create 3D view", "description": "Create a 3D view.", "tool_id": "revit_create_3d_view"},
        {"command_id": "views.create_section", "label": "Create section", "description": "Create a section view.", "tool_id": "revit_create_section_view"},
        {"command_id": "views.apply_template", "label": "Apply view template", "description": "Apply a view template to a view.", "tool_id": "revit_apply_view_template"},
        {"command_id": "sheets.create_from_csv", "label": "Create sheets from CSV", "description": "Create a sheet set from CSV input.", "tool_id": "revit_batch_create_sheets_from_csv"},
        {"command_id": "sheets.place_view", "label": "Place view on sheet", "description": "Place a view onto a sheet.", "tool_id": "revit_place_viewport_on_sheet"},
        {"command_id": "sheets.populate_titleblock", "label": "Populate titleblock", "description": "Fill titleblock parameters.", "tool_id": "revit_populate_titleblock"},
        {"command_id": "sheets.renumber", "label": "Renumber sheets", "description": "Batch renumber sheets.", "tool_id": "revit_renumber_sheets"},
        {"command_id": "schedules.create", "label": "Create schedule", "description": "Create a schedule view.", "tool_id": "revit_create_schedule", "category_id": "SHEETS_DOCS", "capability_id": "schedules"},
        {"command_id": "schedules.get_data", "label": "Get schedule data", "description": "Extract schedule data.", "tool_id": "revit_get_schedule_data", "category_id": "SHEETS_DOCS", "capability_id": "schedules"},
        {"command_id": "exports.export_dwg", "label": "Export DWG", "description": "Export a view to DWG.", "tool_id": "revit_export_dwg"},
        {"command_id": "exports.export_ifc", "label": "Export IFC", "description": "Export to IFC.", "tool_id": "revit_export_ifc"},
        {"command_id": "exports.export_navisworks", "label": "Export Navisworks", "description": "Export to Navisworks NWC.", "tool_id": "revit_export_navisworks"},
        {"command_id": "exports.export_image", "label": "Export image", "description": "Export view to image.", "tool_id": "revit_export_image"},
        {"command_id": "exports.render_3d", "label": "Render 3D view", "description": "Render a 3D view to image.", "tool_id": "revit_render_3d"},
        {"command_id": "qc.warnings_report", "label": "Warnings report", "description": "Generate a warnings report.", "tool_id": "revit_get_warnings", "category_id": "QC_COMPLIANCE", "capability_id": "warnings", "report_output": True},
        {"command_id": "qc.clash_check", "label": "Clash check", "description": "Run clash detection between categories.", "tool_id": "revit_check_clashes", "category_id": "QC_COMPLIANCE", "capability_id": "clash_detection", "report_output": True},
        {"command_id": "utilities.sync_to_central", "label": "Sync to central", "description": "Synchronize to central with relinquish.", "tool_id": "revit_sync_to_central", "risk_level": "break_glass"},
        {"command_id": "utilities.relinquish_all", "label": "Relinquish all", "description": "Relinquish all elements and worksets.", "tool_id": "revit_relinquish_all", "risk_level": "break_glass"},
        {"command_id": "parameters.set_value", "label": "Set parameter value", "description": "Set a parameter value on an element.", "tool_id": "revit_set_parameter_value", "category_id": "PARAMETERS_DATA", "capability_id": "element_parameters"},
        {"command_id": "parameters.batch_set", "label": "Batch set parameters", "description": "Set a parameter value for multiple elements.", "tool_id": "revit_batch_set_parameters", "category_id": "PARAMETERS_DATA", "capability_id": "batch_parameters"},
        {"command_id": "parameters.set_type_parameter", "label": "Set type parameter", "description": "Set a type parameter value.", "tool_id": "revit_set_type_parameter", "category_id": "PARAMETERS_DATA", "capability_id": "type_parameters"},
    ]

    curated = []
    for spec in curated_specs:
        tool_id = spec["tool_id"]
        tool = tool_registry[tool_id]
        minimal_schema = _minimal_schema(tool["input_schema"])
        full_schema = tool["input_schema"]
        examples = spec.get("examples") or [_example_from_schema(minimal_schema)]
        curated.append(
            _command_entry(
                command_id=spec["command_id"],
                label=spec["label"],
                description=spec["description"],
                category_id=spec.get("category_id", tool["category_id"]),
                capability_id=spec.get("capability_id", tool["capability_id"]),
                param_schema_minimal=minimal_schema,
                param_schema_full=full_schema,
                examples=examples,
                underlying_plan=[{"tool_id": tool_id, "args": None}],
                estimated_cost=tool["cost_hint"],
                risk_level=spec.get("risk_level", tool["risk_level"]),
                report_output=spec.get("report_output", False),
                is_curated=True,
            )
        )

    curated.append(_swap_type_command(tool_registry))
    return curated


def _swap_type_command(tool_registry: dict[str, dict[str, Any]]) -> dict[str, Any]:
    tool = tool_registry["revit_batch_set_parameters"]
    minimal_schema = {
        "type": "object",
        "properties": {
            "element_ids": {"type": "array", "items": {"type": "integer"}},
            "parameter_name": {"type": "string", "default": "Type Name"},
            "value": {"type": "string"},
            "confirm": {"type": "boolean", "default": False},
            "reason": {"type": "string"},
        },
        "required": ["element_ids", "value"],
    }
    examples = [{"element_ids": [101, 102], "value": "Supplier B - Type 02", "parameter_name": "Type Name", "confirm": True, "reason": "Client swap request"}]
    return _command_entry(
        command_id="families.swap_type",
        label="Swap family type",
        description="Swap element types by setting a type parameter on multiple elements.",
        category_id="FAMILIES_TYPES",
        capability_id="swap_types",
        param_schema_minimal=minimal_schema,
        param_schema_full=tool["input_schema"],
        examples=examples,
        underlying_plan=[
            {
                "tool_id": "revit_batch_set_parameters",
                "args": {"element_ids": "$element_ids", "parameter_name": "$parameter_name", "value": "$value"},
            }
        ],
        estimated_cost="med",
        risk_level="break_glass",
        report_output=False,
        is_curated=True,
    )


def _auto_command(tool: dict[str, Any]) -> dict[str, Any]:
    minimal_schema = _minimal_schema(tool["input_schema"])
    return _command_entry(
        command_id=f"tool.{tool['tool_id']}",
        label=_humanize_tool_name(tool["tool_id"]),
        description=_short_description(tool["description"]),
        category_id=tool["category_id"],
        capability_id=tool["capability_id"],
        param_schema_minimal=minimal_schema,
        param_schema_full=tool["input_schema"],
        examples=[_example_from_schema(minimal_schema)],
        underlying_plan=[{"tool_id": tool["tool_id"], "args": None}],
        estimated_cost=tool["cost_hint"],
        risk_level=tool["risk_level"],
        report_output=False,
        is_curated=False,
    )


def _command_entry(
    *,
    command_id: str,
    label: str,
    description: str,
    category_id: str,
    capability_id: str,
    param_schema_minimal: dict[str, Any],
    param_schema_full: dict[str, Any],
    examples: list[dict[str, Any]],
    underlying_plan: list[dict[str, Any]],
    estimated_cost: str,
    risk_level: str,
    report_output: bool,
    is_curated: bool,
) -> dict[str, Any]:
    return {
        "command_id": command_id,
        "label": label,
        "description": _short_description(description),
        "category_id": category_id,
        "capability_id": capability_id,
        "param_schema_minimal": param_schema_minimal,
        "param_schema_full": param_schema_full,
        "examples": examples,
        "underlying_plan": underlying_plan,
        "estimated_cost": estimated_cost,
        "risk_level": risk_level,
        "report_output": report_output,
        "is_curated": is_curated,
    }


def _minimal_schema(schema: dict[str, Any]) -> dict[str, Any]:
    required = list(schema.get("required", []) or [])
    properties = schema.get("properties", {}) or {}
    minimal_props: dict[str, Any] = {}
    for name in required:
        prop = properties.get(name, {})
        minimal_prop = {"type": prop.get("type", "string")}
        if "default" in prop:
            minimal_prop["default"] = prop["default"]
        minimal_props[name] = minimal_prop
    return {"type": "object", "properties": minimal_props, "required": required}


def _example_from_schema(schema: dict[str, Any]) -> dict[str, Any]:
    example: dict[str, Any] = {}
    properties = schema.get("properties", {}) or {}
    for name, prop in properties.items():
        example[name] = _placeholder_value(prop.get("type"), prop.get("default"))
    return example


def _placeholder_value(value_type: Any, default: Any) -> Any:
    if default is not None:
        return default
    if isinstance(value_type, list):
        value_type = value_type[0] if value_type else "string"
    if value_type == "integer":
        return 1
    if value_type == "number":
        return 1.0
    if value_type == "boolean":
        return False
    if value_type == "array":
        return []
    if value_type == "object":
        return {}
    return "value"


def _normalize_text(text: str) -> str:
    return text.lower().replace("-", "_")


def _classify_category(text: str) -> str:
    for category_id, keywords in CATEGORY_RULES:
        if any(keyword in text for keyword in keywords):
            return category_id
    return "UTILITIES"


def _classify_capability(category_id: str, text: str) -> str:
    capabilities = CAPABILITY_DEFS.get(category_id, [])
    for capability in capabilities:
        keywords = capability.get("keywords", [])
        if keywords and any(keyword in text for keyword in keywords):
            return capability["capability_id"]
    if capabilities:
        return capabilities[-1]["capability_id"]
    return "misc"


def _build_tags(tool_id: str, description: str) -> list[str]:
    tags = []
    tags.extend([part for part in tool_id.replace("revit_", "").split("_") if part])
    tags.extend([word for word in description.lower().replace(",", " ").split() if len(word) > 2])
    return sorted({tag.strip() for tag in tags if tag.strip()})


def _is_break_glass(text: str, category_id: str) -> bool:
    if category_id == "ADVANCED_BREAKGLASS":
        return True
    return any(keyword in text for keyword in BREAK_GLASS_KEYWORDS)


def _estimate_cost(text: str) -> str:
    if any(keyword in text for keyword in HIGH_COST_KEYWORDS):
        return "high"
    if any(keyword in text for keyword in MED_COST_KEYWORDS):
        return "med"
    return "low"


def _humanize_tool_name(tool_id: str) -> str:
    name = tool_id.replace("revit_", "").replace("_", " ").strip()
    return name.title()


def _short_description(description: str) -> str:
    description = description.strip()
    if len(description) <= 120:
        return description
    return description[:117].rstrip() + "..."
