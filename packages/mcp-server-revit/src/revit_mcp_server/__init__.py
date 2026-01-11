"""Revit MCP server entrypoint."""


def run_server() -> None:
    from .server import run_server as _run_server

    _run_server()


__all__ = ["run_server"]
