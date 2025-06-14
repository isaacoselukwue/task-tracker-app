import { Link, useLocation, Navigate } from 'react-router-dom';
import { useSession } from '../../context/SessionContext';
import { useState, useEffect } from 'react'; // Import useState and useEffect

export default function MainLayout({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, user, logout, bootstrapping } = useSession();
  const location = useLocation();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);

  if (bootstrapping) return null;

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  const toggleSidebar = () => setIsSidebarOpen(!isSidebarOpen);

  useEffect(() => {
    if (isSidebarOpen) {
      setIsSidebarOpen(false);
    }
  }, [location.pathname]);


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
    
    if (isAdmin && !linkSet.has('/dashboard')) {
        linkSet.set('/dashboard', { to: '/dashboard', label: 'Dashboard' });
    }


    links = Array.from(linkSet.values()).sort((a, b) => {
        const order = ['/dashboard', '/tasks', "/admin/user-tasks", '/settings'];
        return order.indexOf(a.to) - order.indexOf(b.to);
    });


    if (!isUser && !isAdmin) {
      showLogout = false;
    }
  }

  return (
    <div className="flex min-h-screen bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
      <nav
        className={`
          fixed inset-y-0 left-0 z-30 w-64 bg-purple-700 dark:bg-purple-900 text-white p-4
          flex flex-col transition-transform duration-300 ease-in-out
          ${isSidebarOpen ? 'translate-x-0' : '-translate-x-full'}
          md:relative md:translate-x-0 md:flex md:w-64 
        `}
      >
        <div className="mb-8">
          <h1 className="text-2xl font-bold">TaskTracker</h1>
          <div className="mt-2 text-sm">
            Welcome, {user?.['given_name'] || 'User'}
          </div>
        </div>
        <ul className="space-y-2 flex-1">
          {links.map(link => (
            <li key={link.to}>
              <Link
                to={link.to}
                className={`block py-2 px-4 rounded hover:bg-purple-600 dark:hover:bg-purple-800 ${
                  location.pathname === link.to ? 'bg-purple-800 dark:bg-purple-700' : ''
                }`}
              >
                {link.label}
              </Link>
            </li>
          ))}
        </ul>
        {showLogout && (
          <button
            onClick={logout}
            className="w-full text-left py-2 px-4 rounded hover:bg-purple-600 dark:hover:bg-purple-800 mt-8"
          >
            Logout
          </button>
        )}
      </nav>

      {isSidebarOpen && (
        <div
          className="fixed inset-0 z-20 bg-black bg-opacity-50 md:hidden"
          onClick={toggleSidebar}
        ></div>
      )}

      <main className="flex-1 p-4 md:p-8 flex flex-col w-full overflow-x-hidden">
        <div className="md:hidden mb-4">
          <button
            onClick={toggleSidebar}
            className="p-2 rounded-md text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-inset focus:ring-purple-500"
            aria-expanded={isSidebarOpen}
            aria-controls="sidebar"
            aria-label="Toggle navigation"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 12h16M4 18h16"></path>
            </svg>
          </button>
        </div>
        {children}
      </main>
    </div>
  );
}