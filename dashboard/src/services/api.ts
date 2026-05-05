import axios from 'axios'

const baseURL = import.meta.env.VITE_API_URL || 'http://localhost:5181/api'

export const api = axios.create({
  baseURL,
  timeout: 6000,
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
