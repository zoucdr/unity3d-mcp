package models

type ResourceInfo struct {
	Url         string `json:"url"`
	Name        string `json:"name"`
	Description string `json:"description"`
	MimeType    string `json:"mimeType"`
}

type PromptInfo struct {
	Name        string       `json:"name"`
	Description string       `json:"description"`
	Keys        []PromptKey  `json:"keys"`
}

type PromptKey struct {
	Key         string                 `json:"key"`
	Description string                 `json:"description"`
	Required    bool                   `json:"required"`
	Type        string                 `json:"type"`
	Default     interface{}            `json:"default,omitempty"`
	EnumValues  []string               `json:"enumValues,omitempty"`
	Examples    []string               `json:"examples,omitempty"`
	Properties  map[string]interface{} `json:"properties,omitempty"`
	ItemType    string                 `json:"itemType,omitempty"`
	Min         int                    `json:"min,omitempty"`
	Max         int                    `json:"max,omitempty"`
}

type PromptExecutionResult struct {
	PromptName string `json:"promptName"`
	Success    bool   `json:"success"`
	Message    string `json:"message"`
	Text       string `json:"text,omitempty"`
	Error      string `json:"error,omitempty"`
}
