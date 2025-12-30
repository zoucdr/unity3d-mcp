package mcp

import (
	"fmt"
	"log"
	"net/http"
	"sync"

	"github.com/gin-gonic/gin"
	"unity-mcp-service/internal/models"
	"unity-mcp-service/internal/statetree"
)

type McpService struct {
	registry         *ToolRegistry
	resourceRegistry *ResourceRegistry
	promptRegistry   *PromptRegistry
	stateTree        *statetree.StateTree
	server           *http.Server
	port             int
	serverName       string
	serverVersion    string
	mu               sync.RWMutex
	isRunning        bool
}

type McpServiceConfig struct {
	Port          int
	ServerName    string
	ServerVersion string
}

func NewMcpService(config *McpServiceConfig) *McpService {
	if config == nil {
		config = &McpServiceConfig{
			Port:          8080,
			ServerName:    "Unity MCP Server",
			ServerVersion: "1.0.0",
		}
	}

	return &McpService{
		registry:         NewToolRegistry(),
		resourceRegistry: NewResourceRegistry(),
		promptRegistry:   NewPromptRegistry(),
		stateTree:        statetree.New(),
		port:             config.Port,
		serverName:       config.ServerName,
		serverVersion:    config.ServerVersion,
	}
}

func (ms *McpService) GetToolRegistry() *ToolRegistry {
	return ms.registry
}

func (ms *McpService) GetResourceRegistry() *ResourceRegistry {
	return ms.resourceRegistry
}

func (ms *McpService) GetPromptRegistry() *PromptRegistry {
	return ms.promptRegistry
}

func (ms *McpService) GetStateTree() *statetree.StateTree {
	return ms.stateTree
}

func (ms *McpService) Start() error {
	ms.mu.Lock()
	defer ms.mu.Unlock()

	if ms.isRunning {
		return fmt.Errorf("service is already running")
	}

	gin.SetMode(gin.ReleaseMode)
	router := gin.Default()

	ms.setupRoutes(router)

	ms.server = &http.Server{
		Addr:    fmt.Sprintf(":%d", ms.port),
		Handler: router,
	}

	go func() {
		log.Printf("[MCP] Starting server on port %d", ms.port)
		if err := ms.server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Printf("[MCP] Server error: %v", err)
		}
	}()

	ms.isRunning = true
	log.Printf("[MCP] Server started successfully")
	return nil
}

func (ms *McpService) Stop() error {
	ms.mu.Lock()
	defer ms.mu.Unlock()

	if !ms.isRunning {
		return fmt.Errorf("service is not running")
	}

	if ms.server != nil {
		if err := ms.server.Close(); err != nil {
			log.Printf("[MCP] Error stopping server: %v", err)
			return err
		}
	}

	ms.isRunning = false
	log.Printf("[MCP] Server stopped")
	return nil
}

func (ms *McpService) IsRunning() bool {
	ms.mu.RLock()
	defer ms.mu.RUnlock()
	return ms.isRunning
}

func (ms *McpService) setupRoutes(router *gin.Engine) {
	router.GET("/health", ms.handleHealth)
	router.GET("/server/info", ms.handleServerInfo)
	
	router.GET("/tools", ms.handleListTools)
	router.POST("/tools/:name/execute", ms.handleExecuteTool)
	router.GET("/tools/:name/schema", ms.handleGetToolSchema)
	
	router.GET("/resources", ms.handleListResources)
	router.GET("/resources/:url", ms.handleGetResource)
	
	router.GET("/prompts", ms.handleListPrompts)
	router.GET("/prompts/:name", ms.handleGetPrompt)
	router.POST("/prompts/:name/execute", ms.handleExecutePrompt)
}

func (ms *McpService) handleHealth(c *gin.Context) {
	c.JSON(http.StatusOK, models.Success("OK", map[string]interface{}{
		"status": "healthy",
	}))
}

func (ms *McpService) handleServerInfo(c *gin.Context) {
	c.JSON(http.StatusOK, models.Success("Server info", &models.ServerInfo{
		Name:    ms.serverName,
		Version: ms.serverVersion,
		Status:  "running",
	}))
}

func (ms *McpService) handleListTools(c *gin.Context) {
	tools := ms.registry.GetToolInfos()
	c.JSON(http.StatusOK, models.Success("Tools retrieved", tools))
}

func (ms *McpService) handleExecuteTool(c *gin.Context) {
	toolName := c.Param("name")

	tool, exists := ms.registry.Get(toolName)
	if !exists {
		c.JSON(http.StatusNotFound, models.Error(fmt.Sprintf("Tool '%s' not found", toolName)))
		return
	}

	var params map[string]interface{}
	if err := c.ShouldBindJSON(&params); err != nil {
		c.JSON(http.StatusBadRequest, models.Error(fmt.Sprintf("Invalid parameters: %v", err)))
		return
	}

	ctx := NewExecutionContext()
	ctx.Parameters = params

	result, err := tool.Execute(ctx)
	if err != nil {
		c.JSON(http.StatusInternalServerError, models.Error(fmt.Sprintf("Execution failed: %v", err)))
		return
	}

	c.JSON(http.StatusOK, models.Success("Tool executed successfully", result))
}

func (ms *McpService) handleGetToolSchema(c *gin.Context) {
	toolName := c.Param("name")

	tool, exists := ms.registry.Get(toolName)
	if !exists {
		c.JSON(http.StatusNotFound, models.Error(fmt.Sprintf("Tool '%s' not found", toolName)))
		return
	}

	schema := tool.InputSchema()
	c.JSON(http.StatusOK, models.Success("Schema retrieved", schema))
}

func (ms *McpService) handleListResources(c *gin.Context) {
	resources := ms.resourceRegistry.GetResourceInfos()
	c.JSON(http.StatusOK, models.Success("Resources retrieved", resources))
}

func (ms *McpService) handleGetResource(c *gin.Context) {
	url := c.Param("url")

	resource, exists := ms.resourceRegistry.Get(url)
	if !exists {
		c.JSON(http.StatusNotFound, models.Error(fmt.Sprintf("Resource '%s' not found", url)))
		return
	}

	c.JSON(http.StatusOK, models.Success("Resource retrieved", map[string]interface{}{
		"url":         resource.Url(),
		"name":        resource.Name(),
		"description": resource.Description(),
		"mimeType":    resource.MimeType(),
	}))
}

func (ms *McpService) handleListPrompts(c *gin.Context) {
	prompts := ms.promptRegistry.GetPromptInfos()
	c.JSON(http.StatusOK, models.Success("Prompts retrieved", prompts))
}

func (ms *McpService) handleGetPrompt(c *gin.Context) {
	name := c.Param("name")

	prompt, exists := ms.promptRegistry.Get(name)
	if !exists {
		c.JSON(http.StatusNotFound, models.Error(fmt.Sprintf("Prompt '%s' not found", name)))
		return
	}

	c.JSON(http.StatusOK, models.Success("Prompt retrieved", map[string]interface{}{
		"name":        prompt.Name(),
		"description": prompt.Description(),
		"keys":        prompt.Keys(),
	}))
}

func (ms *McpService) handleExecutePrompt(c *gin.Context) {
	name := c.Param("name")

	var args map[string]interface{}
	if err := c.ShouldBindJSON(&args); err != nil {
		c.JSON(http.StatusBadRequest, models.Error(fmt.Sprintf("Invalid arguments: %v", err)))
		return
	}

	text, err := ms.promptRegistry.GetPromptText(name, args)
	if err != nil {
		c.JSON(http.StatusInternalServerError, models.Error(fmt.Sprintf("Prompt execution failed: %v", err)))
		return
	}

	c.JSON(http.StatusOK, models.Success("Prompt executed successfully", map[string]interface{}{
		"promptName": name,
		"text":       text,
	}))
}
