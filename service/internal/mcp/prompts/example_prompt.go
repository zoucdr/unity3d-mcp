package prompts

import (
	"unity-mcp-service/internal/mcp"
)

type ExamplePrompt struct{}

func (ep *ExamplePrompt) Name() string {
	return "example_prompt"
}

func (ep *ExamplePrompt) Description() string {
	return "An example prompt demonstrating the MCP framework"
}

func (ep *ExamplePrompt) Keys() []mcp.PromptKey {
	return []mcp.PromptKey{
		{
			Key:         "name",
			Description: "The name to use in the prompt",
			Required:    true,
			Type:        "string",
			Examples:    []string{"Alice", "Bob", "Charlie"},
		},
		{
			Key:         "action",
			Description: "The action to perform",
			Required:    false,
			Type:        "string",
			Default:     "greet",
			EnumValues:  []string{"greet", "farewell", "thank"},
		},
	}
}

func (ep *ExamplePrompt) PromptText() string {
	return "Hello {{name}}, I would like to {{action}} you."
}
