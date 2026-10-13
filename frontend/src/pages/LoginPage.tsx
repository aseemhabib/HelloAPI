import { useState } from "react";
import { login } from "../api/auth";
import { useNavigate } from "react-router";
import { useAuth } from "../auth/AuthContext";

function LoginPage() {
  const navigate = useNavigate();
  const { refreshUser } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const [message, setMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setIsSubmitting(true);
    setMessage("");

    try {
      await login({
        email,
        password,
      });
      await refreshUser();
      navigate("/me", { replace: true });

      //setMessage(`Welcome ${user.firstName}!`);
    } catch (error) {
      if (error instanceof Error) {
        setMessage(error.message);
      } else {
        setMessage("Login failed.");
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main>
      <h1>Sign In</h1>

      <form onSubmit={handleSubmit}>
        <div className="input-group mb-3">
          <label htmlFor="email">Email</label>
          <br />

          <input
            id="email"
            type="email"
            value={email}
            className="form-control"
            onChange={(event) => setEmail(event.target.value)}
            required
          />
        </div>

        <div className="input-group mb-3">
          <label htmlFor="password">Password</label>
          <br />

          <input
            id="password"
            type="password"
            value={password}
            className="form-control"
            onChange={(event) => setPassword(event.target.value)}
            required
          />
        </div>

        <br />

        <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
          {isSubmitting ? "Signing in..." : "Sign in"}
        </button>
      </form>

      {message && <p>{message}</p>}
    </main>
  );
}

export default LoginPage;