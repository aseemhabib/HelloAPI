import { useEffect, useState } from "react";
import { useNavigate } from "react-router";

import {
  getMe,
  logout,
  type CurrentUser,
} from "../api/auth";

function MePage() {
  const navigate = useNavigate();

  const [user, setUser] = useState<CurrentUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function loadUser() {
      try {
        const currentUser = await getMe();
        setUser(currentUser);
      } catch {
        navigate("/login", { replace: true });
      } finally {
        setIsLoading(false);
      }
    }

    loadUser();
  }, [navigate]);

  async function handleLogout() {
    try {
      await logout();
      navigate("/login", { replace: true });
    } catch {
      setError("Logout failed.");
    }
  }

  if (isLoading) {
    return <p>Loading...</p>;
  }

  if (!user) {
    return null;
  }

  return (
    <main>
      <h1>My Account</h1>

      <p>
        <strong>Name:</strong> {user.name}
      </p>

      <p>
        <strong>Email:</strong> {user.email}
      </p>

      <p>
        <strong>User ID:</strong> {user.id}
      </p>

      <button onClick={handleLogout}>
        Log out
      </button>

      {error && <p>{error}</p>}
    </main>
  );
}

export default MePage;