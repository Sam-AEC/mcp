from __future__ import annotations

import json
import time
import uuid
from typing import Any, Callable


ToolRunner = Callable[[str, dict[str, Any]], dict[str, Any]]


class ResultStore:
    def __init__(self, max_items: int = 500) -> None:
        self._store: dict[str, Any] = {}
        self._order: list[str] = []
        self._max_items = max_items

    def put(self, payload: Any) -> str:
        report_id = str(uuid.uuid4())
        self._store[report_id] = payload
        self._order.append(report_id)
        if len(self._order) > self._max_items:
            oldest = self._order.pop(0)
            self._store.pop(oldest, None)
        return report_id

    def get(self, report_id: str) -> Any:
        return self._store.get(report_id)


class SessionStore:
    def __init__(self) -> None:
        self._sessions: dict[str, dict[str, Any]] = {}

    def get(self, session_id: str) -> dict[str, Any]:
        return self._sessions.setdefault(session_id, {})

    def patch(self, session_id: str, patch: dict[str, Any]) -> dict[str, Any]:
        state = self.get(session_id)
        allowed = {"selected_preset", "style_pack"}
        for key, value in patch.items():
            if key in allowed:
                state[key] = value
        return state


class RibbonRuntime:
    def __init__(
        self,
        registry: dict[str, Any],
        tool_runner: ToolRunner,
        result_store: ResultStore | None = None,
        session_store: SessionStore | None = None,
        max_inline_bytes: int = 2000,
    ) -> None:
        self.registry = registry
        self.tool_runner = tool_runner
        self.result_store = result_store or ResultStore()
        self.session_store = session_store or SessionStore()
        self.max_inline_bytes = max_inline_bytes

    def list_categories(self) -> dict[str, Any]:
        return {"categories": self.registry["categories"]}

    def list_capabilities(self, category_id: str) -> dict[str, Any]:
        self._ensure_category(category_id)
        capabilities = self.registry["capabilities"].get(category_id, [])
        thin = [
            {
                "capability_id": cap["capability_id"],
                "label": cap["label"],
                "description": cap["description"],
            }
            for cap in capabilities
        ]
        return {"category_id": category_id, "capabilities": thin}

    def list_commands(self, category_id: str, capability_id: str, limit: int = 20) -> dict[str, Any]:
        self._ensure_category(category_id)
        commands = self._filter_commands(category_id, capability_id)
        commands = sorted(commands, key=lambda cmd: (not cmd["is_curated"], cmd["label"].lower()))
        return {
            "category_id": category_id,
            "capability_id": capability_id,
            "commands": [self._thin_command(cmd) for cmd in commands[:limit]],
            "count": len(commands),
            "limit": limit,
        }

    def search_commands(self, category_id: str, query: str, k: int = 8) -> dict[str, Any]:
        self._ensure_category(category_id)
        query = (query or "").strip().lower()
        matches = []
        for command in self.registry["commands"].values():
            if command["category_id"] != category_id:
                continue
            score = 0
            label = command["label"].lower()
            description = command["description"].lower()
            if query in label:
                score += 3
            if query in description:
                score += 2
            if query in command["command_id"].lower():
                score += 1
            if command["is_curated"]:
                score += 1
            if score > 0:
                matches.append((score, command))
        matches.sort(key=lambda item: (-item[0], item[1]["label"].lower()))
        return {
            "category_id": category_id,
            "query": query,
            "commands": [self._thin_command(match[1]) for match in matches[:k]],
            "count": len(matches),
            "k": k,
        }

    def command_help(self, command_id: str, detail: str = "minimal") -> dict[str, Any]:
        command = self._get_command(command_id)
        response = {
            "command_id": command["command_id"],
            "label": command["label"],
            "description": command["description"],
            "category_id": command["category_id"],
            "capability_id": command["capability_id"],
            "estimated_cost": command["estimated_cost"],
            "risk_level": command["risk_level"],
            "param_schema": command["param_schema_minimal"],
            "examples": command["examples"],
        }
        if command["risk_level"] == "break_glass":
            response["warning"] = "Break-glass command. Requires confirm=true and reason."
        if detail == "full":
            response["param_schema_full"] = command["param_schema_full"]
            response["underlying_plan"] = command["underlying_plan"]
        return response

    def execute(
        self,
        command_id: str,
        params: dict[str, Any] | None = None,
        result_mode: str = "summary",
        session_id: str = "default",
    ) -> dict[str, Any]:
        command = self._get_command(command_id)
        params = params or {}
        self._enforce_break_glass(command, params)
        cleaned_params = {key: value for key, value in params.items() if key not in {"confirm", "reason"}}
        start = time.perf_counter()
        results = self._execute_plan(command, cleaned_params)
        elapsed_ms = int((time.perf_counter() - start) * 1000)
        summary = self._summarize_results(command_id, results)
        report_id = self._store_if_needed(command, results, result_mode)
        if report_id:
            summary["report_id"] = report_id
            if _has_diff(results):
                summary["diff_id"] = report_id
        summary["elapsed_ms"] = elapsed_ms
        self._update_session(session_id, command_id, summary, results)
        if result_mode == "detail":
            return self._detail_response(command_id, results, summary, report_id)
        return summary

    def search_tools(self, category_id: str, query: str, k: int = 8) -> dict[str, Any]:
        self._ensure_category(category_id)
        query = (query or "").strip().lower()
        matches = []
        for tool in self.registry["tools"].values():
            if tool["category_id"] != category_id:
                continue
            text = f"{tool['tool_id']} {tool['description']}".lower()
            if query in text:
                matches.append(tool)
        matches = sorted(matches, key=lambda tool: tool["tool_id"])[:k]
        return {
            "category_id": category_id,
            "query": query,
            "tools": [self._thin_tool(tool) for tool in matches],
            "count": len(matches),
            "k": k,
        }

    def tool_open(self, tool_id: str, category_id: str, detail: str = "minimal") -> dict[str, Any]:
        tool = self._get_tool(tool_id, category_id)
        response = {
            "tool_id": tool["tool_id"],
            "name": tool["name"],
            "description": tool["description"],
            "category_id": tool["category_id"],
            "capability_id": tool["capability_id"],
            "risk_level": tool["risk_level"],
            "param_schema": _minimal_schema(tool["input_schema"]),
        }
        if tool["risk_level"] == "break_glass":
            response["warning"] = "Break-glass tool. Requires confirm=true and reason."
        if detail == "full":
            response["param_schema_full"] = tool["input_schema"]
        return response

    def tool_run(
        self,
        tool_id: str,
        category_id: str,
        args: dict[str, Any] | None = None,
        result_mode: str = "summary",
        session_id: str = "default",
    ) -> dict[str, Any]:
        tool = self._get_tool(tool_id, category_id)
        args = args or {}
        self._enforce_break_glass(tool, args)
        cleaned_args = {key: value for key, value in args.items() if key not in {"confirm", "reason"}}
        start = time.perf_counter()
        result = self.tool_runner(tool_id, cleaned_args)
        elapsed_ms = int((time.perf_counter() - start) * 1000)
        summary = self._summarize_results(tool_id, [result])
        report_id = self._store_if_needed({"report_output": False}, [result], result_mode)
        if report_id:
            summary["report_id"] = report_id
            if _has_diff([result]):
                summary["diff_id"] = report_id
        summary["elapsed_ms"] = elapsed_ms
        self._update_session(session_id, tool_id, summary, [result])
        if result_mode == "detail":
            return self._detail_response(tool_id, [result], summary, report_id)
        return summary

    def context_get(self, session_id: str = "default") -> dict[str, Any]:
        state = self.session_store.get(session_id)
        return {
            "session_id": session_id,
            "active_document_id": state.get("active_document_id"),
            "active_document_title": state.get("active_document_title"),
            "last_command_id": state.get("last_command_id"),
            "last_report_id": state.get("last_report_id"),
            "last_change_summary": state.get("last_change_summary"),
            "selected_preset": state.get("selected_preset"),
            "style_pack": state.get("style_pack"),
        }

    def context_set(self, patch: dict[str, Any], session_id: str = "default") -> dict[str, Any]:
        state = self.session_store.patch(session_id, patch or {})
        return {
            "session_id": session_id,
            "selected_preset": state.get("selected_preset"),
            "style_pack": state.get("style_pack"),
        }

    def report_get(self, report_id: str, page: int = 1, limit: int = 200) -> dict[str, Any]:
        payload = self.result_store.get(report_id)
        if payload is None:
            return {"report_id": report_id, "status": "missing"}
        if isinstance(payload, list):
            total = len(payload)
            start = max(page - 1, 0) * limit
            end = start + limit
            return {"report_id": report_id, "page": page, "limit": limit, "total": total, "items": payload[start:end]}
        return {"report_id": report_id, "payload": payload}

    def _thin_command(self, command: dict[str, Any]) -> dict[str, Any]:
        required = command["param_schema_minimal"].get("required", []) or []
        return {
            "command_id": command["command_id"],
            "label": command["label"],
            "description": command["description"],
            "required_params": required,
            "estimated_cost": command["estimated_cost"],
            "risk_level": command["risk_level"],
        }

    def _thin_tool(self, tool: dict[str, Any]) -> dict[str, Any]:
        return {
            "tool_id": tool["tool_id"],
            "name": tool["name"],
            "short_desc": tool["description"][:120],
            "required_args": tool["required_params"],
            "risk_level": tool["risk_level"],
            "cost_hint": tool["cost_hint"],
        }

    def _filter_commands(self, category_id: str, capability_id: str) -> list[dict[str, Any]]:
        return [
            command
            for command in self.registry["commands"].values()
            if command["category_id"] == category_id and command["capability_id"] == capability_id
        ]

    def _get_command(self, command_id: str) -> dict[str, Any]:
        command = self.registry["commands"].get(command_id)
        if not command:
            raise KeyError(f"Unknown command '{command_id}'")
        return command

    def _get_tool(self, tool_id: str, category_id: str) -> dict[str, Any]:
        tool = self.registry["tools"].get(tool_id)
        if not tool:
            raise KeyError(f"Unknown tool '{tool_id}'")
        if tool["category_id"] != category_id:
            raise ValueError("Tool not in selected category")
        return tool

    def _ensure_category(self, category_id: str) -> None:
        if category_id not in {category["category_id"] for category in self.registry["categories"]}:
            raise KeyError(f"Unknown category '{category_id}'")

    def _execute_plan(self, command: dict[str, Any], params: dict[str, Any]) -> list[dict[str, Any]]:
        results = []
        for step in command["underlying_plan"]:
            tool_id = step["tool_id"]
            args_template = step.get("args")
            if args_template is None:
                args = params
            else:
                args = _resolve_template(args_template, params)
            results.append(self.tool_runner(tool_id, args))
        return results

    def _enforce_break_glass(self, subject: dict[str, Any], params: dict[str, Any]) -> None:
        if subject.get("risk_level") != "break_glass":
            return
        confirm = params.get("confirm")
        reason = params.get("reason")
        if confirm is not True or not reason:
            raise ValueError("Break-glass action requires params.confirm=true and params.reason")

    def _summarize_results(self, command_id: str, results: list[dict[str, Any]]) -> dict[str, Any]:
        element_ids: list[int] = []
        warnings_count = 0
        change_hint = 0
        for result in results:
            warnings_count += _extract_warnings_count(result)
            element_ids.extend(_extract_element_ids(result))
            change_hint = max(change_hint, _extract_change_count(result))
        unique_ids = list(dict.fromkeys(element_ids))
        changed_count = len(unique_ids) if unique_ids else change_hint
        return {
            "status": "ok",
            "command_id": command_id,
            "changed_element_count": changed_count,
            "element_ids": unique_ids[:20],
            "warnings_count": warnings_count,
        }

    def _store_if_needed(self, command: dict[str, Any], results: list[dict[str, Any]], result_mode: str) -> str | None:
        payload = results if len(results) > 1 else results[0]
        serialized = json.dumps(payload, ensure_ascii=True, default=str)
        is_large = len(serialized.encode("utf-8")) > self.max_inline_bytes
        if result_mode == "summary":
            if is_large or command.get("report_output"):
                return self.result_store.put(payload)
        if result_mode == "detail" and is_large:
            return self.result_store.put(payload)
        return None

    def _detail_response(
        self,
        command_id: str,
        results: list[dict[str, Any]],
        summary: dict[str, Any],
        report_id: str | None,
    ) -> dict[str, Any]:
        payload = results if len(results) > 1 else results[0]
        response = {"status": "ok", "command_id": command_id, "summary": summary}
        if report_id:
            response["report_id"] = report_id
            response["detail_truncated"] = True
        else:
            response["result"] = payload
        return response

    def _update_session(self, session_id: str, command_id: str, summary: dict[str, Any], results: list[dict[str, Any]]) -> None:
        state = self.session_store.get(session_id)
        state["last_command_id"] = command_id
        state["last_change_summary"] = {
            "changed_element_count": summary.get("changed_element_count"),
            "warnings_count": summary.get("warnings_count"),
        }
        if summary.get("report_id"):
            state["last_report_id"] = summary["report_id"]
        for result in results:
            if "document_id" in result:
                state["active_document_id"] = result.get("document_id")
            if "title" in result:
                state["active_document_title"] = result.get("title")


def _resolve_template(template: Any, params: dict[str, Any]) -> Any:
    if isinstance(template, str):
        if template.startswith("$"):
            return params.get(template[1:])
        return template
    if isinstance(template, dict):
        return {key: _resolve_template(value, params) for key, value in template.items()}
    if isinstance(template, list):
        return [_resolve_template(item, params) for item in template]
    return template


def _extract_element_ids(result: dict[str, Any]) -> list[int]:
    ids: list[int] = []
    for key, value in result.items():
        if key.endswith("_id") and isinstance(value, int):
            ids.append(value)
        if key.endswith("_ids") and isinstance(value, list):
            ids.extend([item for item in value if isinstance(item, int)])
    return ids


def _extract_warnings_count(result: dict[str, Any]) -> int:
    if isinstance(result.get("warnings"), list):
        return len(result["warnings"])
    if isinstance(result.get("warnings_count"), int):
        return int(result["warnings_count"])
    if isinstance(result.get("issues_found"), int):
        return int(result["issues_found"])
    return 0


def _extract_change_count(result: dict[str, Any]) -> int:
    for key, value in result.items():
        if not isinstance(value, int):
            continue
        if key.endswith("_created") or key.endswith("_updated") or key.endswith("_count") or key.endswith("_found"):
            return value
        if key in {"updated_count", "sheets_created", "issues_found", "clashes_found"}:
            return value
    return 0


def _has_diff(results: list[dict[str, Any]]) -> bool:
    for result in results:
        if "differences" in result:
            return True
    return False


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
