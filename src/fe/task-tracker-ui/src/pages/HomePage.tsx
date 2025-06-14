import { useNavigate } from 'react-router-dom';
import { useTheme } from '../context/ThemeContext';

export default function HomePage() {
  const { darkMode, toggleDarkMode } = useTheme();
  const navigate = useNavigate();

  return (
    <div className="min-h-screen bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 transition-colors duration-300">
      <nav className="w-full max-w-6xl mx-auto px-4 py-6 flex justify-between items-center">
        <div className="flex items-center space-x-2">
        <img src="https://i.imgur.com/8hjLKJ4.png" alt="TaskTracker Logo" className="w-8 h-8 rounded-full object-cover shadow" style={{ background: '#fff' }} />
        <span className="text-2xl font-bold text-purple-700 dark:text-purple-300">TaskTracker</span>
      </div>
        <div className="flex items-center space-x-4">
          <button onClick={toggleDarkMode} className="p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-700 transition"
            aria-label="Toggle dark mode">
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

          <button onClick={() => navigate('/login')} className="px-4 py-2 text-purple-700 dark:text-purple-300 font-semibold hover:underline">
            Login
          </button>
          <button onClick={() => navigate('/signup')} className="px-4 py-2 bg-purple-700 text-white rounded-xl hover:bg-purple-800 dark:bg-purple-500 dark:hover:bg-purple-600">
            Sign Up
          </button>
        </div>
      </nav>

      <section className="text-center mt-20 max-w-2xl mx-auto">
        <h2 className="text-5xl font-extrabold text-purple-800 dark:text-purple-300 leading-tight">
          Organise Your Day <br /> with Ease ✨
        </h2>
        <p className="mt-6 text-gray-600 dark:text-gray-300 text-lg">
          TaskTracker helps you stay on top of your to-dos with a smart, simple, and beautiful interface.
        </p>
        <div className="mt-8 space-x-4">
          <button className="bg-purple-700 text-white px-6 py-3 rounded-lg hover:bg-purple-800 dark:bg-purple-500 dark:hover:bg-purple-600 transition-all"
            onClick={() => navigate('/signup')}>
            Get Started
          </button>
          <button className="text-purple-700 dark:text-purple-300 font-semibold hover:underline" onClick={() => navigate('/forgot-password')}>
            Forgot Password?
          </button>
        </div>
      </section>

      <section className="mt-24 grid md:grid-cols-3 gap-10 text-center max-w-6xl mx-auto px-4">
        <div className="bg-white dark:bg-gray-800 p-6 rounded-xl shadow-md">
          <h3 className="text-xl font-bold text-purple-700 dark:text-purple-300">Plan Tasks</h3>
          <p className="text-gray-600 dark:text-gray-300 mt-2">Create tasks with ease and keep everything organized.</p>
        </div>
        <div className="bg-white dark:bg-gray-800 p-6 rounded-xl shadow-md">
          <h3 className="text-xl font-bold text-purple-700 dark:text-purple-300">Track Progress</h3>
          <p className="text-gray-600 dark:text-gray-300 mt-2">Visualise your progress and stay motivated daily.</p>
        </div>
        <div className="bg-white dark:bg-gray-800 p-6 rounded-xl shadow-md">
          <h3 className="text-xl font-bold text-purple-700 dark:text-purple-300">Stay Productive</h3>
          <p className="text-gray-600 dark:text-gray-300 mt-2">Smart reminders to help you beat procrastination.</p>
        </div>
      </section>

      <footer className="mt-32 text-gray-400 dark:text-gray-500 text-sm text-center pb-10">
        &copy; {new Date().getFullYear()} TaskTracker. All rights reserved.
      </footer>
    </div>
  );
}