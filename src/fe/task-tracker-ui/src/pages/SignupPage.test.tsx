import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import SignupPage from './SignupPage';

vi.stubEnv('VITE_BASE_API_KEY', 'test-api-key');
vi.stubEnv('VITE_API_BASE_URL', 'http://localhost:3000/api');

const mockFetch = vi.fn();
vi.stubGlobal('fetch', mockFetch);

function renderWithRouter(ui: React.ReactElement, initialRoute = '/signup') {
  return render(
    <MemoryRouter initialEntries={[initialRoute]}>
      <Routes>
        <Route path="/signup" element={ui} />
        <Route path="/login" element={<div>Login Page Mock</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe('SignupPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockFetch.mockReset();
  });

  afterEach(() => {
    cleanup();
  });

  const fillForm = (data: Record<string, string>) => {
    if (data.firstName) fireEvent.change(screen.getByPlaceholderText(/first name/i), { target: { value: data.firstName } });
    if (data.lastName) fireEvent.change(screen.getByPlaceholderText(/last name/i), { target: { value: data.lastName } });
    if (data.emailAddress) fireEvent.change(screen.getByPlaceholderText(/email/i), { target: { value: data.emailAddress } });
    if (data.phoneNumber) fireEvent.change(screen.getByPlaceholderText(/phone number/i), { target: { value: data.phoneNumber } });
    if (data.password) fireEvent.change(screen.getByPlaceholderText(/^password$/i), { target: { value: data.password } });
    if (data.confirmPassword) fireEvent.change(screen.getByPlaceholderText(/confirm password/i), { target: { value: data.confirmPassword } });
  };

  const validFormData = {
    firstName: 'Test',
    lastName: 'User',
    emailAddress: 'test@example.com',
    phoneNumber: '1234567890',
    password: 'Password123!',
    confirmPassword: 'Password123!',
  };

  it('renders the signup form correctly', () => {
    renderWithRouter(<SignupPage />);
    expect(screen.getByPlaceholderText(/first name/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/last name/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/email/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/phone number/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/^password$/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/confirm password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign up/i })).toBeInTheDocument();
    expect(screen.getByText(/already have an account\?/i)).toBeInTheDocument();
  });

  it('shows client-side validation errors for empty fields', async () => {
    renderWithRouter(<SignupPage />);
    const signupButton = screen.getByRole('button', { name: /sign up/i });
    const formElement = signupButton.closest('form');
    expect(formElement).toBeInTheDocument();

    if (formElement) {
      fireEvent.submit(formElement);
    }


    await waitFor(() => {
      expect(screen.getByText(/first name is required\./i)).toBeInTheDocument();
      expect(screen.getByText(/last name is required\./i)).toBeInTheDocument();
      expect(screen.getByText(/email is required\./i)).toBeInTheDocument();
      expect(screen.getByText(/phone number is required\./i)).toBeInTheDocument();
      expect(screen.getByText(/password is required\./i)).toBeInTheDocument();
    });
    expect(mockFetch).not.toHaveBeenCalled();
  });

  it('shows client-side validation error for invalid email', async () => {
    renderWithRouter(<SignupPage />);
    fillForm({ ...validFormData, emailAddress: 'invalid-email' });

    const signupButton = screen.getByRole('button', { name: /sign up/i });
    const formElement = signupButton.closest('form');
    expect(formElement).toBeInTheDocument();

    if (formElement) {
      fireEvent.submit(formElement);
    }

    await waitFor(() => {
      expect(screen.getByText(/email is invalid\./i)).toBeInTheDocument();
    });
    expect(mockFetch).not.toHaveBeenCalled();
  });

  it('shows client-side validation error for short password', async () => {
    renderWithRouter(<SignupPage />);
    fillForm({ ...validFormData, password: 'short', confirmPassword: 'short' });
    const signupButton = screen.getByRole('button', { name: /sign up/i });
    const formElement = signupButton.closest('form');
    expect(formElement).toBeInTheDocument();
    if (formElement) fireEvent.submit(formElement);


    await waitFor(() => {
      expect(screen.getByText(/password must be at least 12 characters\./i)).toBeInTheDocument();
    });
    expect(mockFetch).not.toHaveBeenCalled();
  });

  it('shows client-side validation error when passwords do not match', async () => {
    renderWithRouter(<SignupPage />);
    fillForm({ ...validFormData, confirmPassword: 'DifferentPassword123!' });
    const signupButton = screen.getByRole('button', { name: /sign up/i });
    const formElement = signupButton.closest('form');
    expect(formElement).toBeInTheDocument();
    if (formElement) fireEvent.submit(formElement);

    await waitFor(() => {
      expect(screen.getByText(/passwords do not match\./i)).toBeInTheDocument();
    });
    expect(mockFetch).not.toHaveBeenCalled();
  });

  it('submits the form and shows success message on successful signup', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ message: 'Signup successful' }),
    });

    renderWithRouter(<SignupPage />);
    fillForm(validFormData);
    
    const signupButton = screen.getByRole('button', { name: /sign up/i });
    const formElement = signupButton.closest('form');
    expect(formElement).toBeInTheDocument();
    if (formElement) fireEvent.submit(formElement);


    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        'http://localhost:3000/api/authentication/signup',
        expect.objectContaining({
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'X-Api-Key': 'test-api-key',
          },
          body: expect.any(String), 
        })
      );
      const fetchCall = mockFetch.mock.calls[0];
      const requestBody = JSON.parse(fetchCall[1].body);
      expect(requestBody).toEqual(validFormData);
    });

    await waitFor(() => {
      expect(screen.getByText(/signup successful! please check your email for an activation link./i)).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /go to login/i })).toBeInTheDocument();
    });
  });

  it('shows a generic error message from API if signup fails without specific errors array', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: async () => ({ message: 'A general API error occurred' }),
    });

    renderWithRouter(<SignupPage />);
    fillForm(validFormData);
    const signupButton = screen.getByRole('button', { name: /sign up/i });
    const formElement = signupButton.closest('form');
    expect(formElement).toBeInTheDocument();
    if (formElement) fireEvent.submit(formElement);

    await waitFor(() => {
      expect(screen.getByText(/a general api error occurred/i)).toBeInTheDocument();
    });
  });

  it('shows validation errors from API if signup fails with errors array', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      json: async () => ({
        message: 'Sign up failed. Please review errors and try again.',
        errors: ['Backend: Email already exists.', 'Backend: Invalid phone format.'],
      }),
    });

    renderWithRouter(<SignupPage />);
    fillForm(validFormData);
    const signupButton = screen.getByRole('button', { name: /sign up/i });
    const formElement = signupButton.closest('form');
    expect(formElement).toBeInTheDocument();
    if (formElement) fireEvent.submit(formElement);

    await waitFor(() => {
      expect(screen.getByText(/backend: email already exists./i)).toBeInTheDocument();
      expect(screen.getByText(/backend: invalid phone format./i)).toBeInTheDocument();
    });
  });

  it('handles network error during fetch', async () => {
    mockFetch.mockRejectedValueOnce(new Error('Network request failed'));

    renderWithRouter(<SignupPage />);
    fillForm(validFormData);
    const signupButton = screen.getByRole('button', { name: /sign up/i });
    const formElement = signupButton.closest('form');
    expect(formElement).toBeInTheDocument();
    if (formElement) fireEvent.submit(formElement);

    await waitFor(() => {
      expect(screen.getByText(/network request failed/i)).toBeInTheDocument();
    });
  });

  it('disables button and shows loading text while submitting', async () => {
    mockFetch.mockImplementation(() => new Promise(() => {})); 

    renderWithRouter(<SignupPage />);
    fillForm(validFormData);
    const signupButton = screen.getByRole('button', { name: /sign up/i });
    const formElement = signupButton.closest('form');
    expect(formElement).toBeInTheDocument();
    if (formElement) fireEvent.submit(formElement);


    expect(screen.getByRole('button', { name: /signing up.../i })).toBeDisabled();
    await waitFor(() => expect(mockFetch).toHaveBeenCalledTimes(1));
  });
});