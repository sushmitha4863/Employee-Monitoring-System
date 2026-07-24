import { useCallback, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import PageLayout from "../../components/layout/PageLayout";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import useEmsResource from "../../hooks/useEmsResource";
import { emsApi } from "../../services/emsApi";
import { saveBlob } from "../../utils/adminUtils";
import "../SharedAdminPages.css";

const scoreClass = (score) => score >= 90 ? "success" : score >= 75 ? "info" : "warning";

const EmployeeEffectiveness = () => {
  const navigate = useNavigate();
  const load = useCallback(() => emsApi.employeeEffectiveness(), []);
  const { data, loading, error } = useEmsResource(load);
  const [exporting, setExporting] = useState(false);
  const [message, setMessage] = useState("");
  const employees = useMemo(() => Array.isArray(data) ? data : [], [data]);
  const summary = useMemo(() => {
    const avg = employees.length ? Math.round(employees.reduce((sum, item) => sum + (item.effectivenessScore || item.score || 0), 0) / employees.length) : 0;
    return { avg, high: employees.filter((item) => (item.effectivenessScore || item.score || 0) >= 90).length, support: employees.filter((item) => (item.effectivenessScore || item.score || 0) < 70).length };
  }, [employees]);

  const exportEffectiveness = async () => {
    setExporting(true);
    setMessage("");
    try {
      const response = await emsApi.exportReport("performance", "excel");
      saveBlob(response.data, "performance-report.xlsx");
    } catch (err) {
      setMessage(err.response?.data?.message || "Unable to export effectiveness report.");
    } finally {
      setExporting(false);
    }
  };

  return (
    <PageLayout>
      <section className="admin-page-header">
        <div>
          <span className="admin-page-kicker">People Analytics</span>
          <h1>Employee Effectiveness</h1>
          <p>Measure effectiveness using attendance, working hours, late logins, tasks, projects and leave usage.</p>
        </div>
        <button className="admin-primary-btn" disabled={exporting} onClick={exportEffectiveness}>Generate Effectiveness Report</button>
      </section>
      {message && <p className="admin-inline-message">{message}</p>}
      <section className="admin-stats-grid"><div className="admin-stat-card"><p className="admin-stat-label">Avg Effectiveness</p><h3>{summary.avg}%</h3><span>Calculated score</span></div><div className="admin-stat-card"><p className="admin-stat-label">High Performers</p><h3>{summary.high}</h3><span>Score above 90%</span></div><div className="admin-stat-card"><p className="admin-stat-label">Needs Support</p><h3>{summary.support}</h3><span>Follow-up required</span></div><div className="admin-stat-card"><p className="admin-stat-label">Employees</p><h3>{employees.length}</h3><span>Report API</span></div></section>
      {loading && <LoadingState message="Loading effectiveness scores..." />}
      {error && <ErrorState message={error} />}
      {!loading && !error && employees.length === 0 && <EmptyState message="No effectiveness records found." />}
      {!loading && !error && employees.length > 0 && <section className="admin-card"><div className="admin-card-header"><h3>Effectiveness Matrix</h3><button className="admin-soft-btn" onClick={() => navigate("/admin/settings")}>Tune Score Weights</button></div><table className="admin-table"><thead><tr><th>Employee</th><th>Attendance</th><th>Hours</th><th>Late</th><th>Tasks</th><th>Projects</th><th>Leaves</th><th>Score</th></tr></thead><tbody>{employees.map(emp => { const score = emp.effectivenessScore || emp.score || 0; return <tr key={emp.employeeId || emp.userId || emp.email}><td><div className="admin-person"><div className="admin-avatar">{(emp.employeeName || emp.fullName || "U")[0]}</div><div><h4>{emp.employeeName || emp.fullName}</h4><span>{emp.role || emp.designation || "Employee"}</span></div></div></td><td>{emp.attendancePercentage || emp.attendance || 0}%</td><td>{emp.workingHours || emp.averageWorkingHours || 0}</td><td>{emp.lateLoginCount || 0}</td><td>{emp.taskCompletionPercentage || emp.taskCompletion || 0}%</td><td>{emp.projectCompletionPercentage || emp.projectCompletion || 0}%</td><td>{emp.leaveCount || 0}</td><td><span className={`admin-badge ${scoreClass(score)}`}>{score}%</span></td></tr>; })}</tbody></table></section>}
    </PageLayout>
  );
};

export default EmployeeEffectiveness;
