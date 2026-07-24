import { useEffect, useState } from "react";
import {
    MdPeople,
    MdAccessTime,
    MdSupervisorAccount,
    MdFolder
} from "react-icons/md";

import StatCard from "./StatCard";
import { emsApi } from "../../services/emsApi";

const StatsGrid = () => {
    const [summary, setSummary] = useState(null);

    useEffect(() => {
        let mounted = true;
        emsApi.dashboardSummary().then((data) => {
            if (mounted) setSummary(data);
        });
        return () => {
            mounted = false;
        };
    }, []);

    const cards = [
        {
            title: "Total Employees",
            value: summary?.totalEmployees ?? 98,
            subtitle: "Live employee count",
            icon: <MdPeople />,
            color: "#3B82F6"
        },
        {
            title: "Present Today",
            value: summary?.presentEmployees ?? 91,
            subtitle: `${summary?.absentEmployees ?? 7} absent`,
            icon: <MdAccessTime />,
            color: "#10B981"
        },
        {
            title: "Managers",
            value: summary?.totalManagers ?? 12,
            subtitle: "Leadership team",
            icon: <MdSupervisorAccount />,
            color: "#F59E0B"
        },
        {
            title: "Ongoing Projects",
            value: summary?.ongoingProjects ?? 7,
            subtitle: `${summary?.pendingLeaves ?? 3} pending leaves`,
            icon: <MdFolder />,
            color: "#22C55E"
        }
    ];

    return (
        <div className="row g-4">
            {cards.map((card, index) => (
                <div key={index} className="col-xl-3 col-lg-6 col-md-6">
                    <StatCard {...card} />
                </div>
            ))}
        </div>
    );
};

export default StatsGrid;