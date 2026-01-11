"""
MCP Server for Revit - Enables Claude Desktop to control Revit through natural language.
"""
from __future__ import annotations

import asyncio
import json
import os
from typing import Any

from mcp.server import Server
from mcp.server.stdio import stdio_server
from mcp.types import Tool, TextContent

from .bridge.client import BridgeClient
from .config import config
from .errors import BridgeError
from .legacy_catalog import legacy_tool_defs, legacy_tools, resolve_legacy_tool_call
from .ribbon import RibbonRuntime, build_registry

# Initialize the MCP server
app = Server("revit-mcp")

# Initialize bridge client
bridge = BridgeClient(config.bridge_url) if config.bridge_url else None

ribbon_registry = build_registry(legacy_tool_defs())


def _tool_runner(tool_id: str, args: dict[str, Any]) -> dict[str, Any]:
    if not bridge:
        raise BridgeError("Bridge not configured. Set MCP_REVIT_BRIDGE_URL in your .env file.")
    bridge_tool, payload = resolve_legacy_tool_call(tool_id, args)
    return bridge.call_tool(bridge_tool, payload)


ribbon_runtime = RibbonRuntime(ribbon_registry, _tool_runner)


def _is_ribbon_mode() -> bool:
    env_value = os.getenv("RIBBON_MODE")
    if env_value is not None:
        return env_value.lower() in {"1", "true", "yes", "on"}
    return bool(config.ribbon_mode)


def _nav_tools() -> list[Tool]:
    return [
        Tool(
            name="nav.list_categories",
            description="List ribbon categories.",
            inputSchema={"type": "object", "properties": {}},
        ),
        Tool(
            name="nav.list_capabilities",
            description="List capabilities within a category.",
            inputSchema={
                "type": "object",
                "properties": {"category_id": {"type": "string"}},
                "required": ["category_id"],
            },
        ),
        Tool(
            name="nav.list_commands",
            description="List commands within a capability.",
            inputSchema={
                "type": "object",
                "properties": {
                    "category_id": {"type": "string"},
                    "capability_id": {"type": "string"},
                    "limit": {"type": "integer", "default": 20},
                },
                "required": ["category_id", "capability_id"],
            },
        ),
        Tool(
            name="nav.command_help",
            description="Get command help and schema.",
            inputSchema={
                "type": "object",
                "properties": {
                    "command_id": {"type": "string"},
                    "detail": {"type": "string", "enum": ["minimal", "full"], "default": "minimal"},
                },
                "required": ["command_id"],
            },
        ),
        Tool(
            name="nav.execute",
            description="Execute a command by command_id.",
            inputSchema={
                "type": "object",
                "properties": {
                    "command_id": {"type": "string"},
                    "params": {"type": "object"},
                    "result_mode": {"type": "string", "enum": ["summary", "detail"], "default": "summary"},
                    "session_id": {"type": "string"},
                },
                "required": ["command_id"],
            },
        ),
        Tool(
            name="nav.search_commands",
            description="Search commands within a category.",
            inputSchema={
                "type": "object",
                "properties": {"category_id": {"type": "string"}, "query": {"type": "string"}, "k": {"type": "integer", "default": 8}},
                "required": ["category_id", "query"],
            },
        ),
        Tool(
            name="nav.search_tools",
            description="Search underlying tools scoped to a category.",
            inputSchema={
                "type": "object",
                "properties": {"category_id": {"type": "string"}, "query": {"type": "string"}, "k": {"type": "integer", "default": 8}},
                "required": ["category_id", "query"],
            },
        ),
        Tool(
            name="context.get",
            description="Get compact session context.",
            inputSchema={"type": "object", "properties": {"session_id": {"type": "string"}}},
        ),
        Tool(
            name="context.set",
            description="Patch session preferences.",
            inputSchema={
                "type": "object",
                "properties": {"patch": {"type": "object"}, "session_id": {"type": "string"}},
                "required": ["patch"],
            },
        ),
        Tool(
            name="report.get",
            description="Fetch a stored report by report_id.",
            inputSchema={
                "type": "object",
                "properties": {"report_id": {"type": "string"}, "page": {"type": "integer", "default": 1}, "limit": {"type": "integer", "default": 200}},
                "required": ["report_id"],
            },
        ),
    ]


def _json_response(payload: dict[str, Any]) -> list[TextContent]:
    return [TextContent(type="text", text=json.dumps(payload, ensure_ascii=True, separators=(",", ":")))]


def _handle_ribbon_tool(name: str, arguments: Any) -> list[TextContent]:
    args = arguments if isinstance(arguments, dict) else {}
    try:
        if name == "nav.list_categories":
            return _json_response(ribbon_runtime.list_categories())
        if name == "nav.list_capabilities":
            return _json_response(ribbon_runtime.list_capabilities(args.get("category_id")))
        if name == "nav.list_commands":
            return _json_response(
                ribbon_runtime.list_commands(
                    args.get("category_id"),
                    args.get("capability_id"),
                    args.get("limit", 20),
                )
            )
        if name == "nav.command_help":
            return _json_response(ribbon_runtime.command_help(args.get("command_id"), args.get("detail", "minimal")))
        if name == "nav.execute":
            return _json_response(
                ribbon_runtime.execute(
                    args.get("command_id"),
                    args.get("params"),
                    args.get("result_mode", "summary"),
                    args.get("session_id", "default"),
                )
            )
        if name == "nav.search_commands":
            return _json_response(ribbon_runtime.search_commands(args.get("category_id"), args.get("query"), args.get("k", 8)))
        if name == "nav.search_tools":
            return _json_response(ribbon_runtime.search_tools(args.get("category_id"), args.get("query"), args.get("k", 8)))
        if name == "nav.tool_open":
            return _json_response(ribbon_runtime.tool_open(args.get("tool_id"), args.get("category_id"), args.get("detail", "minimal")))
        if name == "nav.tool_run":
            return _json_response(
                ribbon_runtime.tool_run(
                    args.get("tool_id"),
                    args.get("category_id"),
                    args.get("args"),
                    args.get("result_mode", "summary"),
                    args.get("session_id", "default"),
                )
            )
        if name == "context.get":
            return _json_response(ribbon_runtime.context_get(args.get("session_id", "default")))
        if name == "context.set":
            return _json_response(ribbon_runtime.context_set(args.get("patch"), args.get("session_id", "default")))
        if name == "report.get":
            return _json_response(ribbon_runtime.report_get(args.get("report_id"), args.get("page", 1), args.get("limit", 200)))
    except Exception as exc:  # noqa: BLE001
        return _json_response({"status": "error", "message": str(exc)})
    return _json_response({"status": "error", "message": f"Unknown tool '{name}'"})


@app.list_tools()
async def list_tools() -> list[Tool]:
    """List all available Revit tools."""
    if _is_ribbon_mode():
        return _nav_tools()
    return legacy_tools()

@app.call_tool()
async def call_tool(name: str, arguments: Any) -> list[TextContent]:
    """Execute a Revit tool."""
    if name.startswith(("nav.", "context.", "report.")):
        return _handle_ribbon_tool(name, arguments)

    if _is_ribbon_mode():
        return _json_response({"status": "error", "message": "Tool not exposed in ribbon mode."})

    if not bridge:
        return [TextContent(
            type="text",
            text="Error: Bridge not configured. Set MCP_REVIT_BRIDGE_URL in your .env file."
        )]

    try:
        bridge_tool, payload = resolve_legacy_tool_call(name, arguments if isinstance(arguments, dict) else {})
        result = bridge.call_tool(bridge_tool, payload)
        response_text = f"{name} executed successfully\n\nResult:\n{json.dumps(result, indent=2)}"
        return [TextContent(type="text", text=response_text)]

    except KeyError:
        return [TextContent(
            type="text",
            text=f"Error: Unknown tool '{name}'"
        )]

    except BridgeError as e:
        error_msg = f"Revit Bridge Error: {str(e)}\n\n"
        error_msg += "Make sure:\n"
        error_msg += "1. Revit is running\n"
        error_msg += "2. A project is open in Revit\n"
        error_msg += "3. The RevitMCP Bridge add-in is loaded\n"
        error_msg += "4. The bridge is accessible at http://localhost:3000"

        return [TextContent(type="text", text=error_msg)]

    except Exception as e:
        return [TextContent(
            type="text",
            text=f"Error: {str(e)}"
        )]

async def main():
    """Run the MCP server."""
    async with stdio_server() as (read_stream, write_stream):
        await app.run(
            read_stream,
            write_stream,
            app.create_initialization_options()
        )


def run_mcp_server():
    """Entry point for running the MCP server."""
    asyncio.run(main())


if __name__ == "__main__":
    run_mcp_server()
