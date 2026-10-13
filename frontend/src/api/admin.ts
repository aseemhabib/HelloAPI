export interface AdminTestResponse {
  message: string;
}

export async function getAdminTest(): Promise<AdminTestResponse> {
  const response = await fetch("/api/admin/test", {
    credentials: "include",
  });

  if (response.status === 401) {
    throw new Error("Unauthorized");
  }

  if (response.status === 403) {
    throw new Error("Forbidden");
  }

  if (!response.ok) {
    throw new Error(`Admin request failed: ${response.status}`);
  }

  return response.json();
}