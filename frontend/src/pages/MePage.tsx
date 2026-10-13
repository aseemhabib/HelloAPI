import { useNavigate } from "react-router";
import { useAuth } from "../auth/AuthContext";

function MePage() {
  const navigate = useNavigate();

  const {
    user,
    logout,
  } = useAuth();

  async function handleLogout() {
    try {
      await logout();

      navigate(
        "/login",
        { replace: true }
      );
    } catch {
      console.error("Logout failed");
    }
  }

  if (!user) {
    return null;
  }

  return (
    <main>
      <h1>My Account</h1>

      <p>
        <strong>Name:</strong>{" "}
        {user.name}
      </p>

      <p>
        <strong>Email:</strong>{" "}
        {user.email}
      </p>

      <p>
        <strong>User ID:</strong>{" "}
        {user.id}
      </p>

      <button onClick={handleLogout}>
        Log out
      </button>
    </main>
  );
}

export default MePage;