import { useEffect, useState } from "react";
import { emsApi } from "../../services/emsApi";
import "./RecentEmployees.css";

const RecentEmployees = () => {
  const [employees, setEmployees] = useState([]);

  useEffect(() => {
    loadEmployees();
  }, []);

  const loadEmployees = async () => {
    const data = await emsApi.employees();

    if (Array.isArray(data)) {
      setEmployees(data.slice(0, 5));
    }
  };

  return (
    <div className="recent-card">
      <div className="recent-header">
        <h4>Recent Employees</h4>

        <button>View All</button>
      </div>

      <table>
        <thead>
          <tr>
            <th>Employee</th>
            <th>Department</th>
            <th>Role</th>
            <th>Status</th>
          </tr>
        </thead>

        <tbody>
          {employees.map((employee) => (
            <tr key={employee.id}>
              <td>
                <div className="employee-info">
                  <div className="avatar">
                    {(employee.fullName || employee.name || "?").charAt(0)}
                  </div>

                  <span>{employee.fullName || employee.name}</span>
                </div>
              </td>

              <td>{employee.departmentName || employee.department}</td>

              <td>{employee.designation || employee.role}</td>

              <td>
                <span
                  className={`status ${
                    (employee.status || "Present").toLowerCase()
                  }`}
                >
                  {employee.status || "Present"}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default RecentEmployees;