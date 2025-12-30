package main

import (
	"log"

	"unity-mcp-service/internal/mcp"
	"unity-mcp-service/internal/mcp/prompts"
	"unity-mcp-service/internal/mcp/resources"
	"unity-mcp-service/internal/mcp/tools"
)

func main() {
	config := &mcp.McpServiceConfig{
		Port:          8080,
		ServerName:    "Unity MCP Server",
		ServerVersion: "1.0.0",
	}

	service := mcp.NewMcpService(config)

	service.GetToolRegistry().Register(&tools.ExampleTool{})
	service.GetResourceRegistry().Register(&resources.ExampleResource{})
	service.GetPromptRegistry().Register(&prompts.ExamplePrompt{})

	if err := service.Start(); err != nil {
		log.Fatalf("Failed to start MCP service: %v", err)
	}

	log.Println("MCP Service is running. Press Ctrl+C to stop.")
}
