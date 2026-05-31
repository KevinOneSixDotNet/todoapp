<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { isAxiosError } from 'axios'
import api from '../api'
import type { Todo } from '../types'

// ── State ──────────────────────────────────────────────────────────────────
const todos = ref<Todo[]>([])
const loading = ref(true)
const pageError = ref('')
const filter = ref<'all' | 'active' | 'completed'>('all')

// ── Create form ────────────────────────────────────────────────────────────
const showCreateForm = ref(false)
const submittingCreate = ref(false)
const createForm = reactive({ title: '', description: '', dueDate: todayStr() })
const createError = ref('')

// ── Inline edit ────────────────────────────────────────────────────────────
const editingId = ref<string | null>(null)
const editForm = reactive({ title: '', description: '', dueDate: '' })
const editError = ref('')
const submittingEdit = ref(false)

// ── Delete tracking ────────────────────────────────────────────────────────
const deletingId = ref<string | null>(null)

// ── Computed ───────────────────────────────────────────────────────────────
const filteredTodos = computed(() => {
  if (filter.value === 'active') return todos.value.filter(t => !t.isComplete)
  if (filter.value === 'completed') return todos.value.filter(t => t.isComplete)
  return todos.value
})

const counts = computed(() => ({
  all: todos.value.length,
  active: todos.value.filter(t => !t.isComplete).length,
  completed: todos.value.filter(t => t.isComplete).length,
}))

// ── Helpers ────────────────────────────────────────────────────────────────
function todayStr() {
  return new Date().toISOString().slice(0, 10)
}

function toIso(dateStr: string) {
  // Parse as local midnight so the displayed date matches what the user picked
  const [y, m, d] = dateStr.split('-').map(Number)
  return new Date(y, m - 1, d).toISOString()
}

function formatDate(iso: string) {
  const [y, m, d] = iso.slice(0, 10).split('-').map(Number)
  return new Date(y, m - 1, d).toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
  })
}

function isOverdue(iso: string, isComplete: boolean): boolean {
  if (isComplete) return false
  const [y, m, d] = iso.slice(0, 10).split('-').map(Number)
  const due = new Date(y, m - 1, d)
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  return due < today
}

// ── API ────────────────────────────────────────────────────────────────────
async function fetchTodos() {
  loading.value = true
  pageError.value = ''
  try {
    const { data } = await api.get<Todo[]>('/api/todos')
    todos.value = data
  } catch {
    pageError.value = 'Failed to load tasks. Please refresh.'
  } finally {
    loading.value = false
  }
}

function openCreateForm() {
  showCreateForm.value = true
  createForm.title = ''
  createForm.description = ''
  createForm.dueDate = todayStr()
  createError.value = ''
}

async function submitCreate() {
  createError.value = ''
  if (!createForm.title.trim()) { createError.value = 'Title is required.'; return }
  submittingCreate.value = true
  try {
    const { data } = await api.post<Todo>('/api/todos', {
      title: createForm.title.trim(),
      description: createForm.description.trim() || null,
      dueDate: toIso(createForm.dueDate),
    })
    todos.value.unshift(data)
    showCreateForm.value = false
  } catch (e) {
    createError.value = isAxiosError(e)
      ? (e.response?.data?.title ?? 'Failed to create task.')
      : 'Failed to create task.'
  } finally {
    submittingCreate.value = false
  }
}

function startEdit(todo: Todo) {
  editingId.value = todo.id
  editForm.title = todo.title
  editForm.description = todo.description ?? ''
  editForm.dueDate = todo.dueDate.slice(0, 10)
  editError.value = ''
}

async function submitEdit(todo: Todo) {
  editError.value = ''
  if (!editForm.title.trim()) { editError.value = 'Title is required.'; return }
  submittingEdit.value = true
  try {
    const { data } = await api.put<Todo>(`/api/todos/${todo.id}`, {
      title: editForm.title.trim(),
      description: editForm.description.trim() || null,
      dueDate: toIso(editForm.dueDate),
      isComplete: todo.isComplete,
    })
    const idx = todos.value.findIndex(t => t.id === todo.id)
    if (idx !== -1) todos.value[idx] = data
    editingId.value = null
  } catch (e) {
    editError.value = isAxiosError(e)
      ? (e.response?.data?.title ?? 'Failed to update task.')
      : 'Failed to update task.'
  } finally {
    submittingEdit.value = false
  }
}

async function toggleComplete(todo: Todo) {
  const idx = todos.value.findIndex(t => t.id === todo.id)
  if (idx === -1) return
  // Optimistic update
  todos.value[idx] = { ...todo, isComplete: !todo.isComplete }
  try {
    const { data } = await api.put<Todo>(`/api/todos/${todo.id}`, {
      title: todo.title,
      description: todo.description,
      dueDate: todo.dueDate,
      isComplete: !todo.isComplete,
    })
    todos.value[idx] = data
  } catch {
    todos.value[idx] = todo // revert
  }
}

async function deleteTodo(id: string) {
  deletingId.value = id
  try {
    await api.delete(`/api/todos/${id}`)
    todos.value = todos.value.filter(t => t.id !== id)
  } catch (e) {
    pageError.value = isAxiosError(e)
      ? (e.response?.data?.title ?? 'Failed to delete task.')
      : 'Failed to delete task.'
  } finally {
    deletingId.value = null
  }
}

onMounted(fetchTodos)
</script>

<template>
  <div class="max-w-2xl mx-auto px-4 py-10">

    <!-- Page header -->
    <div class="flex items-center justify-between mb-8">
      <h1 class="font-serif text-3xl text-charcoal">My Tasks</h1>
      <button
        v-if="!showCreateForm"
        @click="openCreateForm"
        class="px-4 py-2 bg-primary text-white text-sm font-medium rounded hover:bg-primary-hover transition-colors"
      >
        + New Task
      </button>
    </div>

    <!-- Page-level error -->
    <div v-if="pageError" class="text-sm text-error bg-error-light px-4 py-3 rounded mb-5 flex justify-between">
      <span>{{ pageError }}</span>
      <button @click="pageError = ''" class="ml-4 opacity-60 hover:opacity-100">✕</button>
    </div>

    <!-- Create form -->
    <div v-if="showCreateForm" class="bg-card rounded-lg shadow-card p-6 mb-6 border border-taupe-light">
      <h2 class="font-serif text-lg text-charcoal mb-4">New Task</h2>
      <form @submit.prevent="submitCreate" class="space-y-4">
        <div>
          <label class="block text-xs font-medium text-charcoal mb-1 uppercase tracking-wide">
            Title <span class="text-error">*</span>
          </label>
          <input
            v-model="createForm.title"
            type="text"
            maxlength="100"
            autofocus
            class="w-full px-3 py-2.5 border border-taupe-light rounded text-charcoal placeholder-taupe focus:outline-none focus:border-charcoal transition-colors text-sm"
            placeholder="What needs to be done?"
          />
        </div>
        <div>
          <label class="block text-xs font-medium text-charcoal mb-1 uppercase tracking-wide">Description</label>
          <textarea
            v-model="createForm.description"
            rows="2"
            class="w-full px-3 py-2.5 border border-taupe-light rounded text-charcoal placeholder-taupe focus:outline-none focus:border-charcoal transition-colors resize-none text-sm"
            placeholder="Optional notes…"
          />
        </div>
        <div>
          <label class="block text-xs font-medium text-charcoal mb-1 uppercase tracking-wide">Due date</label>
          <input
            v-model="createForm.dueDate"
            type="date"
            class="px-3 py-2 border border-taupe-light rounded text-charcoal text-sm focus:outline-none focus:border-charcoal transition-colors"
          />
        </div>
        <div v-if="createError" class="text-sm text-error bg-error-light px-3 py-2 rounded">
          {{ createError }}
        </div>
        <div class="flex gap-3 pt-1">
          <button
            type="submit"
            :disabled="submittingCreate"
            class="px-4 py-2 bg-primary text-white text-sm font-medium rounded hover:bg-primary-hover disabled:opacity-50 transition-colors"
          >
            {{ submittingCreate ? 'Adding…' : 'Add Task' }}
          </button>
          <button
            type="button"
            @click="showCreateForm = false"
            class="px-4 py-2 text-sm text-taupe hover:text-charcoal transition-colors"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>

    <!-- Filter tabs -->
    <div class="flex gap-0 mb-5 border-b border-taupe-light">
      <button
        v-for="tab in (['all', 'active', 'completed'] as const)"
        :key="tab"
        @click="filter = tab"
        :class="[
          'px-4 py-2.5 text-sm capitalize -mb-px border-b-2 transition-colors',
          filter === tab
            ? 'border-charcoal text-charcoal font-medium'
            : 'border-transparent text-taupe hover:text-charcoal',
        ]"
      >
        {{ tab }}
        <span class="ml-1 text-xs" :class="filter === tab ? 'text-taupe' : 'text-taupe-light'">
          {{ counts[tab] }}
        </span>
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="text-center py-20 text-taupe text-sm">Loading…</div>

    <!-- Empty state -->
    <div v-else-if="filteredTodos.length === 0" class="text-center py-20">
      <p class="text-taupe text-sm">
        <template v-if="filter === 'completed'">No completed tasks yet.</template>
        <template v-else-if="filter === 'active'">No active tasks — you're all caught up!</template>
        <template v-else>No tasks yet. Create your first one above.</template>
      </p>
    </div>

    <!-- Todo list -->
    <ul v-else class="space-y-3">
      <li
        v-for="todo in filteredTodos"
        :key="todo.id"
        class="bg-card rounded-lg shadow-card overflow-hidden transition-shadow hover:shadow-card-hover"
      >
        <!-- View mode -->
        <div v-if="editingId !== todo.id" class="flex items-start gap-3 p-4">
          <!-- Checkbox -->
          <button
            @click="toggleComplete(todo)"
            :aria-label="todo.isComplete ? 'Mark incomplete' : 'Mark complete'"
            class="mt-0.5 flex-shrink-0 w-5 h-5 rounded border-2 flex items-center justify-center transition-colors"
            :class="todo.isComplete
              ? 'bg-charcoal border-charcoal'
              : 'border-taupe-light hover:border-taupe'"
          >
            <svg
              v-if="todo.isComplete"
              class="w-3 h-3 text-white"
              fill="none" viewBox="0 0 24 24"
              stroke="currentColor" stroke-width="3.5"
            >
              <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
            </svg>
          </button>

          <!-- Content -->
          <div class="flex-1 min-w-0">
            <p
              class="text-sm font-medium leading-snug"
              :class="todo.isComplete ? 'line-through text-taupe' : 'text-charcoal'"
            >
              {{ todo.title }}
            </p>
            <p v-if="todo.description" class="text-xs text-taupe mt-1 leading-relaxed">
              {{ todo.description }}
            </p>
            <p
              class="text-xs mt-1.5 font-medium"
              :class="isOverdue(todo.dueDate, todo.isComplete) ? 'text-error' : 'text-taupe'"
            >
              <span v-if="isOverdue(todo.dueDate, todo.isComplete)">Overdue · </span>
              {{ formatDate(todo.dueDate) }}
            </p>
          </div>

          <!-- Actions -->
          <div class="flex gap-1 flex-shrink-0 mt-0.5">
            <button
              @click="startEdit(todo)"
              class="text-xs text-taupe hover:text-charcoal px-2.5 py-1 rounded hover:bg-background transition-colors"
            >
              Edit
            </button>
            <button
              @click="deleteTodo(todo.id)"
              :disabled="deletingId === todo.id"
              class="text-xs text-taupe hover:text-error px-2.5 py-1 rounded hover:bg-error-light transition-colors disabled:opacity-40"
            >
              {{ deletingId === todo.id ? '…' : 'Delete' }}
            </button>
          </div>
        </div>

        <!-- Edit mode (inline) -->
        <form v-else @submit.prevent="submitEdit(todo)" class="p-4 space-y-3 bg-background/50">
          <input
            v-model="editForm.title"
            type="text"
            maxlength="100"
            class="w-full px-3 py-2 border border-taupe-light rounded text-charcoal text-sm focus:outline-none focus:border-charcoal transition-colors bg-card"
          />
          <textarea
            v-model="editForm.description"
            rows="2"
            class="w-full px-3 py-2 border border-taupe-light rounded text-charcoal text-sm placeholder-taupe focus:outline-none focus:border-charcoal transition-colors resize-none bg-card"
            placeholder="Description (optional)"
          />
          <input
            v-model="editForm.dueDate"
            type="date"
            class="px-3 py-2 border border-taupe-light rounded text-charcoal text-sm focus:outline-none focus:border-charcoal transition-colors bg-card"
          />
          <div v-if="editError" class="text-xs text-error bg-error-light px-3 py-2 rounded">
            {{ editError }}
          </div>
          <div class="flex gap-2">
            <button
              type="submit"
              :disabled="submittingEdit"
              class="px-3 py-1.5 bg-primary text-white text-xs font-medium rounded hover:bg-primary-hover disabled:opacity-50 transition-colors"
            >
              {{ submittingEdit ? 'Saving…' : 'Save' }}
            </button>
            <button
              type="button"
              @click="editingId = null"
              class="px-3 py-1.5 text-xs text-taupe hover:text-charcoal transition-colors"
            >
              Cancel
            </button>
          </div>
        </form>
      </li>
    </ul>

  </div>
</template>
