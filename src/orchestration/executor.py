"""Tool executor for LLM orchestration."""

import asyncio
import json
import logging
from dataclasses import dataclass
from typing import Any

from .client import ClientConfig, GeneFlowClient
from .tools import ToolRegistry, ToolResult, get_tool_registry

logger = logging.getLogger(__name__)


@dataclass
class ExecutionContext:
    """Context for tool execution."""
    session_id: str
    user_id: str | None = None
    metadata: dict | None = None


class ToolExecutor:
    """Executes tools for LLM orchestration.

    This class provides both async and sync interfaces for executing
    bioinformatics tools. It handles parameter validation, error handling,
    and result formatting for LLM consumption.
    """

    def __init__(
        self,
        registry: ToolRegistry | None = None,
        client: GeneFlowClient | None = None,
        client_config: ClientConfig | None = None,
    ):
        self.registry = registry or get_tool_registry()
        self._client = client
        self._client_config = client_config or ClientConfig()
        self._initialized = False

    async def initialize(self) -> None:
        """Initialize the executor."""
        if self._initialized:
            return

        if self._client:
            await self._client.initialize()

        self._initialized = True
        logger.info("ToolExecutor initialized")

    async def close(self) -> None:
        """Close the executor."""
        if self._client:
            await self._client.close()
        self._initialized = False

    async def execute(
        self,
        tool_name: str,
        parameters: dict[str, Any],
        context: ExecutionContext | None = None,
    ) -> ToolResult:
        """Execute a tool by name.

        Args:
            tool_name: Name of the tool to execute
            parameters: Tool parameters
            context: Optional execution context

        Returns:
            ToolResult with success/failure and data
        """
        await self.initialize()

        # Get tool
        tool = self.registry.get(tool_name)
        if not tool:
            return ToolResult(
                success=False,
                error=f"Tool not found: {tool_name}",
            )

        # Log execution
        logger.info(f"Executing tool: {tool_name}")
        logger.debug(f"Parameters: {parameters}")

        # Execute
        try:
            result = await tool.execute(**parameters)

            # Add metadata
            if context:
                result.metadata["session_id"] = context.session_id
                if context.user_id:
                    result.metadata["user_id"] = context.user_id

            return result

        except Exception as e:
            logger.error(f"Tool execution failed: {e}")
            return ToolResult(
                success=False,
                error=f"Execution failed: {str(e)}",
            )

    def execute_sync(
        self,
        tool_name: str,
        parameters: dict[str, Any],
        context: ExecutionContext | None = None,
    ) -> ToolResult:
        """Synchronous wrapper for execute.

        Useful when integrating with synchronous code.
        """
        return asyncio.run(self.execute(tool_name, parameters, context))

    async def execute_batch(
        self,
        calls: list[tuple[str, dict[str, Any]]],
        context: ExecutionContext | None = None,
        parallel: bool = True,
    ) -> list[ToolResult]:
        """Execute multiple tools.

        Args:
            calls: List of (tool_name, parameters) tuples
            context: Optional execution context
            parallel: Execute in parallel if True

        Returns:
            List of ToolResults in same order as calls
        """
        if parallel:
            tasks = [
                self.execute(name, params, context)
                for name, params in calls
            ]
            return await asyncio.gather(*tasks)
        else:
            results = []
            for name, params in calls:
                result = await self.execute(name, params, context)
                results.append(result)
            return results

    def get_available_tools(self, category: str | None = None) -> list[dict]:
        """Get available tools in Anthropic format."""
        return self.registry.to_anthropic_tools(category)

    def get_tool_descriptions(self, category: str | None = None) -> str:
        """Get human-readable tool descriptions."""
        return self.registry.get_tool_descriptions(category)

    def parse_tool_call(self, tool_call: dict) -> tuple[str, dict]:
        """Parse a tool call from LLM response.

        Args:
            tool_call: Tool call dict with 'name' and 'input'/'arguments'

        Returns:
            Tuple of (tool_name, parameters)
        """
        name = tool_call.get("name", "")
        params = tool_call.get("input") or tool_call.get("arguments") or {}

        if isinstance(params, str):
            params = json.loads(params)

        return name, params

    async def handle_tool_call(
        self,
        tool_call: dict,
        context: ExecutionContext | None = None,
    ) -> dict:
        """Handle a tool call from LLM and return formatted response.

        Args:
            tool_call: Tool call from LLM (Anthropic or OpenAI format)
            context: Optional execution context

        Returns:
            Dict with tool result for LLM
        """
        name, params = self.parse_tool_call(tool_call)
        result = await self.execute(name, params, context)

        return {
            "tool_use_id": tool_call.get("id", ""),
            "type": "tool_result",
            "content": result.to_json(),
        }


class ConversationOrchestrator:
    """Orchestrates multi-turn conversations with tool use.

    This class manages the conversation flow between the user, LLM, and tools.
    It handles tool calls, maintains context, and supports streaming.
    """

    def __init__(
        self,
        executor: ToolExecutor | None = None,
        system_prompt: str | None = None,
    ):
        self.executor = executor or ToolExecutor()
        self.system_prompt = system_prompt or self._default_system_prompt()
        self.conversation_history: list[dict] = []

    def _default_system_prompt(self) -> str:
        """Generate default system prompt with tool descriptions."""
        tools_desc = self.executor.get_tool_descriptions()

        return f"""You are a bioinformatics assistant with access to DNA analysis tools.

You can help users analyze DNA sequences by using the following tools:

{tools_desc}

When a user asks about analyzing sequences, use the appropriate tool to provide accurate results.
Always explain the results in a clear, educational way.

Guidelines:
- If the user provides a DNA sequence, analyze it with appropriate tools
- Explain what each metric means in biological context
- Suggest follow-up analyses when relevant
- If the sequence has quality issues, recommend trimming
- Be helpful and educational

Remember: Only invoke tools when the user provides sequence data or asks for specific analyses.
"""

    def get_system_prompt(self) -> str:
        """Get the system prompt."""
        return self.system_prompt

    def get_tools(self, category: str | None = None) -> list[dict]:
        """Get tools in Anthropic format."""
        return self.executor.get_available_tools(category)

    def add_user_message(self, content: str) -> None:
        """Add a user message to history."""
        self.conversation_history.append({
            "role": "user",
            "content": content,
        })

    def add_assistant_message(self, content: str) -> None:
        """Add an assistant message to history."""
        self.conversation_history.append({
            "role": "assistant",
            "content": content,
        })

    async def handle_tool_calls(
        self,
        tool_calls: list[dict],
        context: ExecutionContext | None = None,
    ) -> list[dict]:
        """Handle multiple tool calls and return results.

        Args:
            tool_calls: List of tool calls from LLM
            context: Optional execution context

        Returns:
            List of tool results to send back to LLM
        """
        results = []
        for call in tool_calls:
            result = await self.executor.handle_tool_call(call, context)
            results.append(result)
        return results

    def clear_history(self) -> None:
        """Clear conversation history."""
        self.conversation_history = []

    def get_messages(self) -> list[dict]:
        """Get conversation messages for API call."""
        return self.conversation_history.copy()
