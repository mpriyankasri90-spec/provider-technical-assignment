import { BrowserRouter, Link, Navigate, NavLink, Outlet, Route, Routes } from 'react-router'
import { DeletedProvidersPage } from './features/audit/DeletedProvidersPage'
import { ProviderDetailPage } from './features/providers/ProviderDetailPage'
import { ProviderFormPage } from './features/providers/ProviderFormPage'
import { ProviderListPage } from './features/providers/ProviderListPage'
import { LicenseReportsPage } from './features/reports/LicenseReportsPage'
import { EmptyState } from './components/States'

function Layout() {
  return (
    <>
      <header className="app-header">
        <div className="container app-header-inner">
          <Link to="/providers" className="app-title">
            Provider Licensing
          </Link>
          <nav aria-label="Main">
            <NavLink to="/providers">Providers</NavLink>
            <NavLink to="/reports">License reports</NavLink>
            <NavLink to="/audit/deleted-providers">Deleted providers</NavLink>
          </nav>
        </div>
      </header>
      <main className="container">
        <Outlet />
      </main>
    </>
  )
}

function NotFoundPage() {
  return (
    <EmptyState title="Page not found">
      <Link to="/providers">Go to providers</Link>
    </EmptyState>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<Navigate to="/providers" replace />} />
          <Route path="providers" element={<ProviderListPage />} />
          <Route path="providers/new" element={<ProviderFormPage />} />
          <Route path="providers/:id" element={<ProviderDetailPage />} />
          <Route path="providers/:id/edit" element={<ProviderFormPage />} />
          <Route path="reports" element={<LicenseReportsPage />} />
          <Route path="audit/deleted-providers" element={<DeletedProvidersPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
