import { useEffect, useState } from "react";
import "./EmployeeTable.css";

import {
  HiOutlineEye,
  HiOutlinePencilSquare,
  HiOutlineTrash,
} from "react-icons/hi2";

import { emsApi } from "../../services/emsApi";

const EmployeeTable = () => {
  const [employees, setEmployees] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadEmployees();
  }, []);

  const loadEmployees = async () => {
    try {
      const data = await emsApi.employees();
      setEmployees(data || []);
    } catch (error) {
      console.error("Failed to load employees:", error);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="employee-table-card">
        <div style={{ padding: "40px", textAlign: "center" }}>
          Loading employees...
        </div>
      </div>
    );
  }

  return (
    <div className="employee-table-card">
      <table>
        <thead>
          <tr>
            <th>Employee</th>
            <th>Department</th>
            <th>Role</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>

        <tbody>
          {employees.length === 0 ? (
            <tr>
              <td
                colSpan="5"
                style={{
                  textAlign: "center",
                  padding: "40px",
                }}
              >
                No employees found.
              </td>
            </tr>
          ) : (
            employees.map((employee) => (
              <tr key={employee.id}>
                <td>
                  <div className="employee-info">
                    <div className="employee-avatar">
                      {(employee.fullName ||
                        employee.name ||
                        employee.username ||
                        "?")
                        .charAt(0)
                        .toUpperCase()}
                    </div>

                    <div>
                      <h6>
                        {employee.fullName ||
                          employee.name ||
                          employee.username}
                      </h6>

                      <span>{employee.email}</span>
                    </div>
                  </div>
                </td>

                <td>
                  {employee.departmentName ||
                    employee.department ||
                    "-"}
                </td>

                <td>
                  {employee.designation ||
                    employee.role ||
                    "-"}
                </td>

                <td>
                  <span
                    className={
                      employee.isActive === false
                        ? "badge-inactive"
                        : "badge-active"
                    }
                  >
                    {employee.isActive === false
                      ? "Inactive"
                      : "Active"}
                  </span>
                </td>

                <td>
                  <div className="table-actions">
                    <button title="View">
                      <HiOutlineEye />
                    </button>

                    <button title="Edit">
                      <HiOutlinePencilSquare />
                    </button>

                    <button
                      className="delete-btn"
                      title="Delete"
                    >
                      <HiOutlineTrash />
                    </button>
                  </div>
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
};

export default EmployeeTable;