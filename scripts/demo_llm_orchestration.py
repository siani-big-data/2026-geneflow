#!/usr/bin/env python3
"""Demo script for LLM orchestration with bioinformatics tools.

This script demonstrates how to use the orchestration layer to:
1. Define bioinformatics tools for an LLM
2. Execute tools based on LLM responses
3. Handle multi-turn conversations with tool use

Usage:
    # Test tools directly (no LLM)
    python scripts/demo_llm_orchestration.py --test-tools

    # Show tool definitions (for LLM integration)
    python scripts/demo_llm_orchestration.py --show-tools

    # Interactive demo with mock LLM
    python scripts/demo_llm_orchestration.py --interactive

    # With Anthropic API (requires ANTHROPIC_API_KEY)
    python scripts/demo_llm_orchestration.py --anthropic
"""

import argparse
import asyncio
import json
import os
import sys
from pathlib import Path

# Add project root to path
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root))

from src.orchestration import (
    ToolExecutor,
    get_tool_registry,
)
from src.orchestration.executor import ConversationOrchestrator, ExecutionContext


async def test_tools():
    """Test the bioinformatics tools directly."""
    print("\n" + "=" * 60)
    print("Testing Bioinformatics Tools")
    print("=" * 60)

    executor = ToolExecutor()

    # Test sequence
    test_sequence = "ATGCGATCGATCGATCGATCGATCGTAGCTAGCTAG"
    quality_scores = [30, 32, 28, 35, 29, 31, 27, 33, 30, 28,
                      25, 22, 20, 18, 15, 12, 10, 8, 6, 5,
                      30, 32, 35, 33, 31, 29, 28, 30, 32, 34,
                      35, 33, 31, 29, 28, 30, 32]

    print(f"\nTest sequence: {test_sequence[:30]}...")
    print(f"Length: {len(test_sequence)} bp")

    # Test 1: Sequence Statistics
    print("\n--- Test 1: Sequence Statistics ---")
    result = await executor.execute(
        "sequence_statistics",
        {"sequence": test_sequence},
    )
    print(f"Success: {result.success}")
    if result.success:
        print(f"Length: {result.data['length']}")
        print(f"GC Content: {result.data['gc_percentage']}%")
        print(f"Base counts: {result.data['base_counts']}")

    # Test 2: GC Content
    print("\n--- Test 2: GC Content ---")
    result = await executor.execute(
        "calculate_gc_content",
        {"sequence": test_sequence},
    )
    print(f"Success: {result.success}")
    if result.success:
        print(f"GC: {result.data['gc_percentage']}%")
        print(f"AT: {result.data['at_percentage']}%")

    # Test 3: Reverse Complement
    print("\n--- Test 3: Reverse Complement ---")
    result = await executor.execute(
        "reverse_complement",
        {"sequence": "ATGC"},
    )
    print(f"Success: {result.success}")
    if result.success:
        print(f"Original: {result.data['original']}")
        print(f"Reverse complement: {result.data['reverse_complement']}")

    # Test 4: Motif Search
    print("\n--- Test 4: Motif Search ---")
    result = await executor.execute(
        "search_motifs",
        {"sequence": test_sequence, "pattern": "GATC"},
    )
    print(f"Success: {result.success}")
    if result.success:
        print(f"Pattern 'GATC' found {result.data['match_count']} times")
        for match in result.data["matches"][:3]:
            print(f"  Position {match['start']}-{match['end']}")

    # Test 5: Translation
    print("\n--- Test 5: Translation (frame 1) ---")
    result = await executor.execute(
        "translate_dna",
        {"sequence": test_sequence, "frame": 1},
    )
    print(f"Success: {result.success}")
    if result.success:
        print(f"Protein: {result.data['protein_sequence'][:20]}...")
        print(f"Length: {result.data['protein_length']} aa")

    # Test 6: ORF Detection
    print("\n--- Test 6: ORF Detection ---")
    # Use a sequence with a clear ORF
    orf_sequence = "ATGAAACCCGGGTTTTAAATAGCCC" * 5  # Starts with ATG
    result = await executor.execute(
        "detect_orfs",
        {"sequence": orf_sequence, "min_length": 10},
    )
    print(f"Success: {result.success}")
    if result.success:
        print(f"Total ORFs: {result.data['total_orfs']}")
        print(f"Longest ORF: {result.data['longest_orf_length']} bp")

    print("\n" + "=" * 60)
    print("All tool tests completed!")
    print("=" * 60)


def show_tools():
    """Show tool definitions for LLM integration."""
    registry = get_tool_registry()

    print("\n" + "=" * 60)
    print("Available Bioinformatics Tools")
    print("=" * 60)

    # Show human-readable descriptions
    print("\n--- Tool Descriptions ---")
    print(registry.get_tool_descriptions())

    # Show Anthropic format
    print("\n--- Anthropic Tool Format ---")
    tools = registry.to_anthropic_tools()
    print(json.dumps(tools, indent=2))

    # Show OpenAI format
    print("\n--- OpenAI Tool Format ---")
    tools = registry.to_openai_tools()
    print(json.dumps(tools, indent=2))


async def interactive_demo():
    """Interactive demo with mock tool execution."""
    print("\n" + "=" * 60)
    print("Interactive Bioinformatics Assistant Demo")
    print("=" * 60)
    print("\nThis demo simulates how an LLM would use the tools.")
    print("Enter DNA sequences or analysis requests.")
    print("Type 'quit' to exit.\n")

    executor = ToolExecutor()

    # Example queries and their tool mappings
    examples = [
        ("ATGCGATCGATCGATCGATCG", "sequence_statistics"),
        ("What's the GC content of ATGCGCGCATAT?", "calculate_gc_content"),
        ("Find GATC in ATGGATCGATCGATCGATC", "search_motifs"),
        ("Translate ATGAAACCCGGG", "translate_dna"),
    ]

    print("Example queries you can try:")
    for query, tool in examples:
        print(f"  - '{query}' → uses {tool}")

    print()

    while True:
        try:
            user_input = input("You: ").strip()
            if not user_input:
                continue
            if user_input.lower() == "quit":
                break

            # Simple pattern matching for demo
            sequence = None
            tool_name = None
            params = {}

            # Extract sequence (uppercase DNA letters)
            import re
            seq_match = re.search(r"[ACGT]{6,}", user_input.upper())
            if seq_match:
                sequence = seq_match.group()

            # Determine tool based on keywords
            input_lower = user_input.lower()
            if "gc" in input_lower or "content" in input_lower:
                tool_name = "calculate_gc_content"
                params = {"sequence": sequence}
            elif "find" in input_lower or "motif" in input_lower or "search" in input_lower:
                tool_name = "search_motifs"
                # Try to find pattern
                pattern_match = re.search(r"find\s+([ACGT]+)", user_input.upper())
                pattern = pattern_match.group(1) if pattern_match else "GATC"
                params = {"sequence": sequence, "pattern": pattern}
            elif "translate" in input_lower or "protein" in input_lower:
                tool_name = "translate_dna"
                params = {"sequence": sequence, "frame": 1}
            elif "reverse" in input_lower or "complement" in input_lower:
                tool_name = "reverse_complement"
                params = {"sequence": sequence}
            elif "orf" in input_lower or "reading frame" in input_lower:
                tool_name = "detect_orfs"
                params = {"sequence": sequence, "min_length": 10}
            elif sequence:
                # Default to statistics
                tool_name = "sequence_statistics"
                params = {"sequence": sequence}

            if not sequence:
                print("Assistant: I didn't find a DNA sequence in your input. Please provide a sequence with A, C, G, T characters.")
                continue

            if not tool_name:
                print("Assistant: I'm not sure what analysis you want. Try asking about GC content, motifs, translation, or ORFs.")
                continue

            # Execute tool
            print(f"\n[Using tool: {tool_name}]")
            result = await executor.execute(tool_name, params)

            if result.success:
                print(f"\nAssistant: Here are the results:\n")
                print(json.dumps(result.data, indent=2))
            else:
                print(f"\nAssistant: Analysis failed: {result.error}")

            print()

        except KeyboardInterrupt:
            print("\nGoodbye!")
            break
        except Exception as e:
            print(f"Error: {e}")


async def anthropic_demo():
    """Demo with actual Anthropic API."""
    try:
        import anthropic
    except ImportError:
        print("Error: anthropic package not installed.")
        print("Install with: pip install anthropic")
        return

    api_key = os.environ.get("ANTHROPIC_API_KEY")
    if not api_key:
        print("Error: ANTHROPIC_API_KEY environment variable not set.")
        return

    print("\n" + "=" * 60)
    print("Anthropic API Integration Demo")
    print("=" * 60)

    client = anthropic.Anthropic(api_key=api_key)
    orchestrator = ConversationOrchestrator()
    executor = orchestrator.executor

    print("\nType your bioinformatics questions. Type 'quit' to exit.\n")
    print("Example: 'Analyze this sequence: ATGCGATCGATCGATCGATCG'")
    print()

    context = ExecutionContext(session_id="demo-session")

    while True:
        try:
            user_input = input("You: ").strip()
            if not user_input:
                continue
            if user_input.lower() == "quit":
                break

            orchestrator.add_user_message(user_input)

            # Call Anthropic API with tools
            response = client.messages.create(
                model="claude-sonnet-4-20250514",
                max_tokens=1024,
                system=orchestrator.get_system_prompt(),
                tools=orchestrator.get_tools(),
                messages=orchestrator.get_messages(),
            )

            # Handle response
            assistant_content = []
            tool_calls = []

            for block in response.content:
                if block.type == "text":
                    assistant_content.append(block.text)
                elif block.type == "tool_use":
                    tool_calls.append({
                        "id": block.id,
                        "name": block.name,
                        "input": block.input,
                    })

            # If there are tool calls, execute them
            if tool_calls:
                print("\n[Executing tools...]")
                tool_results = await orchestrator.handle_tool_calls(tool_calls, context)

                # Add tool results to conversation
                for tc, tr in zip(tool_calls, tool_results):
                    print(f"  - {tc['name']}: ", end="")
                    result_data = json.loads(tr["content"])
                    if result_data.get("success"):
                        print("Success")
                    else:
                        print(f"Failed: {result_data.get('error')}")

                # Make another API call with tool results
                orchestrator.conversation_history.append({
                    "role": "assistant",
                    "content": response.content,
                })
                orchestrator.conversation_history.append({
                    "role": "user",
                    "content": tool_results,
                })

                # Get final response
                final_response = client.messages.create(
                    model="claude-sonnet-4-20250514",
                    max_tokens=1024,
                    system=orchestrator.get_system_prompt(),
                    messages=orchestrator.get_messages(),
                )

                final_text = "".join(
                    b.text for b in final_response.content if b.type == "text"
                )
                print(f"\nAssistant: {final_text}")
                orchestrator.add_assistant_message(final_text)

            else:
                # No tool calls, just text response
                response_text = " ".join(assistant_content)
                print(f"\nAssistant: {response_text}")
                orchestrator.add_assistant_message(response_text)

            print()

        except KeyboardInterrupt:
            print("\nGoodbye!")
            break
        except Exception as e:
            print(f"Error: {e}")


def main():
    parser = argparse.ArgumentParser(
        description="Demo LLM orchestration with bioinformatics tools",
    )
    parser.add_argument(
        "--test-tools",
        action="stoREDACTED",
        help="Test tools directly without LLM",
    )
    parser.add_argument(
        "--show-tools",
        action="stoREDACTED",
        help="Show tool definitions for LLM integration",
    )
    parser.add_argument(
        "--interactive",
        action="stoREDACTED",
        help="Interactive demo with mock tool selection",
    )
    parser.add_argument(
        "--anthropic",
        action="stoREDACTED",
        help="Demo with Anthropic API (requires ANTHROPIC_API_KEY)",
    )

    args = parser.parse_args()

    if args.test_tools:
        asyncio.run(test_tools())
    elif args.show_tools:
        show_tools()
    elif args.interactive:
        asyncio.run(interactive_demo())
    elif args.anthropic:
        asyncio.run(anthropic_demo())
    else:
        parser.print_help()
        print("\nRun with --test-tools to test the bioinformatics tools directly.")


if __name__ == "__main__":
    main()
