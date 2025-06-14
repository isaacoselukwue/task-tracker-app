import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor, cleanup } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import LoginPage from './LoginPage';
import { useSession } from '../context/SessionContext';
import * as AuthService from '../services/AuthService';

vi.mock('../context/SessionContext', async (importOriginal) => {
  const actual = await importOriginal<typeof import('../context/SessionContext')>();
  return {
    ...actual,
    useSession: vi.fn(() => ({
      isAuthenticated: false,
      updateSession: vi.fn(),
      user: null,
      logout: vi.fn(),
      refreshToken: vi.fn(),
      tokenExpiresIn: null,
      bootstrapping: false,
    })),
  };
});

vi.mock('../services/AuthService', () => ({
  login: vi.fn(),
}));

const mockLogin = vi.mocked(AuthService.login);
const mockUseSession = vi.mocked(useSession);


function renderWithRouter(ui: React.ReactElement, route = '/login', state?: any) {
  return render(
    <MemoryRouter initialEntries={[{ pathname: route, state }]}>
      <Routes>
        <Route path="/login" element={ui} />
        <Route path="/dashboard" element={<div>Dashboard</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe('LoginPage', () => {
  let mockedUpdateSession: ReturnType<typeof vi.fn>;
  let mockedLogout: ReturnType<typeof vi.fn>;
  let mockedRefreshToken: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    // Reset mocks before each test
    vi.clearAllMocks();

    // Re-initialize the mock for useSession for each test to get a fresh updateSession mock
    mockedUpdateSession = vi.fn();
    mockedLogout = vi.fn();
    mockedRefreshToken = vi.fn();
    mockUseSession.mockReturnValue({
      isAuthenticated: false,
      updateSession: mockedUpdateSession,
      user: null,
      logout: mockedLogout,
      refreshToken: mockedRefreshToken,
      tokenExpiresIn: null,
      bootstrapping: false,
    });
  });

  afterEach(() => {
    cleanup();
  });

  it('renders login form', () => {
    renderWithRouter(<LoginPage />);
    expect(screen.getByPlaceholderText(/email/i)).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('shows verified message if location.state.verified is true', () => {
    renderWithRouter(<LoginPage />, '/login', { verified: true });
    expect(screen.getByText(/account verified! you can now log in./i)).toBeInTheDocument();
  });

  it('shows error message on failed login', async () => {
    mockLogin.mockResolvedValueOnce({
      succeeded: false,
      message: 'Invalid credentials',
      errors: ['Email or password is incorrect'],
      data: null,
    });

    renderWithRouter(<LoginPage />);
    fireEvent.change(screen.getByPlaceholderText(/email/i), { target: { value: 'test@example.com' } });
    fireEvent.change(screen.getByPlaceholderText(/password/i), { target: { value: 'wrongpass' } });
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument();
      expect(screen.getByText(/email or password is incorrect/i)).toBeInTheDocument();
    });
  });

  it('calls updateSession on successful login', async () => {
    mockLogin.mockResolvedValueOnce({ succeeded: true,
      data: {
        accessToken: {
          tokenType: "Bearer",
          accessToken: "mock-access-token",
          expiresIn: 3600,
          refreshToken: "mock-refresh-token"
        } 
      },
      message: 'Login successful',
      errors: [], });

    renderWithRouter(<LoginPage />);
    fireEvent.change(screen.getByPlaceholderText(/email/i), { target: { value: 'test@example.com' } });
    fireEvent.change(screen.getByPlaceholderText(/password/i), { target: { value: 'rightpass' } });
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(mockedUpdateSession).toHaveBeenCalled();
    });
  });

  it('disables button and shows loading text while submitting', async () => {
    mockLogin.mockImplementation(() => new Promise(() => {})); // never resolves

    renderWithRouter(<LoginPage />);
    fireEvent.change(screen.getByPlaceholderText(/email/i), { target: { value: 'test@example.com' } });
    fireEvent.change(screen.getByPlaceholderText(/password/i), { target: { value: 'rightpass' } });
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }));

    expect(screen.getByRole('button', { name: /signing in.../i })).toBeDisabled();
  });
});