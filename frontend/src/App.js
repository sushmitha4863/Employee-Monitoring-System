import { Routes, Route, Navigate } from "react-router-dom";

import Login from "./pages/Login/Login";
import Dashboard from "./pages/Dashboard/Dashboard";
import Managers from "./pages/Managers/Managers";
import HrManagement from "./pages/HrManagement/HrManagement";
import Departments from "./pages/Departments/Departments";
import Attendance from "./pages/Attendance/Attendance";
import Projects from "./pages/Projects/Projects";
import Reports from "./pages/Reports/Reports";
import Settings from "./pages/Settings/Settings";
import Notifications from "./pages/Notifications/Notifications";
import AuditLogs from "./pages/AuditLogs/AuditLogs";
import ProtectedRoute from "./routes/ProtectedRoute";
import PeopleCalendars from "./pages/AdminMonitoring/PeopleCalendars";
import EmployeeDashboard from "./pages/EmployeePanel/EmployeeDashboard";
import EmployeeProjects from "./pages/EmployeePanel/EmployeeProjects";
import EmployeeAttendance from "./pages/EmployeePanel/EmployeeAttendance";
import EmployeeTimesheet from "./pages/EmployeePanel/EmployeeTimesheet";
import EmployeeCalendar from "./pages/EmployeePanel/EmployeeCalendar";
import EmployeePerformance from "./pages/EmployeePanel/EmployeePerformance";
import EmployeeSettings from "./pages/EmployeePanel/EmployeeSettings";
import { HRDashboard, HRPage } from "./pages/RolePanels/HrModule";
import { ManagerDashboard, ManagerPage } from "./pages/RolePanels/ManagerModule";

function App() {
    return (
        <Routes>
            <Route path="/" element={<Navigate to="/login" />} />
            <Route path="/login" element={<Login />} />
            <Route path="/dashboard" element={<Navigate to="/admin/dashboard" />} />
            <Route path="/employees" element={<Navigate to="/login" replace />} />

            <Route path="/admin/dashboard" element={<ProtectedRoute roles={['Admin']}><Dashboard /></ProtectedRoute>} />            <Route path="/admin/managers" element={<ProtectedRoute roles={['Admin']}><Managers /></ProtectedRoute>} />
            <Route path="/admin/hr-management" element={<ProtectedRoute roles={['Admin']}><HrManagement /></ProtectedRoute>} />
            <Route path="/admin/departments" element={<ProtectedRoute roles={['Admin']}><Departments /></ProtectedRoute>} />
            <Route path="/admin/attendance" element={<ProtectedRoute roles={['Admin']}><Attendance /></ProtectedRoute>} />            <Route path="/admin/projects" element={<ProtectedRoute roles={['Admin']}><Projects /></ProtectedRoute>} />
            <Route path="/admin/calendars" element={<ProtectedRoute roles={['Admin']}><PeopleCalendars /></ProtectedRoute>} />            <Route path="/admin/reports" element={<ProtectedRoute roles={['Admin']}><Reports /></ProtectedRoute>} />            <Route path="/admin/notifications" element={<ProtectedRoute roles={['Admin']}><Notifications /></ProtectedRoute>} />
            <Route path="/admin/audit-logs" element={<ProtectedRoute roles={['Admin']}><AuditLogs /></ProtectedRoute>} />
            <Route path="/admin/settings" element={<ProtectedRoute roles={['Admin']}><Settings /></ProtectedRoute>} />            <Route path="/admin/profile" element={<Navigate to="/admin/settings" replace />} />

            <Route path="/employee/dashboard" element={<ProtectedRoute roles={['Employee']}><EmployeeDashboard /></ProtectedRoute>} />
            <Route path="/employee/projects" element={<ProtectedRoute roles={['Employee']}><EmployeeProjects /></ProtectedRoute>} />
            <Route path="/employee/attendance" element={<ProtectedRoute roles={['Employee']}><EmployeeAttendance /></ProtectedRoute>} />
            <Route path="/employee/timesheet" element={<ProtectedRoute roles={['Employee']}><EmployeeTimesheet /></ProtectedRoute>} />
            <Route path="/employee/calendar" element={<ProtectedRoute roles={['Employee']}><EmployeeCalendar /></ProtectedRoute>} />
            <Route path="/employee/performance" element={<ProtectedRoute roles={['Employee']}><EmployeePerformance /></ProtectedRoute>} />
            <Route path="/employee/settings" element={<ProtectedRoute roles={['Employee']}><EmployeeSettings /></ProtectedRoute>} />
            <Route path="/employee/profile" element={<Navigate to="/employee/settings#my-profile" replace />} />
            <Route path="/employee/tasks" element={<Navigate to="/employee/projects" replace />} />
            <Route path="/employee/leaves" element={<Navigate to="/employee/attendance" replace />} />
            <Route path="/employee/documents" element={<Navigate to="/employee/dashboard" replace />} />
            <Route path="/employee/reports" element={<Navigate to="/employee/dashboard" replace />} />
            <Route path="/employee/announcements" element={<Navigate to="/employee/dashboard" replace />} />

            <Route path="/manager/dashboard" element={<ProtectedRoute roles={['Manager']}><ManagerDashboard /></ProtectedRoute>} />
            <Route path="/manager/team" element={<ProtectedRoute roles={['Manager']}><ManagerPage title="My Team" /></ProtectedRoute>} />
            <Route path="/manager/projects" element={<ProtectedRoute roles={['Manager']}><ManagerPage title="Projects & Tasks" /></ProtectedRoute>} />
            <Route path="/manager/attendance" element={<ProtectedRoute roles={['Manager']}><ManagerPage title="Team Attendance" /></ProtectedRoute>} />
            <Route path="/manager/leaves" element={<ProtectedRoute roles={['Manager']}><ManagerPage title="Leave Requests" /></ProtectedRoute>} />
            <Route path="/manager/timesheets" element={<ProtectedRoute roles={['Manager']}><ManagerPage title="Timesheets" /></ProtectedRoute>} />
            <Route path="/manager/performance" element={<ProtectedRoute roles={['Manager']}><ManagerPage title="Performance" /></ProtectedRoute>} />
            <Route path="/manager/calendar" element={<ProtectedRoute roles={['Manager']}><ManagerPage title="Calendar" /></ProtectedRoute>} />
            <Route path="/manager/reports" element={<ProtectedRoute roles={['Manager']}><ManagerPage title="Reports" /></ProtectedRoute>} />
            <Route path="/manager/settings" element={<ProtectedRoute roles={['Manager']}><ManagerPage title="Settings" /></ProtectedRoute>} />
            <Route path="/manager/tasks" element={<Navigate to="/manager/projects" replace />} />
            <Route path="/manager/meetings" element={<Navigate to="/manager/calendar" replace />} />
            <Route path="/manager/announcements" element={<Navigate to="/manager/calendar" replace />} />
            <Route path="/manager/profile" element={<Navigate to="/manager/settings#my-profile" replace />} />
            <Route path="/manager/documents" element={<Navigate to="/manager/dashboard" replace />} />
            <Route path="/manager/notifications" element={<Navigate to="/manager/dashboard" replace />} />

            <Route path="/hr/dashboard" element={<ProtectedRoute roles={['HR']}><HRDashboard /></ProtectedRoute>} />
            <Route path="/hr/employees" element={<ProtectedRoute roles={['HR']}><HRPage title="Employees" /></ProtectedRoute>} />
            <Route path="/hr/managers" element={<ProtectedRoute roles={['HR']}><HRPage title="Managers" /></ProtectedRoute>} />
            <Route path="/hr/recruitment" element={<ProtectedRoute roles={['HR']}><HRPage title="Recruitment" /></ProtectedRoute>} />
            <Route path="/hr/lifecycle" element={<ProtectedRoute roles={['HR']}><HRPage title="Onboarding & Offboarding" /></ProtectedRoute>} />
            <Route path="/hr/attendance-leave" element={<ProtectedRoute roles={['HR']}><HRPage title="Attendance & Leave" /></ProtectedRoute>} />
            <Route path="/hr/training" element={<ProtectedRoute roles={['HR']}><HRPage title="Training" /></ProtectedRoute>} />
            <Route path="/hr/performance" element={<ProtectedRoute roles={['HR']}><HRPage title="Performance" /></ProtectedRoute>} />
            <Route path="/hr/reports" element={<ProtectedRoute roles={['HR']}><HRPage title="Reports" /></ProtectedRoute>} />
            <Route path="/hr/settings" element={<ProtectedRoute roles={['HR']}><HRPage title="Settings" /></ProtectedRoute>} />
            <Route path="/hr/profile" element={<Navigate to="/hr/settings#my-profile" replace />} />
            <Route path="/hr/attendance" element={<Navigate to="/hr/attendance-leave" replace />} />
            <Route path="/hr/leaves" element={<Navigate to="/hr/attendance-leave" replace />} />
            <Route path="/hr/applicants" element={<Navigate to="/hr/recruitment" replace />} />
            <Route path="/hr/interviews" element={<Navigate to="/hr/recruitment" replace />} />
            <Route path="/hr/onboarding" element={<Navigate to="/hr/lifecycle" replace />} />
            <Route path="/hr/offboarding" element={<Navigate to="/hr/lifecycle" replace />} />
            <Route path="/hr/payroll" element={<Navigate to="/hr/dashboard" replace />} />
            <Route path="/hr/calendar" element={<Navigate to="/hr/dashboard" replace />} />
            <Route path="/hr/documents" element={<Navigate to="/hr/dashboard" replace />} />
            <Route path="/hr/compliance" element={<Navigate to="/hr/dashboard" replace />} />
            <Route path="/hr/announcements" element={<Navigate to="/hr/dashboard" replace />} />
            <Route path="/hr/organization" element={<Navigate to="/hr/dashboard" replace />} />
            <Route path="/hr/policies" element={<Navigate to="/hr/dashboard" replace />} />
            <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
    );
}

export default App;




