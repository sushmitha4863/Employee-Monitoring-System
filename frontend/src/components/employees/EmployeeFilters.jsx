import "./EmployeeFilters.css";

import { HiOutlineMagnifyingGlass } from "react-icons/hi2";

const EmployeeFilters = () => {

    return (

        <div className="employee-filters">

            <div className="search-box">

                <HiOutlineMagnifyingGlass className="search-icon"/>

                <input
                    type="text"
                    placeholder="Search employee..."
                />

            </div>

            <select>

                <option>All Departments</option>

                <option>Development</option>

                <option>HR</option>

                <option>Accounts</option>

                <option>Testing</option>

            </select>

            <select>

                <option>All Roles</option>

                <option>Employee</option>

                <option>Manager</option>

                <option>HR</option>

                <option>Admin</option>

            </select>

            <select>

                <option>All Status</option>

                <option>Active</option>

                <option>Inactive</option>

            </select>

        </div>

    );

};

export default EmployeeFilters;