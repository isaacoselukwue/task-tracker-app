import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import AuthLayout from './components/AuthLayout';

export default function PasswordResetPage() {
  const { tokenAndUserId } = useParams();
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setValidationErrors([]);
    if (newPassword.length < 12) {
      setValidationErrors(['Password must be at least 12 characters.']);
      return;
    }
    if (newPassword !== confirmPassword) {
      setValidationErrors(['Passwords do not match.']);
      return;
    }
    setLoading(true);
    try {
      if (!tokenAndUserId) throw new Error('Invalid reset link.');
      const [token, userid] = tokenAndUserId.split('&');
      if (!token || !userid) throw new Error('Invalid reset link.');
      const apiKey = import.meta.env.VITE_BASE_API_KEY;
      const baseUrl = import.meta.env.VITE_API_BASE_URL;
      const response = await fetch(`${baseUrl}/account/password-reset`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Api-Key': apiKey || '',
        },
        body: JSON.stringify({
          userId: userid,
          resetToken: token,
          newPassword,
          confirmPassword,
        }),
      });
      const data = await response.json();
      if (!response.ok) {
        if (
          data.message === 'Password reset failed. Please review errors and try again.' &&
          Array.isArray(data.errors)
        ) {
          setValidationErrors(data.errors);
        } else {
          setError(data.message || 'Password reset failed');
        }
        setLoading(false);
        return;
      }
      setSuccess(true);
      setTimeout(() => navigate('/login'), 2000);
    } catch (err: any) {
      setError(err.message || 'Password reset failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthLayout title="Reset Password">
      {success ? (
        <div className="text-center">
          <p className="mb-4">Password reset successful! Redirecting to login...</p>
        </div>
      ) : (
        <form onSubmit={handleSubmit}>
          {error && <div className="mb-4 p-2 bg-red-100 text-red-700 rounded">{error}</div>}
          {validationErrors.length > 0 && (
            <div className="mb-4 p-2 bg-red-100 text-red-700 rounded">
              <ul className="list-disc pl-5">
                {validationErrors.map((err, i) => <li key={i}>{err}</li>)}
              </ul>
            </div>
          )}
          <input
            type="password"
            className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700"
            placeholder="New password"
            value={newPassword}
            onChange={e => setNewPassword(e.target.value)}
            required
          />
          <input
            type="password"
            className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700"
            placeholder="Confirm new password"
            value={confirmPassword}
            onChange={e => setConfirmPassword(e.target.value)}
            required
          />
          <button
            type="submit"
            className="w-full bg-purple-700 text-white py-2 rounded hover:bg-purple-800 dark:bg-purple-600 transition"
            disabled={loading}
          >
            {loading ? 'Resetting...' : 'Reset Password'}
          </button>
        </form>
      )}
    </AuthLayout>
  );
}