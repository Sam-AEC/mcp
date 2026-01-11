from __future__ import annotations

import json
import os
import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "packages" / "mcp-server-revit" / "src"))

os.environ.setdefault("MCP_REVIT_WORKSPACE_DIR", ".")
os.environ.setdefault("MCP_REVIT_ALLOWED_DIRECTORIES", ".")

from revit_mcp_server.legacy_catalog import legacy_tool_defs, legacy_tools
from revit_mcp_server.ribbon import RibbonRuntime, build_registry


def approx_tokens(payload: Any) -> int:
    text = json.dumps(payload, ensure_ascii=True, separators=(",", ":"), default=str)
    return int(len(text) / 4)


def legacy_tool_payload() -> list[dict[str, Any]]:
    return [
        {"name": tool.name, "description": tool.description, "inputSchema": tool.inputSchema}
        for tool in legacy_tools()
    ]


def fake_runner(tool_id: str, args: dict[str, Any]) -> dict[str, Any]:
    if tool_id == "revit_batch_create_sheets_from_csv":
        return {"sheets_created": 12, "sheet_prefix": args.get("sheet_prefix", "A")}
    if tool_id == "revit_batch_set_parameters":
        ids = args.get("element_ids") or []
        return {"updated_count": len(ids), "parameter_name": args.get("parameter_name")}
    if tool_id == "revit_get_warnings":
        return {"warnings": [f"W{i:03d}" for i in range(200)]}
    if tool_id == "revit_check_clashes":
        return {"clashes_found": 18}
    return {"ok": True}


def run_benchmark() -> dict[str, Any]:
    registry = build_registry(legacy_tool_defs())
    runtime = RibbonRuntime(registry, fake_runner)

    legacy_list_tokens = approx_tokens(legacy_tool_payload())

    scenarios = {
        "create_views_sheets_set": {
            "old_tool": "revit_batch_create_sheets_from_csv",
            "old_result": fake_runner("revit_batch_create_sheets_from_csv", {}),
            "new_steps": [
                runtime.search_commands("SHEETS_DOCS", "sheet set", 8),
                runtime.command_help("sheets.create_from_csv"),
                runtime.execute("sheets.create_from_csv", {"csv_path": "C:/sheets.csv"}, "summary"),
            ],
        },
        "swap_types": {
            "old_tool": "revit_batch_set_parameters",
            "old_result": fake_runner("revit_batch_set_parameters", {"element_ids": list(range(120)), "parameter_name": "Type Name"}),
            "new_steps": [
                runtime.search_commands("FAMILIES_TYPES", "swap type", 8),
                runtime.command_help("families.swap_type"),
                runtime.execute(
                    "families.swap_type",
                    {"element_ids": list(range(120)), "value": "Supplier B - Type 02", "confirm": True, "reason": "Benchmark swap"},
                    "summary",
                ),
            ],
        },
        "qc_report": {
            "old_tool": "revit_get_warnings",
            "old_result": fake_runner("revit_get_warnings", {}),
            "new_steps": [
                runtime.search_commands("QC_COMPLIANCE", "warnings report", 8),
                runtime.command_help("qc.warnings_report"),
                runtime.execute("qc.warnings_report", {}, "summary"),
            ],
        },
    }

    results: dict[str, Any] = {"assumptions": {"rtt_ms_per_call": 150}}
    for name, scenario in scenarios.items():
        old_tokens = legacy_list_tokens + approx_tokens(scenario["old_result"])
        new_tokens = sum(approx_tokens(step) for step in scenario["new_steps"])
        old_calls = 1
        new_calls = len(scenario["new_steps"])
        results[name] = {
            "old_mode": {
                "tool_calls": old_calls,
                "approx_tokens": old_tokens,
                "estimated_latency_ms": old_calls * results["assumptions"]["rtt_ms_per_call"],
            },
            "ribbon_mode": {
                "tool_calls": new_calls,
                "approx_tokens": new_tokens,
                "estimated_latency_ms": new_calls * results["assumptions"]["rtt_ms_per_call"],
            },
            "token_reduction_pct": round((1 - (new_tokens / max(old_tokens, 1))) * 100, 2),
        }

    return results


if __name__ == "__main__":
    benchmark = run_benchmark()
    print(json.dumps(benchmark, indent=2))
