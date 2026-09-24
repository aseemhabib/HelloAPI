export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
}

export interface CurrentUser {
  id: string;
  email: string;
  name: string;
}

export async function login(
  request: LoginRequest
): Promise<LoginResponse> {
  const response = await fetch("/api/auth/login", {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error("Invalid email or password.");
    }

    throw new Error(`Login failed: ${response.status}`);
  }

  return response.json();
}

export async function getMe(): Promise<CurrentUser> {
  const response = await fetch("/api/auth/me", {
    credentials: "include",
  });

  if (response.status === 401) {
    throw new Error("Unauthorized");
  }

  if (!response.ok) {
    throw new Error(`Failed to get current user: ${response.status}`);
  }

  return response.json();
}

export async function logout(): Promise<void> {
  const csrfToken = await getCsrfToken();

  const response = await fetch("/api/auth/logout", {
    method: "POST",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": csrfToken,
    },
  });

  if (!response.ok) {
    throw new Error(`Logout failed: ${response.status}`);
  }
}

export async function getCsrfToken(): Promise<string> {
  const response = await fetch("/api/auth/csrf-token", {
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error(
      `Failed to get CSRF token: ${response.status}`
    );
  }

  const result = await response.json();

  return result.token;
}