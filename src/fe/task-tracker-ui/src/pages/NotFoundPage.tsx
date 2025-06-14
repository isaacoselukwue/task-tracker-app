import { useNavigate } from 'react-router-dom';

export default function NotFoundPage() {
  const navigate = useNavigate();
  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100">
      <img src="https://i.imgur.com/8hjLKJ4.png" alt="TaskTracker Logo" className="w-16 h-16 rounded-full mb-6 shadow" style={{ background: '#fff' }}/>
      <h1 className="text-5xl font-extrabold text-purple-700 dark:text-purple-300 mb-4">404</h1>
      <p className="text-xl mb-6">Sorry, the page you are looking for does not exist.</p>
      <button onClick={() => navigate('/')}
        className="px-6 py-2 bg-purple-700 text-white rounded-lg hover:bg-purple-800 dark:bg-purple-500 dark:hover:bg-purple-600 transition">
        Go to Homepage
      </button>
    </div>
  );
}