<template>
  <div class="home">
    <header class="header">
      <h1>Unity MCP Service</h1>
      <div v-if="serverInfo" class="server-info">
        <span>{{ serverInfo.name }} v{{ serverInfo.version }}</span>
        <span class="status" :class="{ running: serverInfo.status === 'running' }">
          {{ serverInfo.status }}
        </span>
      </div>
    </header>

    <main class="main">
      <section class="tools-section">
        <h2>Available Tools</h2>
        <div v-if="loading" class="loading">Loading...</div>
        <div v-else-if="error" class="error">{{ error }}</div>
        <div v-else class="tools-list">
          <div v-for="tool in tools" :key="tool.name" class="tool-card">
            <h3>{{ tool.name }}</h3>
            <p>{{ tool.description }}</p>
            <button @click="selectTool(tool)">Execute</button>
          </div>
        </div>
      </section>

      <section class="resources-section">
        <h2>Available Resources</h2>
        <div v-if="loading" class="loading">Loading...</div>
        <div v-else-if="error" class="error">{{ error }}</div>
        <div v-else class="resources-list">
          <div v-for="resource in resources" :key="resource.url" class="resource-card">
            <h3>{{ resource.name }}</h3>
            <p>{{ resource.description }}</p>
            <p class="mimetype">Type: {{ resource.mimeType }}</p>
            <button @click="selectResource(resource)">View</button>
          </div>
        </div>
      </section>

      <section class="prompts-section">
        <h2>Available Prompts</h2>
        <div v-if="loading" class="loading">Loading...</div>
        <div v-else-if="error" class="error">{{ error }}</div>
        <div v-else class="prompts-list">
          <div v-for="prompt in prompts" :key="prompt.name" class="prompt-card">
            <h3>{{ prompt.name }}</h3>
            <p>{{ prompt.description }}</p>
            <button @click="selectPrompt(prompt)">Execute</button>
          </div>
        </div>
      </section>

      <section v-if="selectedTool" class="execution-section">
        <h2>Execute: {{ selectedTool.name }}</h2>
        <div class="tool-form">
          <div v-for="(param, key) in toolParams" :key="key" class="form-group">
            <label>{{ key }}</label>
            <input
              v-model="formData[key]"
              :type="param.type"
              :placeholder="param.description"
            />
          </div>
          <button @click="executeTool" :disabled="loading">
            {{ loading ? 'Executing...' : 'Execute' }}
          </button>
        </div>
        <div v-if="executionResult" class="result">
          <h3>Result:</h3>
          <pre>{{ JSON.stringify(executionResult, null, 2) }}</pre>
        </div>
      </section>

      <section v-if="selectedPrompt" class="prompt-execution-section">
        <h2>Execute Prompt: {{ selectedPrompt.name }}</h2>
        <div class="prompt-form">
          <div v-for="key in promptKeys" :key="key.key" class="form-group">
            <label>{{ key.key }} {{ key.required ? '(required)' : '(optional)' }}</label>
            <input
              v-model="promptFormData[key.key]"
              :placeholder="key.description"
            />
            <span v-if="key.enumValues" class="enum-hint">
              Options: {{ key.enumValues.join(', ') }}
            </span>
          </div>
          <button @click="executePrompt" :disabled="loading">
            {{ loading ? 'Executing...' : 'Execute' }}
          </button>
        </div>
        <div v-if="promptResult" class="result">
          <h3>Result:</h3>
          <pre>{{ promptResult }}</pre>
        </div>
      </section>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useMcpStore } from '../stores/mcp'

const mcpStore = useMcpStore()

const serverInfo = computed(() => mcpStore.serverInfo)
const tools = computed(() => mcpStore.tools)
const loading = computed(() => mcpStore.loading)
const error = computed(() => mcpStore.error)

const selectedTool = ref(null)
const formData = ref({})
const executionResult = ref(null)

const toolParams = computed(() => {
  if (!selectedTool.value) return {}
  return selectedTool.value.inputSchema?.properties || {}
})

const selectTool = (tool) => {
  selectedTool.value = tool
  formData.value = {}
  executionResult.value = null
}

const executeTool = async () => {
  try {
    const result = await mcpStore.executeTool(selectedTool.value.name, formData.value)
    executionResult.value = result
  } catch (err) {
    executionResult.value = { error: err.message }
  }
}

onMounted(async () => {
  await mcpStore.fetchServerInfo()
  await mcpStore.fetchTools()
})
</script>

<style scoped>
.home {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}

.header {
  margin-bottom: 30px;
  padding: 20px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.server-info {
  margin-top: 10px;
  display: flex;
  gap: 20px;
  align-items: center;
}

.status {
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  background: #f5f5f5;
}

.status.running {
  background: #4caf50;
  color: white;
}

.main {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
}

.tools-section,
.execution-section {
  background: white;
  padding: 20px;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.tools-list {
  display: grid;
  gap: 10px;
}

.tool-card {
  padding: 15px;
  border: 1px solid #e0e0e0;
  border-radius: 4px;
}

.tool-card h3 {
  margin: 0 0 10px 0;
  color: #333;
}

.tool-card p {
  margin: 0 0 10px 0;
  color: #666;
  font-size: 14px;
}

.tool-card button {
  padding: 8px 16px;
  background: #2196f3;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.tool-card button:hover {
  background: #1976d2;
}

.form-group {
  margin-bottom: 15px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
}

.form-group input {
  width: 100%;
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.tool-form button {
  width: 100%;
  padding: 10px;
  background: #4caf50;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.tool-form button:hover {
  background: #45a049;
}

.tool-form button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.result {
  margin-top: 20px;
  padding: 15px;
  background: #f5f5f5;
  border-radius: 4px;
}

.result pre {
  overflow-x: auto;
}

.loading,
.error {
  padding: 20px;
  text-align: center;
  color: #666;
}

.error {
  color: #f44336;
}
</style>
