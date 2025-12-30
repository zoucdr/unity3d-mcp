package mcp

import (
	"sync"
)

type Tool interface {
	Name() string
	Description() string
	InputSchema() map[string]interface{}
	Execute(ctx *ExecutionContext) (*ExecutionResult, error)
}

type ToolInfo struct {
	Name        string
	Description string
	InputSchema map[string]interface{}
}

type ToolRegistry struct {
	tools map[string]Tool
	mu    sync.RWMutex
}

func NewToolRegistry() *ToolRegistry {
	return &ToolRegistry{
		tools: make(map[string]Tool),
	}
}

func (tr *ToolRegistry) Register(tool Tool) {
	tr.mu.Lock()
	defer tr.mu.Unlock()
	tr.tools[tool.Name()] = tool
}

func (tr *ToolRegistry) Get(name string) (Tool, bool) {
	tr.mu.RLock()
	defer tr.mu.RUnlock()
	tool, exists := tr.tools[name]
	return tool, exists
}

func (tr *ToolRegistry) List() []Tool {
	tr.mu.RLock()
	defer tr.mu.RUnlock()

	tools := make([]Tool, 0, len(tr.tools))
	for _, tool := range tr.tools {
		tools = append(tools, tool)
	}
	return tools
}

func (tr *ToolRegistry) GetToolInfos() []ToolInfo {
	tr.mu.RLock()
	defer tr.mu.RUnlock()

	infos := make([]ToolInfo, 0, len(tr.tools))
	for _, tool := range tr.tools {
		infos = append(infos, ToolInfo{
			Name:        tool.Name(),
			Description: tool.Description(),
			InputSchema: tool.InputSchema(),
		})
	}
	return infos
}
