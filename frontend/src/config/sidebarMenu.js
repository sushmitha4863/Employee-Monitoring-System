import {
    MdDashboard,
    MdSupervisorAccount,
    MdBusiness,
    MdAccessTime,
    MdFolder,
    MdAssessment,
    MdSettings,
    MdPerson
} from "react-icons/md";

export const adminMenu = [

    {
        title: "Dashboard",
        icon: MdDashboard,
        path: "/admin/dashboard"
    },
    {
        title: "Managers",
        icon: MdSupervisorAccount,
        path: "/admin/managers"
    },

    {
        title: "Departments",
        icon: MdBusiness,
        path: "/admin/departments"
    },

    {
        title: "Attendance",
        icon: MdAccessTime,
        path: "/admin/attendance"
    },
    {
        title: "Projects",
        icon: MdFolder,
        path: "/admin/projects"
    },

    {
        title: "Reports",
        icon: MdAssessment,
        path: "/admin/reports"
    },
    {
        title: "Settings",
        icon: MdSettings,
        path: "/admin/settings"
    },

    {
        title: "Profile",
        icon: MdPerson,
        path: "/admin/profile"
    }

];

