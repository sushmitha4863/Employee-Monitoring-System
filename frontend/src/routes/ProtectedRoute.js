import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

const ProtectedRoute = ({ children, roles = [] }) => {

    const {
        user,
        loading,
        isAuthenticated
    } = useAuth();

    if (loading) {

        return <h3>Loading...</h3>;

    }

    if (!isAuthenticated) {

        return <Navigate to="/login" replace />;

    }

    if (
        roles.length > 0 &&
        !roles.includes(user?.role)
    ) {

        return <Navigate to="/login" replace />;

    }

    return children;

};

export default ProtectedRoute;