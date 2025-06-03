import { Link } from 'react-router-dom';
import { useTheme } from '../../context/ThemeContext';

export default function AuthLayout({ title, children }: { title: string; children: React.ReactNode }) {
  const { darkMode, toggleDarkMode } = useTheme();
  return (
    <div className="min-h-screen bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 flex items-center justify-center px-4 relative">
      <button onClick={toggleDarkMode} className="absolute top-6 right-6 p-2 rounded-full hover:bg-gray-200 dark:hover:bg-gray-700" aria-label="Toggle Dark Mode">
        {darkMode ? (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path d="M21 12.79A9 9 0 1111.21 3a7 7 0 109.79 9.79z" />
          </svg>
        ) : (
          <svg className="w-5 h-5" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24">
            <path d="M12 3v1m0 16v1m9-9h-1M4 12H3m16.364-6.364l-.707.707M6.343 17.657l-.707.707m12.728 0l-.707-.707M6.343 6.343l-.707-.707M12 7a5 5 0 000 10a5 5 0 000-10z" />
          </svg>
        )}
      </button>

      {/* Auth Card */}
      <div className="max-w-md w-full bg-white dark:bg-gray-800 p-6 rounded-xl shadow-md">
        <div className="mb-6 text-center">
          <Link to="/" className="text-3xl font-bold text-purple-700 dark:text-purple-300">
            TaskTracker
          </Link>
          <h2 className="text-xl font-semibold mt-2">{title}</h2>
        </div>
        {children}
        <div className="mt-4 text-sm text-center text-gray-600 dark:text-gray-400">
          <Link to="/" className="hover:underline text-purple-600 dark:text-purple-300">
            Back to Home
          </Link>
        </div>
      </div>
    </div>
  );
}