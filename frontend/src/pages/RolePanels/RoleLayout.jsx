import { useState } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { MdDashboard, MdPeople, MdAccessTime, MdTaskAlt, MdFolder, MdTrendingUp, MdEventNote, MdAssessment, MdCalendarMonth, MdDescription, MdCampaign, MdSettings, MdSearch, MdLogout, MdWork, MdPayments, MdVerifiedUser, MdPerson } from "react-icons/md";
import NotificationBell from "../../components/common/NotificationBell";
import ThemeToggle from "../../components/common/ThemeToggle";
import "./RolePanel.css";

const iconMap = { dashboard: MdDashboard, team: MdPeople, attendance: MdAccessTime, tasks: MdTaskAlt, projects: MdFolder, performance: MdTrendingUp, leaves: MdEventNote, reports: MdAssessment, calendar: MdCalendarMonth, documents: MdDescription, announcements: MdCampaign, settings: MdSettings, jobs: MdWork, payroll: MdPayments, compliance: MdVerifiedUser, MdPerson };

const RoleLayout = ({ role = "Manager", base = "/manager", menu = [], children }) => {
    const { logout, user } = useAuth();
    const navigate = useNavigate();
    const [accountOpen, setAccountOpen] = useState(false);
    const name = user?.fullName || user?.username || role;

    const handleLogout = () => {
        logout();
        navigate("/login");
    };

    return (
        <div className="role-shell">
            <aside className="role-sidebar">
                <div>
                    <div className="role-logo role-brand"><span>EMS</span><strong>Employee Monitoring System</strong></div>
                    <div className="role-label">{role.toUpperCase()} PANEL</div>
                    <nav className="role-menu">
                        {menu.map((item) => {
                            const Icon = iconMap[item.icon];
                            return <NavLink key={item.path} to={`${base}${item.path}`}><Icon /> {item.label}</NavLink>;
                        })}
                    </nav>
                </div>
                <div>
                    <div className="employee-mini-profile"><div className="role-avatar">{name[0]}</div><div><h4>{name}</h4><p>{role}</p><div className="employee-online">● Online</div></div></div>
                    <button className="logout-btn" onClick={handleLogout} style={{ marginTop: 14 }}><MdLogout /> Logout</button>
                </div>
            </aside>
            <main className="role-main">
                <header className="role-topbar">
                    <form className="role-search" onSubmit={(event) => event.preventDefault()}><MdSearch /><input aria-label="Search manager portal" /></form>
                    <div className="employee-top-actions">
                        <ThemeToggle />
                        <NotificationBell /><div className="employee-account"><button className="employee-account-trigger" aria-label="Open account menu" aria-expanded={accountOpen} onClick={() => setAccountOpen(!accountOpen)}><div className="role-profile"><div className="role-avatar">{name[0]}</div><div><strong>{name}</strong><p className="role-muted">{role}</p></div></div></button>{accountOpen&&<div className="employee-account-menu"><div className="employee-account-summary"><strong>{name}</strong><span>{role}</span></div><button onClick={()=>{setAccountOpen(false);navigate(`${base}/settings#my-profile`)}}><MdPerson/> My Profile</button><button onClick={()=>{setAccountOpen(false);navigate(`${base}/settings`)}}><MdSettings/> Settings</button><button onClick={handleLogout}><MdLogout/> Logout</button></div>}</div>
                    </div>
                </header>
                <div className="role-page">{children}</div>
            </main>
        </div>
    );
};

export default RoleLayout;
