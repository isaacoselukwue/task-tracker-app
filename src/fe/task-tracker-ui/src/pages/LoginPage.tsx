import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import AuthLayout from './components/AuthLayout';
import { useSession } from '../context/SessionContext';
import { login } from '../services/AuthService';

export default function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  const location = useLocation();
  const navigate = useNavigate();
  const { isAuthenticated, updateSession } = useSession();

  useEffect(() => {
    if (isAuthenticated) {
      navigate('/dashboard', { replace: true });
    }
  }, [isAuthenticated, navigate]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setErrors([]);
    setLoading(true);

    try {
      const result = await login({ emailAddress: email, password });
      if (!result.succeeded) {
        setError(result.message || 'Login failed. Please try again.');
        if (Array.isArray(result.errors) && result.errors.length > 0) {
          setErrors(result.errors);
        }
        setLoading(false);
        return;
      }
      updateSession();
    } catch (err: any) {
      setError(err.message || 'Login failed. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout title="Welcome Back">
      {location.state?.verified && (
        <div className="mb-4 p-2 bg-green-100 dark:bg-green-900 text-green-700 dark:text-green-200 rounded">
          Account verified! You can now log in.
        </div>
      )}
      <form onSubmit={handleSubmit}>
        {error && (
          <div className="mb-4 p-2 bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-200 rounded">
            <div className="font-semibold">{error}</div>
            {errors.length > 0 && (
              <ul className="list-disc pl-5 mt-2">
                {errors.map((err, i) => (
                  <li key={i}>{err}</li>
                ))}
              </ul>
            )}
          </div>
        )}
        <input type="email" placeholder="Email" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700"
          value={email} onChange={(e) => setEmail(e.target.value)} required />
        <input type="password" placeholder="Password" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700"
          value={password} onChange={(e) => setPassword(e.target.value)} required />
        <button type="submit" className="w-full bg-purple-700 text-white py-2 rounded hover:bg-purple-800 dark:bg-purple-600 dark:hover:bg-purple-700 transition"
          disabled={loading}>
          {loading ? 'Signing in...' : 'Sign In'}
        </button>
        <div className="mt-4 text-sm text-center">
          <Link to="/forgot-password" className="text-purple-700 dark:text-purple-300 hover:underline mr-2">
            Forgot Password?
          </Link>
          <span>
            Don't have an account?{' '}
            <Link to="/signup" className="text-purple-700 dark:text-purple-300 hover:underline">
              Sign Up
            </Link>
          </span>
        </div>
      </form>
    </AuthLayout>
  );
}