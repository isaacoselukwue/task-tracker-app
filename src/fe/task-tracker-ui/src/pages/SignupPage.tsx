import { useState } from 'react';
import { Link } from 'react-router-dom';
import AuthLayout from './components/AuthLayout';

export default function SignupPage() {
  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    emailAddress: '',
    password: '',
    confirmPassword: '',
    phoneNumber: '',
  });
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

    const validate = () => {
      const errors: string[] = [];
      if (!form.firstName.trim()) errors.push('First name is required.');
      if (!form.lastName.trim()) errors.push('Last name is required.');
      if (!form.emailAddress.trim()) errors.push('Email is required.');
      if (!/\S+@\S+\.\S+/.test(form.emailAddress)) errors.push('Email is invalid.');
      if (!form.phoneNumber.trim()) errors.push('Phone number is required.');
      if (!form.password) errors.push('Password is required.');
      if (form.password.length < 12) errors.push('Password must be at least 12 characters.');
      if (form.password !== form.confirmPassword) errors.push('Passwords do not match.');
      return errors;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setValidationErrors([]);
    setLoading(true);

    const errors = validate();
    if (errors.length > 0) {
      setValidationErrors(errors);
      setLoading(false);
      return;
    }

    try {
      const apiKey = import.meta.env.VITE_BASE_API_KEY;
      const baseUrl = import.meta.env.VITE_API_BASE_URL;
      const response = await fetch(`${baseUrl}/authentication/signup`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Api-Key': apiKey || '',
        },
        body: JSON.stringify(form),
      });
      const data = await response.json();
      if (!response.ok) {
        if (
          data.message === 'Sign up failed. Please review errors and try again.' && Array.isArray(data.errors)
        ) {
          setValidationErrors(data.errors);
        } else {
          setError(data.message || 'Signup failed');
        }
        setLoading(false);
        return;
      }
      setSuccess(true);
    } catch (err: any) {
      setError(err.message || 'Signup failed');
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <AuthLayout title="Check Your Email">
        <div className="text-center">
          <p className="mb-4">Signup successful! Please check your email for an activation link.</p>
          <Link to="/login" className="text-purple-700 dark:text-purple-300 hover:underline">Go to Login</Link>
        </div>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title="Create an Account">
      <form onSubmit={handleSubmit}>
        {error && (<div className="mb-4 p-2 bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-200 rounded">{error}</div>)}
        {validationErrors.length > 0 && (
          <div className="mb-4 p-2 bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-200 rounded">
            <ul className="list-disc pl-5">
              {validationErrors.map((err, i) => <li key={i}>{err}</li>)}
            </ul>
          </div>
        )}
        <input name="firstName" type="text" placeholder="First Name" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700" value={form.firstName} onChange={handleChange} required />
        <input name="lastName" type="text" placeholder="Last Name" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700" value={form.lastName} onChange={handleChange} required />
        <input name="emailAddress" type="email" placeholder="Email" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700" value={form.emailAddress} onChange={handleChange} required />
        <input name="phoneNumber" type="text" placeholder="Phone Number" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700" value={form.phoneNumber} onChange={handleChange} required />
        <input name="password" type="password" placeholder="Password" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700" value={form.password} onChange={handleChange} required />
        <input name="confirmPassword" type="password" placeholder="Confirm Password" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700" value={form.confirmPassword} onChange={handleChange} required />
        <button type="submit" className="w-full bg-purple-700 text-white py-2 rounded hover:bg-purple-800 dark:bg-purple-600 dark:hover:bg-purple-700 transition" disabled={loading}>
          {loading ? 'Signing up...' : 'Sign Up'}
        </button>
        <div className="mt-4 text-sm text-center">
          Already have an account?{' '}
          <Link to="/login" className="text-purple-700 dark:text-purple-300 hover:underline">
            Login
          </Link>
        </div>
      </form>
    </AuthLayout>
  );
}