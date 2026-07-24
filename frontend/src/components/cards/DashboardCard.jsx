import React from "react";
import "./DashboardCard.css";

const DashboardCard = ({
    title,
    value,
    icon,
    color
}) => {

    return (

        <div className="dashboard-card">

            <div
                className="card-icon"
                style={{
                    background: color
                }}
            >
                {icon}
            </div>

            <div className="card-info">

                <h6>{title}</h6>

                <h2>{value}</h2>

            </div>

        </div>

    );

};

export default DashboardCard;