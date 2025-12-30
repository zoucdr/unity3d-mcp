package statetree

import (
	"encoding/json"
	"sync"
)

type Context struct {
	JsonData      map[string]interface{}
	ObjectRefs    map[string]interface{}
	Completed     bool
	Result        interface{}
	mu            sync.RWMutex
	asyncReturnCh chan interface{}
}

func NewContext(jsonData map[string]interface{}, objectRefs map[string]interface{}) *Context {
	return &Context{
		JsonData:      jsonData,
		ObjectRefs:    objectRefs,
		Completed:     false,
		asyncReturnCh: make(chan interface{}, 1),
	}
}

func (ctx *Context) GetJsonValue(key string) (interface{}, bool) {
	ctx.mu.RLock()
	defer ctx.mu.RUnlock()

	if ctx.JsonData == nil {
		return nil, false
	}

	value, exists := ctx.JsonData[key]
	return value, exists
}

func (ctx *Context) GetObjectRef(key string) (interface{}, bool) {
	ctx.mu.RLock()
	defer ctx.mu.RUnlock()

	if ctx.ObjectRefs == nil {
		return nil, false
	}

	value, exists := ctx.ObjectRefs[key]
	return value, exists
}

func (ctx *Context) SetObjectRef(key string, value interface{}) {
	ctx.mu.Lock()
	defer ctx.mu.Unlock()

	if ctx.ObjectRefs == nil {
		ctx.ObjectRefs = make(map[string]interface{})
	}

	ctx.ObjectRefs[key] = value
}

func (ctx *Context) Complete(result interface{}) {
	ctx.mu.Lock()
	defer ctx.mu.Unlock()

	if !ctx.Completed {
		ctx.Completed = true
		ctx.Result = result

		select {
		case ctx.asyncReturnCh <- result:
		default:
		}
	}
}

func (ctx *Context) IsCompleted() bool {
	ctx.mu.RLock()
	defer ctx.mu.RUnlock()
	return ctx.Completed
}

func (ctx *Context) GetResult() interface{} {
	ctx.mu.RLock()
	defer ctx.mu.RUnlock()
	return ctx.Result
}

func (ctx *Context) AsyncReturn(result interface{}) interface{} {
	go func() {
		ctx.Complete(result)
	}()
	return nil
}

func (ctx *Context) GetAsyncResult() <-chan interface{} {
	return ctx.asyncReturnCh
}

func (ctx *Context) TryGet(key string) (interface{}, bool) {
	return ctx.GetJsonValue(key)
}

func (ctx *Context) Get(key string) interface{} {
	if value, exists := ctx.GetJsonValue(key); exists {
		return value
	}
	return nil
}

func (ctx *Context) GetString(key string) string {
	if value, exists := ctx.GetJsonValue(key); exists {
		if str, ok := value.(string); ok {
			return str
		}
	}
	return ""
}

func (ctx *Context) GetInt(key string) int {
	if value, exists := ctx.GetJsonValue(key); exists {
		switch v := value.(type) {
		case float64:
			return int(v)
		case int:
			return v
		case json.Number:
			if num, err := v.Int64(); err == nil {
				return int(num)
			}
		}
	}
	return 0
}

func (ctx *Context) GetFloat(key string) float64 {
	if value, exists := ctx.GetJsonValue(key); exists {
		switch v := value.(type) {
		case float64:
			return v
		case int:
			return float64(v)
		case json.Number:
			if num, err := v.Float64(); err == nil {
				return num
			}
		}
	}
	return 0.0
}

func (ctx *Context) GetBool(key string) bool {
	if value, exists := ctx.GetJsonValue(key); exists {
		if b, ok := value.(bool); ok {
			return b
		}
	}
	return false
}

func (ctx *Context) GetArray(key string) []interface{} {
	if value, exists := ctx.GetJsonValue(key); exists {
		if arr, ok := value.([]interface{}); ok {
			return arr
		}
	}
	return nil
}

func (ctx *Context) GetObject(key string) map[string]interface{} {
	if value, exists := ctx.GetJsonValue(key); exists {
		if obj, ok := value.(map[string]interface{}); ok {
			return obj
		}
	}
	return nil
}
