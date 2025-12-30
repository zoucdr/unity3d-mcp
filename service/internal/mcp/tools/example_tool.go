package tools

import (
	"fmt"

	"unity-mcp-service/internal/mcp"
)

type ExampleTool struct{}

func (et *ExampleTool) Name() string {
	return "example_tool"
}

func (et *ExampleTool) Description() string {
	return "An example tool demonstrating the MCP framework"
}

func (et *ExampleTool) InputSchema() map[string]interface{} {
	return map[string]interface{}{
		"type": "object",
		"properties": map[string]interface{}{
			"message": map[string]interface{}{
				"type":        "string",
				"description": "A message to echo back",
			},
		},
		"required": []string{"message"},
	}
}

func (et *ExampleTool) Execute(ctx *mcp.ExecutionContext) (*mcp.ExecutionResult, error) {
	message, ok := ctx.Parameters["message"].(string)
	if !ok {
		return &mcp.ExecutionResult{
			ToolName: et.Name(),
			Success:  false,
			Message:  "Invalid parameter: message must be a string",
			Error:    "parameter type mismatch",
		}, fmt.Errorf("invalid parameter type")
	}

	return &mcp.ExecutionResult{
		ToolName: et.Name(),
		Success:  true,
		Message:  "Tool executed successfully",
		Data: map[string]interface{}{
			"echo": message,
		},
	}, nil
}
