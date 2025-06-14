import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import AuthLayout from './components/AuthLayout';

export default function SignupVerificationPage() {
  const { tokenAndUserId } = useParams();
  const [progress, setProgress] = useState(1);
  const [status, setStatus] = useState<'pending' | 'success' | 'error'>('pending');
  const [message, setMessage] = useState('');
  const navigate = useNavigate();

  useEffect(() => {
    let timer: NodeJS.Timeout;
    if (status === 'pending' && progress < 100) {
      timer = setTimeout(() => setProgress((p) => Math.min(p + Math.floor(Math.random() * 10) + 5, 100)), 100);
    }
    return () => clearTimeout(timer);
  }, [progress, status]);

  useEffect(() => {
    const verify = async () => {
        if (!tokenAndUserId) {
        setStatus('error');
        setMessage('Invalid verification link.');
        return;
        }
        const [token, userid] = tokenAndUserId.split('&');
        if (!token || !userid) {
        setStatus('error');
        setMessage('Invalid verification link.');
        return;
        }
        try {
        const apiKey = import.meta.env.VITE_BASE_API_KEY;
        const baseUrl = import.meta.env.VITE_API_BASE_URL;
        const response = await fetch(`${baseUrl}/authentication/signup/verify`, {
            method: 'POST',
            headers: {
            'Content-Type': 'application/json',
            'X-Api-Key': apiKey || '',
            },
            body: JSON.stringify({ UserId: userid, ActivationToken: token }),
        });
        const data = await response.json();
        if (!response.ok) throw new Error(data.message || 'Verification failed');
        setProgress(100);
        setStatus('success');
        setTimeout(() => navigate('/login', { replace: true, state: { verified: true } }), 2000);
        } catch (err: any) {
        setStatus('error');
        setMessage(err.message || 'Verification failed');
        }
    };
    verify();
    }, [tokenAndUserId, navigate]);

  return (
    <AuthLayout title="Verifying Account">
      <div className="flex flex-col items-center justify-center min-h-[200px]">
        {status === 'pending' && (
          <>
            <div className="w-full bg-gray-200 rounded-full h-4 mb-4 dark:bg-gray-700">
              <div className="bg-purple-700 h-4 rounded-full transition-all" style={{ width: `${progress}%` }} />
            </div>
            <p className="text-center">Verifying your account... {progress}%</p>
          </>
        )}
        {status === 'success' && (
          <div className="text-center">
            <p className="text-green-600 dark:text-green-400 mb-2">Account verified successfully!</p>
            <p>Redirecting to login...</p>
          </div>
        )}
        {status === 'error' && (
          <div className="text-center">
            <p className="text-red-600 dark:text-red-400 mb-2">{message}</p>
            <p>
              <a href="/login" className="text-purple-700 dark:text-purple-300 hover:underline">Go to Login</a>
            </p>
          </div>
        )}
      </div>
    </AuthLayout>
  );
}