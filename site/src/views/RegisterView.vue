<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()

const username = ref('')
const password = ref('')
const confirm = ref('')
const error = ref('')
const loading = ref(false)

async function handleSubmit() {
  error.value = ''
  if (!username.value || !password.value || !confirm.value) {
    error.value = 'Please fill in all fields.'
    return
  }
  if (password.value.length < 6) {
    error.value = 'Password must be at least 6 characters.'
    return
  }
  if (password.value !== confirm.value) {
    error.value = 'Passwords do not match.'
    return
  }
  loading.value = true
  try {
    await auth.register(username.value, password.value)
    router.push('/todos')
  } catch (e: any) {
    const data = e.response?.data
    const errors = data?.errors as Record<string, string[]> | undefined
    // Surface field validation messages (e.g. "Password too short") but not
    // conflict reasons — confirming a username exists enables enumeration attacks.
    error.value =
      (errors && Object.values(errors).flat()[0]) ??
      'Registration failed. Please check your details and try again.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-[calc(100vh-3.5rem)] flex items-center justify-center px-4">
    <div class="w-full max-w-sm">
      <h1 class="font-serif text-3xl text-charcoal text-center mb-2">Create account</h1>
      <p class="text-taupe text-center text-sm mb-8">Start managing your tasks</p>

      <div class="bg-card rounded-lg shadow-card p-8">
        <form @submit.prevent="handleSubmit" class="space-y-5">
          <div>
            <label class="block text-sm font-medium text-charcoal mb-1.5">Username</label>
            <input
              v-model="username"
              type="text"
              autocomplete="username"
              class="w-full px-3 py-2.5 border border-taupe-light rounded text-charcoal placeholder-taupe focus:outline-none focus:border-charcoal transition-colors"
              placeholder="choose a username"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-charcoal mb-1.5">Password</label>
            <input
              v-model="password"
              type="password"
              autocomplete="new-password"
              class="w-full px-3 py-2.5 border border-taupe-light rounded text-charcoal placeholder-taupe focus:outline-none focus:border-charcoal transition-colors"
              placeholder="min. 6 characters"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-charcoal mb-1.5">Confirm password</label>
            <input
              v-model="confirm"
              type="password"
              autocomplete="new-password"
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
            {{ loading ? 'Creating account…' : 'Create account' }}
          </button>
        </form>
      </div>

      <p class="text-center text-sm text-taupe mt-6">
        Already have an account?
        <RouterLink to="/login" class="text-charcoal underline underline-offset-2">Sign in</RouterLink>
      </p>
    </div>
  </div>
</template>
