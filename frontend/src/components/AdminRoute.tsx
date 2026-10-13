import { Navigate } from "react-router";
import { useAuth } from "../auth/AuthContext";

interface AdminRouteProps {
  children: React.ReactNode;
}

function AdminRoute({ children }: AdminRouteProps) {
  const {
    user,
    isAuthenticated,
    isLoading,
  } = useAuth();

  if (isLoading) {
    return <p>Loading...</p>;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!user?.roles.includes("Admin")) {
    return <Navigate to="/me" replace />;
  }

  return children;
}

export default AdminRoute;