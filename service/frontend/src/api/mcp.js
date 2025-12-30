import api from './index'

export const mcpApi = {
  getHealth() {
    return api.get('/health')
  },

  getServerInfo() {
    return api.get('/server/info')
  },

  getTools() {
    return api.get('/tools')
  },

  executeTool(name, params) {
    return api.post(`/tools/${name}/execute`, params)
  },

  getToolSchema(name) {
    return api.get(`/tools/${name}/schema`)
  },

  getResources() {
    return api.get('/resources')
  },

  getResource(url) {
    return api.get(`/resources/${url}`)
  },

  getPrompts() {
    return api.get('/prompts')
  },

  getPrompt(name) {
    return api.get(`/prompts/${name}`)
  },

  executePrompt(name, args) {
    return api.post(`/prompts/${name}/execute`, args)
  }
}
