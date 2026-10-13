import { useEffect, useState } from "react";
import { getAdminTest } from "../api/admin";

function AdminPage() {
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    async function loadAdminData() {
      try {
        const result = await getAdminTest();
        setMessage(result.message);
      } catch (err) {
        setError(
          err instanceof Error
            ? err.message
            : "Failed to load admin data"
        );
      }
    }

    loadAdminData();
  }, []);

  return (
    <main>
      <h1>Admin</h1>

      {message && <p>{message}</p>}
      {error && <p>{error}</p>}
    </main>
  );
}

export default AdminPage;