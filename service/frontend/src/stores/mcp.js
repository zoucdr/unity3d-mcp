import { defineStore } from 'pinia'
import { mcpApi } from '../api/mcp'

export const useMcpStore = defineStore('mcp', {
  state: () => ({
    serverInfo: null,
    tools: [],
    resources: [],
    prompts: [],
    loading: false,
    error: null
  }),

  actions: {
    async fetchServerInfo() {
      this.loading = true
      this.error = null
      try {
        const response = await mcpApi.getServerInfo()
        if (response.success) {
          this.serverInfo = response.data
        }
      } catch (err) {
        this.error = err.message
      } finally {
        this.loading = false
      }
    },

    async fetchTools() {
      this.loading = true
      this.error = null
      try {
        const response = await mcpApi.getTools()
        if (response.success) {
          this.tools = response.data
        }
      } catch (err) {
        this.error = err.message
      } finally {
        this.loading = false
      }
    },

    async fetchResources() {
      this.loading = true
      this.error = null
      try {
        const response = await mcpApi.getResources()
        if (response.success) {
          this.resources = response.data
        }
      } catch (err) {
        this.error = err.message
      } finally {
        this.loading = false
      }
    },

    async fetchPrompts() {
      this.loading = true
      this.error = null
      try {
        const response = await mcpApi.getPrompts()
        if (response.success) {
          this.prompts = response.data
        }
      } catch (err) {
        this.error = err.message
      } finally {
        this.loading = false
      }
    },

    async executeTool(name, params) {
      this.loading = true
      this.error = null
      try {
        const response = await mcpApi.executeTool(name, params)
        return response
      } catch (err) {
        this.error = err.message
        throw err
      } finally {
        this.loading = false
      }
    },

    async executePrompt(name, args) {
      this.loading = true
      this.error = null
      try {
        const response = await mcpApi.executePrompt(name, args)
        return response
      } catch (err) {
        this.error = err.message
        throw err
      } finally {
        this.loading = false
      }
    }
  }
})
