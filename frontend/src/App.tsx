import { Link, Navigate, Route, Routes } from "react-router";

import RegisterPage from "./pages/RegisterPage";
import LoginPage from "./pages/LoginPage";
import MePage from "./pages/MePage";
import ProtectedRoute from "./components/ProtectedRoute";
import { useAuth } from "./auth/AuthContext";
import AdminPage from "./pages/AdminPage";
import AdminRoute from "./components/AdminRoute";

function App() {
  const { user, isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <p>Loading...</p>;
  }

  return (
    <>
      <nav>
        {!isLoading && !isAuthenticated && (
          <>
            <Link to="/register">
              Register
            </Link>

            {" | "}

            <Link to="/login">
              Sign In
            </Link>
          </>
        )}

        {!isLoading && isAuthenticated && (
          <Link to="/me">
            My Account
          </Link>
        )}

        {user?.roles.includes("Admin") && (
          <>
            {" | "}
            <Link to="/admin">
              Admin
            </Link>
          </>
        )}
      </nav>

      
      <Routes>
        <Route
          path="/"
          element={
            isAuthenticated
              ? <Navigate to="/me" replace />
              : <Navigate to="/login" replace />
          }
        />

        <Route
          path="/register"
          element={<RegisterPage />}
        />

        <Route
          path="/login"
          element={<LoginPage />}
        />

        <Route
          path="/me"
          element={
            <ProtectedRoute>
              <MePage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/admin"
          element={
            <AdminRoute>
              <AdminPage />
            </AdminRoute>
          }
        />
      </Routes>
    </>
  );
}

export default App;