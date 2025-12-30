package mcp

import (
	"fmt"
	"strings"
	"sync"
)

type Prompt interface {
	Name() string
	Description() string
	Keys() []PromptKey
	PromptText() string
}

type PromptKey struct {
	Key         string
	Description string
	Required    bool
	Type        string
	Default     interface{}
	EnumValues  []string
	Examples    []string
	Properties  map[string]interface{}
	ItemType    string
	Min         int
	Max         int
}

type PromptInfo struct {
	Name        string
	Description string
	Keys        []PromptKey
}

type PromptRegistry struct {
	prompts map[string]Prompt
	mu      sync.RWMutex
}

func NewPromptRegistry() *PromptRegistry {
	return &PromptRegistry{
		prompts: make(map[string]Prompt),
	}
}

func (pr *PromptRegistry) Register(prompt Prompt) {
	pr.mu.Lock()
	defer pr.mu.Unlock()
	pr.prompts[prompt.Name()] = prompt
}

func (pr *PromptRegistry) Get(name string) (Prompt, bool) {
	pr.mu.RLock()
	defer pr.mu.RUnlock()
	prompt, exists := pr.prompts[name]
	return prompt, exists
}

func (pr *PromptRegistry) List() []Prompt {
	pr.mu.RLock()
	defer pr.mu.RUnlock()

	prompts := make([]Prompt, 0, len(pr.prompts))
	for _, prompt := range pr.prompts {
		prompts = append(prompts, prompt)
	}
	return prompts
}

func (pr *PromptRegistry) GetPromptInfos() []PromptInfo {
	pr.mu.RLock()
	defer pr.mu.RUnlock()

	infos := make([]PromptInfo, 0, len(pr.prompts))
	for _, prompt := range pr.prompts {
		infos = append(infos, PromptInfo{
			Name:        prompt.Name(),
			Description: prompt.Description(),
			Keys:        prompt.Keys(),
		})
	}
	return infos
}

func (pr *PromptRegistry) GetPromptText(name string, args map[string]interface{}) (string, error) {
	pr.mu.RLock()
	defer pr.mu.RUnlock()

	prompt, exists := pr.prompts[name]
	if !exists {
		return "", fmt.Errorf("prompt '%s' not found", name)
	}

	text := prompt.PromptText()
	for key, value := range args {
		placeholder := fmt.Sprintf("{{%s}}", key)
		text = strings.ReplaceAll(text, placeholder, fmt.Sprintf("%v", value))
	}

	return text, nil
}
