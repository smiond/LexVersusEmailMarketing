import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { Layout } from './components/Layout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { AuthProvider } from './context/AuthContext';
import { CampaignsPage } from './pages/Campaigns';
import { ContactsPage } from './pages/Contacts';
import { DashboardPage } from './pages/Dashboard';
import { LoginPage } from './pages/Login';
import { RegisterPage } from './pages/Register';
import { SendersPage } from './pages/Senders';
import { TemplatesPage } from './pages/Templates';
import { UnsubscribePage } from './pages/Unsubscribe';

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/unsubscribe/:token" element={<UnsubscribePage />} />

          <Route element={<ProtectedRoute />}>
            <Route element={<Layout />}>
              <Route index element={<DashboardPage />} />
              <Route path="contacts" element={<ContactsPage />} />
              <Route path="templates" element={<TemplatesPage />} />
              <Route path="senders" element={<SendersPage />} />
              <Route path="campaigns" element={<CampaignsPage />} />
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
