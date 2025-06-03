import { Link } from 'react-router-dom';
import AuthLayout from './components/AuthLayout';

export default function SignupPage() {
  return (
    <AuthLayout title="Create an Account">
      <form>
        <input type="text" placeholder="Name" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700"/>
        <input type="email" placeholder="Email" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700"/>
        <input type="password" placeholder="Password" className="block w-full mb-4 p-2 border rounded bg-white dark:bg-gray-700" />
        <button type="submit" className="w-full bg-purple-700 text-white py-2 rounded hover:bg-purple-800 dark:bg-purple-600 dark:hover:bg-purple-700 transition">
          Sign Up
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