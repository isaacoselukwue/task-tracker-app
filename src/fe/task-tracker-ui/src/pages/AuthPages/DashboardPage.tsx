import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTheme } from '../../context/ThemeContext';
import MainLayout from '../components/MainLayout';
import { useSession } from '../../context/SessionContext';

interface Task {
  id: string;
  title: string;
  description: string | null;
  scheduledFor: string;
  status: string;
}

interface TasksResponse {
  page: number;
  size: number;
  totalPages: number;
  totalResults: number;
  results: Task[];
}

interface ApiTasksResponse {
  data: TasksResponse;
  succeeded: boolean;
  message?: string;
  errors?: string[];
}

interface UserStatistics {
  usersCount: number;
  activeUsersCount: number;
  deactivatedUsersCount: number;
  deletedUsersCount: number;
  pendingUsersCount: number;
}

export default function DashboardPage() {
  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [userStats, setUserStats] = useState<UserStatistics | null>(null);
  const [statsLoading, setStatsLoading] = useState(false);
  const [statsError, setStatsError] = useState<string | null>(null);
  const [statsRefreshing, setStatsRefreshing] = useState(false);

  const { darkMode, toggleDarkMode } = useTheme();
  const { isAuthenticated, user } = useSession();
  const navigate = useNavigate();

  const [toast, setToast] = useState<string | null>(null);

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }

    if (user) {
      const roles = Array.isArray(user['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'])
        ? user['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        : [user['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']];
      if (roles.includes('User') && user['Permission'] === 'CanView') {
        fetchTasks();
      }
      if (roles.includes('Admin')) {
        fetchUserStats(false);
      }
    }
  }, [isAuthenticated, user]);

  const fetchTasks = async () => {
    setLoading(true);
    setError(null);

    try {
      const apiKey = import.meta.env.VITE_BASE_API_KEY;
      const baseUrl = import.meta.env.VITE_API_BASE_URL;

      const response = await fetch(`${baseUrl}/Tasks/upcoming?PageCount=3&PageNumber=1`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
          'X-Api-Key': apiKey || '',
          'Content-Type': 'application/json'
        }
      });
      const apiResponse: ApiTasksResponse = await response.json();
      if (!response.ok || !apiResponse.succeeded) {
        throw new Error(apiResponse.message || 'Failed to fetch tasks');
      }

      setTasks(apiResponse.data?.results || []); 
    } catch (err: any) {
      setError(err.message || 'Error fetching tasks');
      setTasks([]);
    } finally {
      setLoading(false);
    }
  };

  const fetchUserStats = async (refresh: boolean) => {
    if (refresh) setStatsRefreshing(true);
    else setStatsLoading(true);
    setStatsError(null);
    try {
      const apiKey = import.meta.env.VITE_BASE_API_KEY;
      const baseUrl = import.meta.env.VITE_API_BASE_URL;
      const response = await fetch(
        `${baseUrl}/Tasks/admin/stastistics?RefreshData=${refresh ? 'true' : 'false'}`,
        {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
            'X-Api-Key': apiKey || '',
            'Content-Type': 'application/json'
          }
        }
      );
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || 'Failed to fetch statistics');
      setUserStats(data.data);
    } catch (err: any) {
      setStatsError('Failed to fetch user statistics');
      setToast('Failed to fetch user statistics');
    } finally {
      setStatsLoading(false);
      setStatsRefreshing(false);
    }
  };

  if (!isAuthenticated || !user) {
    return <div className="flex justify-center items-center min-h-screen bg-white dark:bg-gray-900 text-gray-900 dark:text-white">
      Loading...
    </div>;
  }

  const roles = Array.isArray(user['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'])
    ? user['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
    : [user['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']];
  const isAdmin = roles.includes('Admin');

  return (
    <MainLayout>
      <div className="w-full max-w-3xl mx-auto">
        <div className="flex justify-between items-center mb-8">
          <h2 className="text-2xl font-bold dark:text-white">
            Welcome, {user['given_name']}!
          </h2>
          <button onClick={toggleDarkMode} className="p-2 rounded-full hover:bg-gray-200 dark:hover:bg-gray-700" aria-label="Toggle Dark Mode" >
            {darkMode ? (
              <svg className="w-5 h-5 text-gray-900 dark:text-white" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path d="M21 12.79A9 9 0 1111.21 3a7 7 0 109.79 9.79z" />
              </svg>
            ) : (
              <svg className="w-5 h-5 text-gray-900 dark:text-white" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
                <path d="M12 3v1m0 16v1m9-9h-1M4 12H3m16.364-6.364l-.707.707M6.343 17.657l-.707.707m12.728 0l-.707-.707M6.343 6.343l-.707-.707M12 7a5 5 0 000 10a5 5 0 000-10z" />
              </svg>
            )}
          </button>
        </div>

        {/* Admin User Statistics Section */}
        {isAdmin && (
          <section className={`mb-8 transition-all ${statsLoading || statsRefreshing ? 'animate-pulse' : ''}`}>
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-xl font-semibold dark:text-white">User Statistics</h3>
              <button
                className="p-2 rounded-full hover:bg-gray-200 dark:hover:bg-gray-700"
                aria-label="Refresh User Statistics"
                onClick={() => fetchUserStats(true)}
                disabled={statsRefreshing}
                title="Refresh"
              >
                <svg
                  className={`w-5 h-5 ${statsRefreshing ? 'animate-spin' : ''}`}
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  viewBox="0 0 24 24"
                >
                  <path d="M4 4v5h.582M19.418 19A9 9 0 105 5.582" />
                </svg>
              </button>
            </div>
            {!userStats && (statsLoading || statsRefreshing) && (
              <div className="flex justify-center items-center py-8">
                <svg className="animate-spin h-8 w-8 text-purple-700" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none"/>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"/>
                </svg>
              </div>
            )}
            {userStats && (
              <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
                <div className="bg-white dark:bg-gray-800 rounded-xl shadow-md p-4 flex flex-col items-center relative">
                  <span className="text-2xl font-bold text-purple-700 dark:text-purple-300">{userStats.usersCount}</span>
                  <span className="text-xs text-gray-500 dark:text-gray-400">Total Users</span>
                </div>
                <div className="bg-white dark:bg-gray-800 rounded-xl shadow-md p-4 flex flex-col items-center">
                  <span className="text-2xl font-bold text-green-700 dark:text-green-300">{userStats.activeUsersCount}</span>
                  <span className="text-xs text-gray-500 dark:text-gray-400">Active</span>
                </div>
                <div className="bg-white dark:bg-gray-800 rounded-xl shadow-md p-4 flex flex-col items-center">
                  <span className="text-2xl font-bold text-yellow-700 dark:text-yellow-300">{userStats.deactivatedUsersCount}</span>
                  <span className="text-xs text-gray-500 dark:text-gray-400">Inactive</span>
                </div>
                <div className="bg-white dark:bg-gray-800 rounded-xl shadow-md p-4 flex flex-col items-center">
                  <span className="text-2xl font-bold text-red-700 dark:text-red-300">{userStats.deletedUsersCount}</span>
                  <span className="text-xs text-gray-500 dark:text-gray-400">Deleted</span>
                </div>
                <div className="bg-white dark:bg-gray-800 rounded-xl shadow-md p-4 flex flex-col items-center">
                  <span className="text-2xl font-bold text-gray-700 dark:text-gray-300">{userStats.pendingUsersCount}</span>
                  <span className="text-xs text-gray-500 dark:text-gray-400">Pending</span>
                </div>
              </div>
            )}
            {statsError && (
              <div className="text-red-500 mt-2 text-center">{statsError}</div>
            )}
          </section>
        )}

        <section className="mb-8">
          <h3 className="text-xl font-semibold mb-4 dark:text-white">Upcoming Tasks</h3>

          {loading && <p className="dark:text-white">Loading tasks...</p>}
          {error && <p className="text-red-500">{error}</p>}

          {!loading && !error && tasks.length === 0 && (
            <div className="flex flex-col items-center justify-center text-center py-8">
              <svg xmlns="http://www.w3.org/2000/svg" className="h-16 w-16 text-purple-400 mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <rect x="3" y="8" width="18" height="13" rx="2" strokeWidth="2" stroke="currentColor" fill="none" />
                <path stroke="currentColor" strokeWidth="2" d="M16 2v4M8 2v4M3 10h18" />
              </svg>
              <p className="dark:text-white text-lg">No upcoming tasks found.<br />Enjoy your day!</p>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {tasks.map(task => (
              <div key={task.id} className="bg-white dark:bg-gray-800 rounded-xl shadow-md p-4">
                <h4 className="font-semibold text-lg mb-2 dark:text-white">{task.title}</h4>
                <p className="text-gray-600 dark:text-gray-300 text-sm mb-2">
                  {task.description || 'No description'}
                </p>
                <p className="text-purple-600 dark:text-purple-300 text-sm">
                  {new Date(task.scheduledFor).toLocaleString()}
                </p>
              </div>
            ))}
          </div>
        </section>
      </div>
      {toast && (
        <div className="fixed bottom-6 left-1/2 transform -translate-x-1/2 bg-red-600 text-white px-4 py-2 rounded shadow-lg z-50 animate-fade-in">
          {toast}
          <button className="ml-4" onClick={() => setToast(null)}>&times;</button>
        </div>
      )}
    </MainLayout>
  );
}