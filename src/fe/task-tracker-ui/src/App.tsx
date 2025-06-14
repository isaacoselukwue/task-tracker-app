import { Routes, Route } from 'react-router-dom';
import HomePage from './pages/HomePage';
import Login from './pages/LoginPage';
import Signup from './pages/SignupPage';
import SignupVerificationPage from './pages/SignupVerificationPage';
import DashboardPage from './pages/AuthPages/DashboardPage';
import TasksPage from './pages/AuthPages/TasksPage';
import SettingsPage from './pages/AuthPages/SettingsPage';
import AdminUserTasksPage from './pages/AuthPages/AdminUserTasksPage';
import NotFoundPage from './pages/NotFoundPage';
import ForgotPasswordPage from './pages/ForgotPasswordPage';
import PasswordResetPage from './pages/PasswordResetPage';

function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/login" element={<Login />} />
      <Route path="/signup" element={<Signup />} />
      <Route path="/dashboard" element={<DashboardPage />} />
      <Route path="/tasks" element={<TasksPage />} />
      <Route path="/settings" element={<SettingsPage />} />
      <Route path="/signup/verify/:tokenAndUserId" element={<SignupVerificationPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/passwords/reset/:tokenAndUserId" element={<PasswordResetPage />} />
      <Route path="/admin/user-tasks" element={<AdminUserTasksPage />} />
      <Route path="/admin/user-tasks/:userId" element={<AdminUserTasksPage />} />
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

export default App;