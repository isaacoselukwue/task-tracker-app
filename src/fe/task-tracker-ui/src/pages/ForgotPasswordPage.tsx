import { useState } from 'react';
import AuthLayout from './components/AuthLayout';

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const apiKey = import.meta.env.VITE_BASE_API_KEY;
      const baseUrl = import.meta.env.VITE_API_BASE_URL;
      const response = await fetch(`${baseUrl}/account/password-reset/initial`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Api-Key': apiKey || '',
        },
        body: JSON.stringify({ emailAddress: email }),
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data.message || 'Failed to send reset email');
      setSuccess(true);
    } catch (err: any) {
      setError(err.message || 'Failed to send reset email');
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout title="Forgot Password">
      {success ? (
        <div className="text-center">
          <p className="mb-4">If your email exists, a password reset link has been sent.</p>
        </div>
      ) : (
        <form onSubmit={handleSubmit}>
          {error && <div className="mb-4 p-2 bg-red-100 text-red-700 rounded">{error}</div>}
          <input
            type="email"
            className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700"
            placeholder="Enter your email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
          />
          <button type="submit"
            className="w-full bg-purple-700 text-white py-2 rounded hover:bg-purple-800 dark:bg-purple-600 transition flex items-center justify-center"
            disabled={loading} >
            {loading ? (
              <>
                <span className="inline-block mr-2">
                  <svg className="animate-spin h-5 w-5 text-white" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none"/>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"/>
                  </svg>
                </span>
                Sending...
              </>
            ) : (
              'Send Reset Link'
            )}
          </button>
        </form>
      )}
    </AuthLayout>
  );
}