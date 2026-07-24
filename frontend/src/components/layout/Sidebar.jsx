import "./Sidebar.css";
import logo from "../../assets/images/logo.png";

import {
  MdDashboard,
  MdSupervisorAccount,
  MdBusiness,
  MdAccessTime,
  MdFolder,
  MdAssessment,
  MdSettings,
  MdLogout,
  MdCalendarMonth,
  MdNotifications,
  MdHistory,
} from "react-icons/md";

import { NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";

const Sidebar = () => {
  const { logout, user } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  const menu = [
    { name: "Dashboard", icon: <MdDashboard />, path: "/admin/dashboard" },
    { name: "Managers", icon: <MdSupervisorAccount />, path: "/admin/managers" },
    { name: "HR Management", icon: <MdSupervisorAccount />, path: "/admin/hr-management" },
    { name: "Departments", icon: <MdBusiness />, path: "/admin/departments" },
    { name: "Attendance", icon: <MdAccessTime />, path: "/admin/attendance" },
    { name: "Projects", icon: <MdFolder />, path: "/admin/projects" },
    { name: "Company Calendar", icon: <MdCalendarMonth />, path: "/admin/calendars" },
    { name: "Reports", icon: <MdAssessment />, path: "/admin/reports" },
    { name: "Notifications", icon: <MdNotifications />, path: "/admin/notifications" },
    { name: "Audit Logs", icon: <MdHistory />, path: "/admin/audit-logs" },
    { name: "Settings", icon: <MdSettings />, path: "/admin/settings" },
  ];

  return (
    <aside className="sidebar">
      <div>
        <div className="sidebar-logo">
          <img src={logo} alt="Logo" />
          <p>Employee Monitoring System</p>
        </div>
        <nav>
          {menu.map((item) => (
            <NavLink key={item.name} to={item.path} className={({ isActive }) => isActive ? "menu-item active" : "menu-item"}>
              {item.icon}<span>{item.name}</span>
            </NavLink>
          ))}
        </nav>
      </div>
      <div>
        <div className="profile-box">
          <div className="avatar">{user?.fullName ? user.fullName[0] : "A"}</div>
          <div><h5>{user?.fullName || "Administrator"}</h5><span>{user?.role || "Admin"}</span></div>
        </div>
        <button className="logout-btn" onClick={handleLogout}><MdLogout /> Logout</button>
      </div>
    </aside>
  );
};

export default Sidebar;

