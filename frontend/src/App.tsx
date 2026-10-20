import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, Navigate, Route, Routes, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { api, ApiError, errorMessage } from './api'
import type { Admin, Tenant, TenantPage } from './api'

const date = (value: string) => new Intl.DateTimeFormat('mn-MN', {
  year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit',
}).format(new Date(value))

function Brand() {
  return <div className="brand"><span className="brand-mark" aria-hidden="true">a<span>·</span></span>
    <span>AI Support Agent<small>Платформын удирдлага</small></span></div>
}

function Notice({ children }: { children: React.ReactNode }) {
  return <div className="notice error" role="alert">{children}</div>
}

export default function App() {
  const [admin, setAdmin] = useState<Admin | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError('')
    api<Admin>('/auth/session', { signal: controller.signal }).then(setAdmin).catch((err: unknown) => {
      if (controller.signal.aborted) return
      if (err instanceof ApiError && err.status === 401) setAdmin(null)
      else setError(errorMessage(err))
    }).finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [attempt])
  useEffect(() => {
    const expired = () => setAdmin(null)
    window.addEventListener('session-expired', expired)
    return () => window.removeEventListener('session-expired', expired)
  }, [])

  if (loading) return <div className="boot" role="status"><Brand /><p>Нэвтрэлтийг шалгаж байна 11</p></div>
  if (error) return <div className="boot"><Brand /><Notice>{error}</Notice>
    <button onClick={() => setAttempt(attempt + 1)}>Дахин оролдох</button></div>
  if (!admin) return <Routes><Route path="/login" element={<Login onLogin={setAdmin} />} />
    <Route path="*" element={<Navigate to="/login" replace />} /></Routes>
  return <div className="app-shell">
    <a className="skip-link" href="#main">Агуулга руу очих</a>
    <aside className="sidebar"><Brand /><p className="nav-label">УДИРДЛАГА</p>
      <Link className="nav-item" to="/tenants" aria-current="page"><span aria-hidden="true">▦</span> Tenant бүртгэл 33</Link>
      <div className="sidebar-footer"><span className="avatar">A</span><div>Платформын админ<small>{admin.email}</small></div></div>
    </aside>
    <div className="workspace"><header className="topbar"><span>Удирдлагын хэсэг</span><span className="role-label">Платформын админ</span></header>
      <main id="main"><Routes>
        <Route path="/tenants" element={<Tenants />} />
        <Route path="/tenants/:id" element={<TenantDetail />} />
        <Route path="*" element={<Navigate to="/tenants" replace />} />
      </Routes></main>
      <footer className="page-footer">AI Support Agent Platform</footer>
    </div>
  </div>
}

function Login({ onLogin }: { onLogin: (admin: Admin) => void }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const navigate = useNavigate()
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy) return
    const data = new FormData(event.currentTarget)
    setBusy(true); setError('')
    try {
      const admin = await api<Admin>('/auth/login', { method: 'POST', body: JSON.stringify({
        email: String(data.get('email')).trim(), password: data.get('password'),
      }) })
      onLogin(admin)
      navigate('/tenants', { replace: true })
    } catch (err) { setError(errorMessage(err)) } finally { setBusy(false) }
  }
  return <div className="login-page"><section className="login-intro"><Brand />
    <div className="login-copy"><span className="eyebrow">AI SUPPORT AGENT PLATFORM</span>
      <h1>Харилцагчийн<br />удирдлага.</h1><p>Программ хөгжүүлэгч компаниудын<br />нэгдсэн бүртгэл.</p></div>
    <span className="login-foot">Платформын админд зориулсан хэсэг</span>
  </section><main className="login-main"><div className="login-form-wrap">
    <span className="eyebrow muted">ТАВТАЙ МОРИЛ</span><h2>Админ нэвтрэх</h2>
    <p className="description">Удирдлагын хэсэгт нэвтрэх мэдээллээ оруулна уу.</p>
    <form onSubmit={submit} aria-busy={busy}>
      <label htmlFor="email">Имэйл</label><input id="email" name="email" type="email" autoComplete="username" required maxLength={254} placeholder="admin@company.mn" disabled={busy} />
      <label htmlFor="password">Нууц үг</label><input id="password" name="password" type="password" autoComplete="current-password" required maxLength={1024} disabled={busy} />
      {error && <Notice>{error}</Notice>}
      <button className="login-button" disabled={busy}>{busy ? 'Нэвтэрч байна…' : 'Нэвтрэх'}<span aria-hidden="true">→</span></button>
    </form>
  </div></main></div>
}

function Tenants() {
  const [params, setParams] = useSearchParams()
  const parsed = Number(params.get('page') ?? 1)
  const page = Number.isSafeInteger(parsed) && parsed > 0 && parsed <= 85899345 ? parsed : 1
  const [data, setData] = useState<TenantPage | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [creating, setCreating] = useState(false)
  const [success, setSuccess] = useState('')
  const [revision, setRevision] = useState(0)
  useEffect(() => {
    const controller = new AbortController()
    setLoading(true); setError('')
    api<TenantPage>(`/platform/tenants?page=${page}&pageSize=25`, { signal: controller.signal })
      .then(setData).catch((err: unknown) => { if (!controller.signal.aborted) setError(errorMessage(err)) })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [page, revision])
  return <>
    <div className="breadcrumb">Удирдлага <span>/</span> Tenant бүртгэл</div>
    <div className="page-heading"><div><span className="eyebrow muted">ХАРИЛЦАГЧИД</span><h1>Tenant бүртгэл</h1>
      <p className="description">Платформд бүртгэлтэй программ хөгжүүлэгч компаниуд.</p></div>
      <button onClick={() => { setCreating(true); setSuccess('') }} disabled={creating}><span aria-hidden="true">＋</span> Tenant бүртгэх</button></div>
    {success && <div className="notice success" role="status">{success}</div>}
    {creating && <TenantForm onCancel={() => setCreating(false)} onCreated={(tenant) => {
      setCreating(false); setSuccess(`${tenant.name} амжилттай бүртгэгдлээ.`)
      setParams({}); setRevision(value => value + 1)
    }} />}
    <section className="tenant-section" aria-labelledby="tenant-list-heading" aria-busy={loading}>
      <div className="section-heading"><h2 id="tenant-list-heading">Бүртгэлтэй компаниуд</h2>
        {!loading && !error && data && <span className="count">{data.totalCount}</span>}</div>
      {loading ? <div className="empty-state" role="status">Бүртгэлийг ачаалж байна…</div>
        : error ? <div className="section-message"><Notice>{error}</Notice><button className="secondary" onClick={() => setRevision(value => value + 1)}>Дахин оролдох</button></div>
        : data && data.items.length > 0 ? <>
          <div className="table-scroll"><table><thead><tr><th>Компанийн нэр</th><th>Холбоо барих имэйл</th><th>Бүртгэсэн огноо</th><th><span className="sr-only">Дэлгэрэнгүй</span></th></tr></thead>
            <tbody>{data.items.map(tenant => <tr key={tenant.id}><td><Link className="tenant-link" to={`/tenants/${tenant.id}`}>{tenant.name}</Link></td>
              <td>{tenant.contactEmail}</td><td className="date-cell">{date(tenant.createdAt)}</td>
              <td><Link className="row-arrow" to={`/tenants/${tenant.id}`} aria-label={`${tenant.name} дэлгэрэнгүй`}>↗</Link></td></tr>)}</tbody></table></div>
        </> : <div className="empty-state"><span className="empty-symbol" aria-hidden="true">▦</span><h3>{data && data.totalCount > 0 ? 'Энэ хуудсанд бүртгэл алга' : 'Tenant бүртгэл алга'}</h3>
          <p>«Tenant бүртгэх» товчоор компани бүртгэнэ үү.</p></div>}
      {!loading && !error && data && data.totalCount > 0 && <div className="pagination"><span>{page}-р хуудас / {Math.ceil(data.totalCount / data.pageSize)}</span>
        <div><button className="secondary" disabled={page <= 1} onClick={() => setParams({ page: String(page - 1) })}>← Өмнөх</button>
          <button className="secondary" disabled={page * data.pageSize >= data.totalCount} onClick={() => setParams({ page: String(page + 1) })}>Дараах →</button></div></div>}
    </section>
  </>
}

function TenantForm({ onCancel, onCreated }: { onCancel: () => void; onCreated: (tenant: Tenant) => void }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [fields, setFields] = useState<Record<string, string[]>>({})
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy) return
    const form = event.currentTarget
    const values = new FormData(form)
    const name = String(values.get('name')).trim()
    if (!name) { setFields({ Name: ['Компанийн нэрээ оруулна уу.'] }); return }
    setBusy(true); setError(''); setFields({})
    try {
      const tenant = await api<Tenant>('/platform/tenants', { method: 'POST', body: JSON.stringify({
        name, contactEmail: String(values.get('contactEmail')).trim(), phone: String(values.get('phone')).trim() || null,
      }) })
      onCreated(tenant)
    } catch (err) {
      setError(errorMessage(err))
      if (err instanceof ApiError) setFields(err.fields)
    } finally { setBusy(false) }
  }

  return <section className="create-section" aria-labelledby="create-title"><div className="section-heading"><h2 id="create-title">Tenant шинээр бүртгэх</h2>
    <span className="subtle">* Заавал бөглөх</span></div>
    <form onSubmit={submit} aria-busy={busy}><div className="form-grid">
      <div><label htmlFor="tenant-name">Компанийн нэр *</label><input id="tenant-name" name="name" required maxLength={200} autoFocus disabled={busy}
        aria-invalid={!!fields.Name} aria-describedby={fields.Name ? 'name-error' : undefined} placeholder="Компанийн нэр" />
        {fields.Name && <span className="field-error" id="name-error">{fields.Name[0]}</span>}</div>
      <div><label htmlFor="tenant-email">Холбоо барих имэйл *</label><input id="tenant-email" name="contactEmail" type="email" required maxLength={254} disabled={busy}
        aria-invalid={!!fields.ContactEmail} aria-describedby={fields.ContactEmail ? 'email-error' : undefined} placeholder="contact@company.mn" />
        {fields.ContactEmail && <span className="field-error" id="email-error">{fields.ContactEmail[0]}</span>}</div>
      <div><label htmlFor="tenant-phone">Утас <span className="optional">сонголттой</span></label><input id="tenant-phone" name="phone" type="tel" maxLength={32} disabled={busy}
        aria-invalid={!!fields.Phone} aria-describedby={fields.Phone ? 'phone-error' : undefined} placeholder="Утасны дугаар" />
        {fields.Phone && <span className="field-error" id="phone-error">{fields.Phone[0]}</span>}</div>
    </div>{error && <Notice>{error}</Notice>}
      <div className="form-actions"><button type="button" className="secondary" disabled={busy} onClick={onCancel}>Болих</button>
        <button disabled={busy}>{busy ? 'Бүртгэж байна…' : 'Бүртгэх'}</button></div>
    </form></section>
}

function TenantDetail() {
  const { id } = useParams()
  const [tenant, setTenant] = useState<Tenant | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const [revision, setRevision] = useState(0)
  useEffect(() => {
    const controller = new AbortController()
    setLoading(true); setError(''); setTenant(null)
    api<Tenant>(`/platform/tenants/${id}`, { signal: controller.signal }).then(setTenant)
      .catch((err: unknown) => { if (!controller.signal.aborted) setError(errorMessage(err)) })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [id, revision])
  return <>
    <div className="breadcrumb"><Link to="/tenants">Tenant бүртгэл</Link><span>/</span>Дэлгэрэнгүй</div>
    <Link className="back-link" to="/tenants">← Жагсаалт руу буцах</Link>
    {loading ? <div className="empty-state" role="status">Мэдээлэл ачаалж байна…</div>
      : error ? <><Notice>{error}</Notice><button className="secondary" onClick={() => setRevision(value => value + 1)}>Дахин оролдох</button></>
      : tenant && <><div className="page-heading"><div><span className="eyebrow muted">TENANT</span><h1>{tenant.name}</h1>
        <p className="description">Компанийн бүртгэлийн мэдээлэл.</p></div></div>
        <section className="detail-section"><h2>Үндсэн мэдээлэл</h2><dl className="detail-grid">
          <div><dt>Компанийн нэр</dt><dd>{tenant.name}</dd></div>
          <div><dt>Холбоо барих имэйл</dt><dd>{tenant.contactEmail}</dd></div>
          <div><dt>Утас</dt><dd>{tenant.phone || 'Бүртгээгүй'}</dd></div>
          <div><dt>Бүртгэсэн огноо</dt><dd>{date(tenant.createdAt)}</dd></div>
          <div className="full-width"><dt>Tenant ID</dt><dd className="mono">{tenant.id}</dd></div>
        </dl></section></>}
  </>
}
