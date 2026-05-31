<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()

const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

async function handleSubmit() {
  error.value = ''
  if (!username.value || !password.value) {
    error.value = 'Please fill in all fields.'
    return
  }
  loading.value = true
  try {
    await auth.login(username.value, password.value)
    router.push('/todos')
  } catch (e: any) {
    error.value = e.response?.data?.message ?? 'Invalid username or password.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-[calc(100vh-3.5rem)] flex items-center justify-center px-4">
    <div class="w-full max-w-sm">
      <h1 class="font-serif text-3xl text-charcoal text-center mb-2">Welcome back</h1>
      <p class="text-taupe text-center text-sm mb-8">Sign in to your account</p>

      <div class="bg-card rounded-lg shadow-card p-8">
        <form @submit.prevent="handleSubmit" class="space-y-5">
          <div>
            <label class="block text-sm font-medium text-charcoal mb-1.5">Username</label>
            <input
              v-model="username"
              type="text"
              autocomplete="username"
              class="w-full px-3 py-2.5 border border-taupe-light rounded text-charcoal placeholder-taupe focus:outline-none focus:border-charcoal transition-colors"
              placeholder="your username"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-charcoal mb-1.5">Password</label>
            <input
              v-model="password"
              type="password"
              autocomplete="current-password"
              class="w-full px-3 py-2.5 border border-taupe-light rounded text-charcoal placeholder-taupe focus:outline-none focus:border-charcoal transition-colors"
              placeholder="••••••••"
            />
          </div>

          <div v-if="error" class="text-sm text-error bg-error-light px-3 py-2 rounded">
            {{ error }}
          </div>

          <button
            type="submit"
            :disabled="loading"
            class="w-full py-2.5 bg-primary text-white rounded font-medium hover:bg-primary-hover disabled:opacity-50 transition-colors"
          >
            {{ loading ? 'Signing in…' : 'Sign in' }}
          </button>
        </form>
      </div>

      <p class="text-center text-sm text-taupe mt-6">
        Don't have an account?
        <RouterLink to="/register" class="text-charcoal underline underline-offset-2">Register</RouterLink>
      </p>
    </div>
  </div>
</template>
