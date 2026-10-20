import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { api, ApiError } from './api'

const fetchMock = vi.fn<typeof fetch>()

beforeEach(() => {
  fetchMock.mockReset()
  vi.stubGlobal('fetch', fetchMock)
  vi.stubGlobal('window', new EventTarget())
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('api', () => {
  it('sends GET requests to /api with cookies and returns JSON', async () => {
    const page = { items: [], totalCount: 0, page: 1, pageSize: 25 }
    fetchMock.mockResolvedValueOnce(Response.json(page))

    await expect(api('/platform/tenants')).resolves.toEqual(page)

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/platform/tenants')
    const options = fetchMock.mock.calls[0]?.[1]
    expect(options?.credentials).toBe('same-origin')
    expect(new Headers(options?.headers).get('Accept')).toBe('application/json')
  })

  it('fetches a CSRF token before sending a JSON POST', async () => {
    const tenant = { id: 'tenant-1', name: 'Example' }
    const body = JSON.stringify({ name: 'Example', contactEmail: 'hello@example.mn' })
    fetchMock.mockResolvedValueOnce(Response.json({ token: 'test-csrf-token' }))
    fetchMock.mockResolvedValueOnce(Response.json(tenant, { status: 201 }))

    await expect(api('/platform/tenants', { method: 'POST', body })).resolves.toEqual(tenant)

    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/auth/csrf')
    expect(fetchMock.mock.calls[0]?.[1]?.credentials).toBe('same-origin')
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/platform/tenants')
    const options = fetchMock.mock.calls[1]?.[1]
    expect(options).toMatchObject({ method: 'POST', body, credentials: 'same-origin' })
    const headers = new Headers(options?.headers)
    expect(headers.get('X-CSRF-TOKEN')).toBe('test-csrf-token')
    expect(headers.get('Content-Type')).toBe('application/json')
  })

  it('does not send the POST when fetching the CSRF token fails', async () => {
    fetchMock.mockResolvedValueOnce(Response.json({}, { status: 500 }))

    await expect(api('/platform/tenants', { method: 'POST', body: '{}' }))
      .rejects.toMatchObject({ status: 500 })
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('preserves validation errors in ApiError', async () => {
    const fields = { Name: ['Компанийн нэрээ оруулна уу.'] }
    fetchMock.mockResolvedValueOnce(Response.json({ errors: fields }, { status: 400 }))

    const error = await api('/platform/tenants').catch((error: unknown) => error)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 400, fields, message: 'Оруулсан мэдээллээ шалгана уу.' })
  })

  it.each([
    [403, 'Энэ үйлдлийг хийх эрхгүй байна.'],
    [404, 'Tenant олдсонгүй.'],
    [429, 'Хэт олон оролдлого хийлээ. Түр хүлээгээд дахин оролдоно уу.'],
    [500, 'Серверийн алдаа гарлаа. Дараа дахин оролдоно уу.'],
  ])('maps HTTP %i to its user-facing error', async (status, message) => {
    fetchMock.mockResolvedValueOnce(Response.json({}, { status }))

    await expect(api('/platform/tenants')).rejects.toMatchObject({ status, message })
  })

  it('emits session-expired when a platform request returns 401', async () => {
    const expired = vi.fn()
    window.addEventListener('session-expired', expired)
    fetchMock.mockResolvedValueOnce(Response.json({}, { status: 401 }))

    await expect(api('/platform/tenants')).rejects.toMatchObject({ status: 401 })
    expect(expired).toHaveBeenCalledTimes(1)
  })

  it('does not emit session-expired for an unauthenticated session check', async () => {
    const expired = vi.fn()
    window.addEventListener('session-expired', expired)
    fetchMock.mockResolvedValueOnce(Response.json({}, { status: 401 }))

    await expect(api('/auth/session')).rejects.toMatchObject({ status: 401 })
    expect(expired).not.toHaveBeenCalled()
  })

  it('handles an HTTP error whose body is not JSON', async () => {
    fetchMock.mockResolvedValueOnce(new Response('Bad gateway', { status: 502 }))

    await expect(api('/platform/tenants')).rejects.toMatchObject({ status: 502, fields: {} })
  })

  it('converts a network failure to ApiError with status 0', async () => {
    fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'))

    await expect(api('/platform/tenants')).rejects.toMatchObject({
      status: 0,
      message: 'Сервертэй холбогдож чадсангүй. Холболтоо шалгаад дахин оролдоно уу.',
    })
  })

  it('forwards the abort signal and preserves AbortError', async () => {
    const controller = new AbortController()
    const error = new DOMException('The operation was aborted', 'AbortError')
    controller.abort()
    fetchMock.mockRejectedValueOnce(error)

    await expect(api('/platform/tenants', { signal: controller.signal })).rejects.toBe(error)
    expect(fetchMock.mock.calls[0]?.[1]?.signal).toBe(controller.signal)
  })
})
