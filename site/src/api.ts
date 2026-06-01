import axios from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5010',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Redirect to login on 401, but not for auth endpoints — a 401 from
// /api/auth/login is a credential failure, not an expired session, and
// the component's own catch block handles displaying the error.
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const url: string = error.config?.url ?? ''
    if (error.response?.status === 401 && !url.includes('/api/auth/')) {
      localStorage.removeItem('token')
      localStorage.removeItem('username')
      window.location.replace('/login')
    }
    return Promise.reject(error)
  }
)

export default api
