package mcp

type ExecutionContext struct {
	Parameters map[string]interface{}
	Metadata   map[string]interface{}
}

type ExecutionResult struct {
	ToolName string
	Success  bool
	Message  string
	Data     interface{}
	Error    string
}

func NewExecutionContext() *ExecutionContext {
	return &ExecutionContext{
		Parameters: make(map[string]interface{}),
		Metadata:   make(map[string]interface{}),
	}
}
