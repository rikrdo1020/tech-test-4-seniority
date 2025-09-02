import { Route, Routes } from "react-router-dom";
import LoginPage from "./components/pages/LoginPage";
import { ProtectedRoute } from "./app/routes/ProtectedRoute";
import DashboardPage from "./components/pages/DashboardPage";
import { PublicRoute } from "./app/routes/PublicRoute";
import Layout from "./components/organisms/Layout";
import AllTasksPage from "./components/pages/AllTasksPage";
import TaskFormPage from "./components/pages/TaskFormPage";
import UserEditFormPage from "./components/pages/UserEditFormPage";

function App() {
  return (
    <>
      <Routes>
        <Route
          path="/login"
          element={
            <PublicRoute>
              <LoginPage />
            </PublicRoute>
          }
        />
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <Layout />
            </ProtectedRoute>
          }
        >
          <Route path="/" element={<DashboardPage />} />
          <Route path="tasks" element={<AllTasksPage />} />
          <Route path="tasks/new" element={<TaskFormPage />} />
          <Route path="tasks/:id/edit" element={<TaskFormPage />} />
          <Route path="me/edit" element={<UserEditFormPage />} />
        </Route>
      </Routes>
    </>
  );
}

export default App;
