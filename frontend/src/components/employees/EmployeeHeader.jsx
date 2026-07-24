import "./EmployeeHeader.css";

import { HiOutlineUserPlus } from "react-icons/hi2";

const EmployeeHeader = () => {

    return (

        <div className="employee-header">

            <div>

                <span className="employee-tag">
                    Employee Management
                </span>

                <h1>
                    Employees
                </h1>

                <p>
                    Manage employees, departments, roles and employment information.
                </p>

            </div>

            <button className="add-employee-btn">

                <HiOutlineUserPlus />

                Add Employee

            </button>

        </div>

    );

};

export default EmployeeHeader;