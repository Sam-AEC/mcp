import pytest

from revit_mcp_server.legacy_catalog import legacy_tool_defs
from revit_mcp_server.ribbon import RibbonRuntime, build_registry


def test_registry_includes_all_tools():
    registry = build_registry(legacy_tool_defs())
    assert len(registry["tools"]) == len(legacy_tool_defs())


def test_command_registry_has_curated_and_auto():
    registry = build_registry(legacy_tool_defs())
    commands = registry["commands"]
    assert "modeling.create_wall" in commands
    assert "tool.revit_create_wall" in commands


def test_break_glass_requires_confirm():
    registry = build_registry(legacy_tool_defs())
    runtime = RibbonRuntime(registry, lambda tool_id, args: {"ok": True})
    with pytest.raises(ValueError):
        runtime.execute("families.swap_type", {"element_ids": [1], "value": "Type B"})
