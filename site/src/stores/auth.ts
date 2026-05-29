import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import api from '../api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token'))
  const username = ref(localStorage.getItem('username'))

  const isAuthenticated = computed(() => !!token.value)

  async function login(usernameVal: string, password: string) {
    const { data } = await api.post('/api/auth/login', { username: usernameVal, password })
    token.value = data.token
    username.value = data.username
    localStorage.setItem('token', data.token)
    localStorage.setItem('username', data.username)
  }

  async function register(usernameVal: string, password: string) {
    const { data } = await api.post('/api/auth/register', { username: usernameVal, password })
    token.value = data.token
    username.value = data.username
    localStorage.setItem('token', data.token)
    localStorage.setItem('username', data.username)
  }

  function logout() {
    token.value = null
    username.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('username')
  }

  return { token, username, isAuthenticated, login, register, logout }
})
