export interface Admin { id: string; email: string }
export interface Tenant {
  id: string
  name: string
  contactEmail: string
  phone: string | null
  createdByAdminId: string
  createdAt: string
}
export interface TenantPage { items: Tenant[]; totalCount: number; page: number; pageSize: number }
interface Problem { title?: string; errors?: Record<string, string[]>; traceId?: string }

export class ApiError extends Error {
  constructor(public status: number, message: string, public fields: Record<string, string[]> = {}) {
    super(message)
  }
}

export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body) headers.set('Content-Type', 'application/json')
  if (init.method && !['GET', 'HEAD'].includes(init.method)) {
    const { token } = await api<{ token: string }>('/auth/csrf')
    headers.set('X-CSRF-TOKEN', token)
  }
  let response: Response
  try {
    response = await fetch(`/api${path}`, { ...init, headers, credentials: 'same-origin' })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new ApiError(0, 'Сервертэй холбогдож чадсангүй. Холболтоо шалгаад дахин оролдоно уу.')
  }
  if (!response.ok) {
    const problem: Problem = await response.json().catch(() => ({}))
    if (response.status === 401 && path.startsWith('/platform/'))
      window.dispatchEvent(new Event('session-expired'))
    const message = response.status === 429 ? 'Хэт олон оролдлого хийлээ. Түр хүлээгээд дахин оролдоно уу.'
      : response.status === 403 ? 'Энэ үйлдлийг хийх эрхгүй байна.'
      : response.status >= 500 ? 'Серверийн алдаа гарлаа. Дараа дахин оролдоно уу.'
      : response.status === 400 ? 'Оруулсан мэдээллээ шалгана уу.'
      : response.status === 404 ? 'Tenant олдсонгүй.'
      : response.status === 401 ? 'Имэйл эсвэл нууц үг буруу байна.'
      : 'Хүсэлтийг гүйцэтгэж чадсангүй.'
    throw new ApiError(response.status, message, problem.errors)
  }
  return response.json() as Promise<T>
}

export function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Алдаа гарлаа. Дахин оролдоно уу.'
}
