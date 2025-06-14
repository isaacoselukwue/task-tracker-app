import { Link, useLocation, Navigate } from 'react-router-dom';
import { useSession } from '../../context/SessionContext';

export default function MainLayout({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, user, logout, bootstrapping } = useSession();
  const location = useLocation();

  if (bootstrapping) return null;

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  const getRoles = (roleClaim: string | string[] | undefined) => {
    if (!roleClaim) return [];
    return Array.isArray(roleClaim) ? roleClaim : [roleClaim];
  };

  let links: { to: string; label: string }[] = [];
  let showLogout = true;
  let isAdmin = false;
  if (user) {
    const roles = getRoles(user['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']);
    const isUser = roles.includes('User');
    isAdmin = roles.includes('Admin');

    const linkSet = new Map<string, { to: string; label: string }>();

    if (isUser && user['Permission'] === 'CanView') {
      [
        { to: '/dashboard', label: 'Dashboard' },
        { to: '/tasks', label: 'Tasks' },
        { to: '/settings', label: 'Settings' },
      ].forEach(link => linkSet.set(link.to, link));
    }
    if (isAdmin) {
      [
        { to: '/settings', label: 'Settings' },
        { to: '/admin/user-tasks', label: "All Users' Tasks" },
      ].forEach(link => linkSet.set(link.to, link));
    }

    links = Array.from(linkSet.values());

    if (!isUser && !isAdmin) {
      showLogout = false;
    }
  }

  return (
    <div className="flex min-h-screen bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
      <nav className="w-64 bg-purple-700 dark:bg-purple-900 text-white p-4 flex flex-col">
        <div className="mb-8">
          <h1 className="text-2xl font-bold">TaskTracker</h1>
          <div className="mt-2 text-sm">
            Welcome, {user?.['given_name'] || 'User'}
          </div>
        </div>
        <ul className="space-y-2 flex-1">
          {links.map(link => (
            <li key={link.to}>
              <Link to={link.to} className={`block py-2 px-4 rounded hover:bg-purple-600 dark:hover:bg-purple-800 ${
                  location.pathname === link.to ? 'bg-purple-800 dark:bg-purple-700' : ''
                }`} >
                {link.label}
              </Link>
            </li>
          ))}
        </ul>
        {showLogout && (
          <button onClick={logout} className="w-full text-left py-2 px-4 rounded hover:bg-purple-600 dark:hover:bg-purple-800 mt-8" >
            Logout
          </button>
        )}
      </nav>

      <main className="flex-1 p-8">
        {children}
      </main>
    </div>
  );
}