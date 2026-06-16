import axios, { AxiosHeaders } from 'axios'

export const baseURL = import.meta.env.VITE_API_URL || 'http://localhost:5181/api'

export const api = axios.create({
  baseURL,
  timeout: 6000,
})

const roleMap: Record<string, string> = {
  admin: 'Administrator',
  supervisor: 'Supervisor',
  operator: 'Operator',
  quality: 'QualityTechnician',
  logistics: 'Logistics',
  client: 'Customer',
  demoViewer: 'DemoViewer',
}

api.interceptors.request.use((config) => {
  try {
    const rawUser = localStorage.getItem('user')
    if (rawUser) {
      const profile = JSON.parse(rawUser) as { username?: string; roleKey?: string }
      const role = roleMap[profile.roleKey || ''] || 'DemoViewer'
      const headers = AxiosHeaders.from(config.headers)
      headers.set('X-DriveTrace-Role', role)
      if (profile.username) headers.set('X-DriveTrace-User', profile.username)
      config.headers = headers
    }
  } catch {
    // Local demo authentication must never block API access.
  }
  return config
})

export async function apiGet<T>(path: string, fallback: T): Promise<T> {
  try {
    const response = await api.get<T>(path)
    return response.data
  } catch (error) {
    console.warn(`DriveTrace Core API fallback used for ${path}`, error)
    return fallback
  }
}

export async function apiPost<TRequest, TResponse>(path: string, payload: TRequest): Promise<TResponse> {
  const response = await api.post<TResponse>(path, payload)
  return response.data
}

export async function apiPut<TRequest>(path: string, payload: TRequest): Promise<void> {
  await api.put(path, payload)
}

export async function apiDelete(path: string): Promise<void> {
  await api.delete(path)
}

export async function createEntity<TEntity extends { id?: number }>(path: string, payload: Partial<TEntity>): Promise<TEntity> {
  return apiPost<Partial<TEntity>, TEntity>(path, payload)
}

export async function updateEntity<TEntity extends { id?: number }>(path: string, id: number, payload: Partial<TEntity>): Promise<void> {
  await apiPut<Partial<TEntity>>(`${path}/${id}`, { ...payload, id })
}

export async function deleteEntity(path: string, id: number): Promise<void> {
  await apiDelete(`${path}/${id}`)
}

export function getApiErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const responseData = error.response?.data
    if (typeof responseData === 'string' && responseData.trim()) return responseData
    if (responseData && typeof responseData === 'object') {
      const detail = 'detail' in responseData ? responseData.detail : undefined
      const title = 'title' in responseData ? responseData.title : undefined
      if (typeof detail === 'string' && detail.trim()) return detail
      if (typeof title === 'string' && title.trim()) return title
    }
    if (error.response?.status) return `HTTP ${error.response.status}`
    if (error.message) return error.message
  }
  return 'Unexpected error'
}
