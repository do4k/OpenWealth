import { BrowserRouter, Navigate, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { AuthProvider, useAuth } from './auth'
import LoginPage from './pages/LoginPage'
import DashboardPage from './pages/DashboardPage'
import IncomePage from './pages/IncomePage'
import StudentLoansPage from './pages/StudentLoansPage'
import MortgagesPage from './pages/MortgagesPage'
import SavingsPage from './pages/SavingsPage'
import InvestmentsPage from './pages/InvestmentsPage'
import SharingPage from './pages/SharingPage'
import TaxSettingsPage from './pages/TaxSettingsPage'
import PublicProfilePage from './pages/PublicProfilePage'

function RequireAuth({ children }: { children: React.ReactElement }) {
  const { isAuthenticated } = useAuth()
  const location = useLocation()
  if (!isAuthenticated) return <Navigate to="/login" state={{ from: location }} replace />
  return children
}

function Shell({ children }: { children: React.ReactNode }) {
  const { displayName, logout } = useAuth()
  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">£</span> OpenWealth
        </div>
        <nav>
          <NavLink to="/" end>Dashboard</NavLink>
          <NavLink to="/income">Income</NavLink>
          <NavLink to="/student-loans">Student loans</NavLink>
          <NavLink to="/mortgages">Mortgages &amp; property</NavLink>
          <NavLink to="/savings">Savings</NavLink>
          <NavLink to="/investments">Investments</NavLink>
          <div className="nav-section">Account</div>
          <NavLink to="/sharing">Sharing</NavLink>
          <NavLink to="/tax-settings">Tax settings</NavLink>
        </nav>
        <div className="sidebar-footer">
          <span className="user-name">{displayName}</span>
          <button className="link-button" onClick={logout}>Log out</button>
        </div>
      </aside>
      <main className="content">{children}</main>
    </div>
  )
}

function Protected({ page }: { page: React.ReactElement }) {
  return (
    <RequireAuth>
      <Shell>{page}</Shell>
    </RequireAuth>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/p/:slug" element={<PublicProfilePage />} />
          <Route path="/" element={<Protected page={<DashboardPage />} />} />
          <Route path="/income" element={<Protected page={<IncomePage />} />} />
          <Route path="/student-loans" element={<Protected page={<StudentLoansPage />} />} />
          <Route path="/mortgages" element={<Protected page={<MortgagesPage />} />} />
          <Route path="/savings" element={<Protected page={<SavingsPage />} />} />
          <Route path="/investments" element={<Protected page={<InvestmentsPage />} />} />
          <Route path="/sharing" element={<Protected page={<SharingPage />} />} />
          <Route path="/tax-settings" element={<Protected page={<TaxSettingsPage />} />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
