import "./DashboardHeader.css";

import { MdPersonAddAlt1 } from "react-icons/md";

const DashboardHeader = () => {

    return (

        <div className="dashboard-header">

            <div>

                <span className="dashboard-badge">

                    Employee Monitoring System

                </span>

                <h1>

                    Good Morning, Admin 👋

                </h1>

                <p>

                    Welcome back! Here's what's happening in your company today.

                </p>

            </div>

            <button className="add-btn">

                <MdPersonAddAlt1 />

                Add Employee

            </button>

        </div>

    );

};

export default DashboardHeader;