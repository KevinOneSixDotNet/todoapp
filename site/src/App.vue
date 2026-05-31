<script setup lang="ts">
import { RouterView, RouterLink, useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'

const auth = useAuthStore()
const router = useRouter()

function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <div class="min-h-screen bg-background">
    <header class="bg-card border-b border-taupe-light">
      <div class="max-w-2xl mx-auto px-4 h-14 flex items-center justify-between">
        <span class="font-serif text-lg font-medium text-charcoal tracking-wide">
          Function Health
        </span>
        <nav class="flex items-center gap-5 text-sm">
          <template v-if="auth.isAuthenticated">
            <span class="text-taupe">{{ auth.username }}</span>
            <button
              @click="logout"
              class="text-charcoal hover:text-primary transition-colors"
            >
              Sign out
            </button>
          </template>
          <template v-else>
            <RouterLink to="/login" class="text-taupe hover:text-charcoal transition-colors">
              Sign in
            </RouterLink>
            <RouterLink
              to="/register"
              class="px-3 py-1.5 bg-primary text-white rounded hover:bg-primary-hover transition-colors"
            >
              Register
            </RouterLink>
          </template>
        </nav>
      </div>
    </header>

    <main>
      <RouterView />
    </main>
  </div>
</template>
