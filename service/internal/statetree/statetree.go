package statetree

import (
	"encoding/json"
	"fmt"
	"strings"
)

const (
	DefaultKey = "*"
)

type ContextFunc func(ctx *Context) interface{}
type JsonFunc func(data map[string]interface{}) interface{}

type StateTree struct {
	Key            string
	Select         map[interface{}]*StateTree
	OptionalParams map[string]bool
	ContextFunc    ContextFunc
	JsonFunc       JsonFunc
	ErrorMessage   string
}

func New() *StateTree {
	return &StateTree{
		Select:         make(map[interface{}]*StateTree),
		OptionalParams: make(map[string]bool),
	}
}

func (st *StateTree) Run(ctx *Context) interface{} {
	current := st

	for current.ContextFunc == nil && current.JsonFunc == nil {
		keyToLookup := interface{}(DefaultKey)
		var next *StateTree

		if current.Key != "" && ctx != nil {
			if token, exists := ctx.GetJsonValue(current.Key); exists {
				keyToLookup = convertTokenToKey(token)
				if n, ok := current.Select[keyToLookup]; ok {
					next = n
				}
			}
		}

		if next == nil && ctx != nil {
			for k, v := range current.Select {
				if k == nil {
					continue
				}

				key := fmt.Sprintf("%v", k)
				if current.OptionalParams[key] {
					if paramToken, exists := ctx.GetJsonValue(key); exists && paramToken != nil {
						if !isNil(paramToken) && !isEmptyString(paramToken) {
							next = v
							break
						}
					}
				}
			}

			if next == nil {
				if n, ok := current.Select[DefaultKey]; ok {
					next = n
				}
			}
		}

		if next == nil {
			supportedKeys := []string{}
			for k := range current.Select {
				keyStr := fmt.Sprintf("%v", k)
				if keyStr != DefaultKey && !current.OptionalParams[keyStr] {
					supportedKeys = append(supportedKeys, keyStr)
				}
			}

			for k := range current.Select {
				keyStr := fmt.Sprintf("%v", k)
				if current.OptionalParams[keyStr] {
					supportedKeys = append(supportedKeys, keyStr+" (optional)")
				}
			}

			supportedKeysList := "none"
			if len(supportedKeys) > 0 {
				supportedKeysList = strings.Join(supportedKeys, ", ")
			}

			current.ErrorMessage = fmt.Sprintf("Invalid value '%v' for key '%s'. Supported values: [%s]", keyToLookup, current.Key, supportedKeysList)
			return nil
		}

		current = next
	}

	if current.ContextFunc != nil {
		return current.ContextFunc(ctx)
	}

	if current.JsonFunc != nil {
		return current.JsonFunc(ctx.JsonData)
	}

	return nil
}

func convertTokenToKey(token interface{}) interface{} {
	if token == nil {
		return DefaultKey
	}

	switch v := token.(type) {
	case float64:
		if v == float64(int64(v)) {
			return int64(v)
		}
		return v
	case bool:
		return v
	case string:
		if v == "" {
			return DefaultKey
		}
		return v
	case map[string]interface{}:
		if data, ok := v["Value"].(string); ok && data != "" {
			return data
		}
		return v
	default:
		return v
	}
}

func isNil(v interface{}) bool {
	if v == nil {
		return true
	}

	switch v.(type) {
	case json.Number:
		return false
	case map[string]interface{}:
		return false
	case []interface{}:
		return false
	default:
		return v == nil
	}
}

func isEmptyString(v interface{}) bool {
	if str, ok := v.(string); ok {
		return str == ""
	}
	return false
}

func (st *StateTree) String() string {
	var sb strings.Builder
	st.print(&sb, "", true, "")
	return sb.String()
}

func (st *StateTree) print(sb *strings.Builder, indent string, last bool, parentEdgeLabel string) {
	if indent == "" {
		sb.WriteString("StateTree\n")
	}

	edgesIndent := indent
	if st.Key != "" && st.Key != parentEdgeLabel {
		sb.WriteString(fmt.Sprintf("%s└─ %s:\n", indent, st.Key))
		edgesIndent = indent + "   "
	}

	entries := make([]struct {
		key   interface{}
		value *StateTree
	}, 0, len(st.Select))

	for k, v := range st.Select {
		entries = append(entries, struct {
			key   interface{}
			value *StateTree
		}{k, v})
	}

	for i, entry := range entries {
		isLastChild := i == len(entries)-1
		connector := "└─"
		if !isLastChild {
			connector = "├─"
		}

		label := fmt.Sprintf("%v", entry.key)
		if label == DefaultKey {
			label = "*"
		}

		if st.OptionalParams[label] {
			label = label + "(option)"
		}

		if entry.value.ContextFunc != nil || entry.value.JsonFunc != nil {
			actionName := "Anonymous"
			if entry.value.ContextFunc != nil {
				actionName = "ContextFunc"
			} else if entry.value.JsonFunc != nil {
				actionName = "JsonFunc"
			}
			sb.WriteString(fmt.Sprintf("%s%s %s → %s\n", edgesIndent, connector, label, actionName))
		} else {
			sb.WriteString(fmt.Sprintf("%s%s %s\n", edgesIndent, connector, label))
			nextIndent := edgesIndent
			if isLastChild {
				nextIndent = edgesIndent + "   "
			} else {
				nextIndent = edgesIndent + "│  "
			}
			entry.value.print(sb, nextIndent, isLastChild, label)
		}
	}
}
