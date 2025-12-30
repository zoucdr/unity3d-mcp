package mcp

import (
	"sync"
)

type Resource interface {
	Url() string
	Name() string
	Description() string
	MimeType() string
}

type ResourceInfo struct {
	Url         string
	Name        string
	Description string
	MimeType    string
}

type ResourceRegistry struct {
	resources map[string]Resource
	mu        sync.RWMutex
}

func NewResourceRegistry() *ResourceRegistry {
	return &ResourceRegistry{
		resources: make(map[string]Resource),
	}
}

func (rr *ResourceRegistry) Register(resource Resource) {
	rr.mu.Lock()
	defer rr.mu.Unlock()
	rr.resources[resource.Url()] = resource
}

func (rr *ResourceRegistry) Get(url string) (Resource, bool) {
	rr.mu.RLock()
	defer rr.mu.RUnlock()
	resource, exists := rr.resources[url]
	return resource, exists
}

func (rr *ResourceRegistry) List() []Resource {
	rr.mu.RLock()
	defer rr.mu.RUnlock()

	resources := make([]Resource, 0, len(rr.resources))
	for _, resource := range rr.resources {
		resources = append(resources, resource)
	}
	return resources
}

func (rr *ResourceRegistry) GetResourceInfos() []ResourceInfo {
	rr.mu.RLock()
	defer rr.mu.RUnlock()

	infos := make([]ResourceInfo, 0, len(rr.resources))
	for _, resource := range rr.resources {
		infos = append(infos, ResourceInfo{
			Url:         resource.Url(),
			Name:        resource.Name(),
			Description: resource.Description(),
			MimeType:    resource.MimeType(),
		})
	}
	return infos
}
