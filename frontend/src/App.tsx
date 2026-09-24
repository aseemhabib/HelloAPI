import { Link, Navigate, Route, Routes } from "react-router";

import RegisterPage from "./pages/RegisterPage";
import LoginPage from "./pages/LoginPage";
import MePage from "./pages/MePage";

function App() {
  return (
    <>
      <nav>
        <Link to="/register">Register</Link>
        {" | "}
        <Link to="/login">Sign In</Link>
      </nav>

      <Routes>
        <Route path="/" element={<Navigate to="/register" replace />} />

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
          element={<MePage />}
        />
      </Routes>
    </>
  );
}

export default App;