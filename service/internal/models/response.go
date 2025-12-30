package models

import "time"

type Response struct {
	Success bool        `json:"success"`
	Message string      `json:"message"`
	Data    interface{} `json:"data,omitempty"`
}

func Success(message string, data interface{}) *Response {
	return &Response{
		Success: true,
		Message: message,
		Data:    data,
	}
}

func Error(message string) *Response {
	return &Response{
		Success: false,
		Message: message,
	}
}

type ToolInfo struct {
	Name        string                 `json:"name"`
	Description string                 `json:"description"`
	InputSchema map[string]interface{} `json:"inputSchema"`
}

type ToolParameter struct {
	Type        string      `json:"type"`
	Description string      `json:"description"`
	Required    bool        `json:"required"`
	Default     interface{} `json:"default,omitempty"`
	Enum        []string    `json:"enum,omitempty"`
	Examples    []string    `json:"examples,omitempty"`
	Properties  map[string]interface{} `json:"properties,omitempty"`
	ItemType    string      `json:"itemType,omitempty"`
	Min         *int        `json:"minimum,omitempty"`
	Max         *int        `json:"maximum,omitempty"`
}

type MethodKey struct {
	Name         string
	Description  string
	Required     bool
	Type         string
	Default      interface{}
	EnumValues   []string
	Examples     []string
	Properties   map[string]interface{}
	ItemType     string
	Min          int
	Max          int
}

type HttpRequestRecord struct {
	ID           string    `json:"id"`
	ClientID     string    `json:"clientId"`
	IP           string    `json:"ip"`
	UserAgent    string    `json:"userAgent"`
	ConnectedAt  time.Time `json:"connectedAt"`
	LastActiveAt time.Time `json:"lastActiveAt"`
	RequestCount int       `json:"requestCount"`
}

type ServerInfo struct {
	Name    string `json:"name"`
	Version string `json:"version"`
	Status  string `json:"status"`
}

type ToolExecutionResult struct {
	ToolName   string      `json:"toolName"`
	Success    bool        `json:"success"`
	Message    string      `json:"message"`
	Data       interface{} `json:"data,omitempty"`
	Error      string      `json:"error,omitempty"`
	ExecutedAt time.Time   `json:"executedAt"`
	Duration   int64       `json:"duration"`
}
