import { Routes, Route, Navigate } from "react-router-dom";

import Login from "../pages/Login/Login";
import ProtectedRoute from "./ProtectedRoute";

import Dashboard from "../pages/Dashboard/Dashboard";

const AppRoutes = () => {

    return (

        <Routes>

            <Route
                path="/"
                element={<Navigate to="/login" />}
            />

            <Route
                path="/login"
                element={<Login />}
            />

           <Route
    path="/admin/dashboard"
    element={
        <ProtectedRoute roles={["Admin"]}>
            <Dashboard />
        </ProtectedRoute>
    }
/>

        </Routes>

    );

};

export default AppRoutes;