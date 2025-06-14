interface LoginRequest {
  emailAddress: string;
  password: string;
}

interface AccessTokenResponse {
  tokenType: string | null;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface LoginResponse {
  data?: {
    accessToken: AccessTokenResponse;
  } | null;
  succeeded: boolean;
  message: string;
  errors: string[];
}

export const login = async (credentials: LoginRequest): Promise<LoginResponse> => {
  try {
    const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
    const API_KEY = import.meta.env.VITE_BASE_API_KEY;
    
    const response = await fetch(`${API_BASE_URL}/authentication/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Api-Key': API_KEY
      },
      body: JSON.stringify(credentials)
    });
    
    const data: LoginResponse = await response.json();
    
    if (response.ok && data.succeeded) {
      localStorage.setItem('accessToken', data.data!.accessToken.accessToken);
      localStorage.setItem('refreshToken', data.data!.accessToken.refreshToken);
      localStorage.setItem('expiresIn', data.data!.accessToken.expiresIn.toString());
      window.dispatchEvent(new Event('authChanged'));
      return data;
    } else {
      throw new Error(data.message || data.errors?.join(', ') || 'Login failed');
    }
  } catch (error) {
    console.error('Login error:', error);
    throw error;
  }
};

export const logout = (): void => {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
};

export const apiLogout = async (): Promise<void> => {
  const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
  const API_KEY = import.meta.env.VITE_BASE_API_KEY;
  const refreshToken = localStorage.getItem('refreshToken');
  if (!refreshToken) return;

  try {
    await fetch(`${API_BASE_URL}/authentication/logout`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Api-Key': API_KEY
      },
      body: JSON.stringify({ EncryptedToken: refreshToken })
    });
  } catch (error) {
    console.error('Logout API error:', error);
  }
};