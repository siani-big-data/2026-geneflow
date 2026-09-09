"""Base classes for LLM tools."""

import json
from dataclasses import dataclass, field
from typing import Any, Callable


@dataclass
class ToolParameter:
    """Definition of a tool parameter."""
    name: str
    type: str  # "string", "integer", "number", "boolean", "array", "object"
    description: str
    required: bool = True
    default: Any = None
    enum: list[str] | None = None
    items_type: str | None = None  # For array types

    def to_json_schema(self) -> dict:
        """Convert to JSON Schema format."""
        schema: dict[str, Any] = {
            "type": self.type,
            "description": self.description,
        }
        if self.enum:
            schema["enum"] = self.enum
        if self.type == "array" and self.items_type:
            schema["items"] = {"type": self.items_type}
        if self.default is not None:
            schema["default"] = self.default
        return schema


@dataclass
class ToolResult:
    """Result of a tool execution."""
    success: bool
    data: Any = None
    error: str | None = None
    metadata: dict = field(default_factory=dict)

    def to_dict(self) -> dict:
        """Convert to dictionary for LLM consumption."""
        result = {
            "success": self.success,
        }
        if self.success:
            result["data"] = self.data
        else:
            result["error"] = self.error
        if self.metadata:
            result["metadata"] = self.metadata
        return result

    def to_json(self) -> str:
        """Convert to JSON string."""
        return json.dumps(self.to_dict(), indent=2, default=str)


@dataclass
class Tool:
    """Definition of an LLM-callable tool."""
    name: str
    description: str
    parameters: list[ToolParameter]
    handler: Callable[..., ToolResult]
    category: str = "general"
    examples: list[dict] = field(default_factory=list)

    def to_openai_schema(self) -> dict:
        """Convert to OpenAI function calling format."""
        properties = {}
        required = []

        for param in self.parameters:
            properties[param.name] = param.to_json_schema()
            if param.required:
                required.append(param.name)

        return {
            "type": "function",
            "function": {
                "name": self.name,
                "description": self.description,
                "parameters": {
                    "type": "object",
                    "properties": properties,
                    "required": required,
                },
            },
        }

    def to_anthropic_schema(self) -> dict:
        """Convert to Anthropic tool use format."""
        properties = {}
        required = []

        for param in self.parameters:
            properties[param.name] = param.to_json_schema()
            if param.required:
                required.append(param.name)

        return {
            "name": self.name,
            "description": self.description,
            "input_schema": {
                "type": "object",
                "properties": properties,
                "required": required,
            },
        }

    async def execute(self, **kwargs) -> ToolResult:
        """Execute the tool with given parameters."""
        try:
            # Validate required parameters
            for param in self.parameters:
                if param.required and param.name not in kwargs:
                    return ToolResult(
                        success=False,
                        error=f"Missing required parameter: {param.name}",
                    )
                # Apply defaults
                if param.name not in kwargs and param.default is not None:
                    kwargs[param.name] = param.default

            # Call handler
            result = await self.handler(**kwargs)
            return result

        except Exception as e:
            return ToolResult(
                success=False,
                error=f"Tool execution failed: {str(e)}",
            )


class ToolRegistry:
    """Registry of available tools."""

    def __init__(self):
        self._tools: dict[str, Tool] = {}
        self._categories: dict[str, list[str]] = {}

    def register(self, tool: Tool) -> None:
        """Register a tool."""
        self._tools[tool.name] = tool

        if tool.category not in self._categories:
            self._categories[tool.category] = []
        self._categories[tool.category].append(tool.name)

    def get(self, name: str) -> Tool | None:
        """Get a tool by name."""
        return self._tools.get(name)

    def list_tools(self, category: str | None = None) -> list[Tool]:
        """List all tools, optionally filtered by category."""
        if category:
            names = self._categories.get(category, [])
            return [self._tools[n] for n in names]
        return list(self._tools.values())

    def list_categories(self) -> list[str]:
        """List all categories."""
        return list(self._categories.keys())

    def to_openai_tools(self, category: str | None = None) -> list[dict]:
        """Export all tools in OpenAI format."""
        return [t.to_openai_schema() for t in self.list_tools(category)]

    def to_anthropic_tools(self, category: str | None = None) -> list[dict]:
        """Export all tools in Anthropic format."""
        return [t.to_anthropic_schema() for t in self.list_tools(category)]

    def get_tool_descriptions(self, category: str | None = None) -> str:
        """Get human-readable tool descriptions."""
        tools = self.list_tools(category)
        lines = []

        for tool in tools:
            lines.append(f"## {tool.name}")
            lines.append(f"{tool.description}")
            lines.append("\nParameters:")
            for param in tool.parameters:
                req = "(required)" if param.required else "(optional)"
                lines.append(f"  - {param.name} ({param.type}) {req}: {param.description}")
            lines.append("")

        return "\n".join(lines)
