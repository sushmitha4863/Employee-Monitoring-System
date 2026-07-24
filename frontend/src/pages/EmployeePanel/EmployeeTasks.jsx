import { useCallback, useMemo, useState } from "react";
import { EmployeeLayout, EmployeeHero } from "./EmployeeWidgets";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import useEmsResource from "../../hooks/useEmsResource";
import { emsApi } from "../../services/emsApi";

const statusClass = (status) => status === "Completed" ? "success" : status === "Pending" ? "info" : status === "Blocked" ? "danger" : "warning";

const EmployeeTasks = () => {
  const loadTasks = useCallback(() => emsApi.myTasks(), []);
  const { data, setData, loading, error, reload } = useEmsResource(loadTasks);
  const [editing, setEditing] = useState(null);
  const [savingId, setSavingId] = useState(null);
  const [message, setMessage] = useState("");
  const tasks = useMemo(() => Array.isArray(data) ? data : [], [data]);
  const stats = useMemo(() => ({ total: tasks.length, completed: tasks.filter((task) => task.status === "Completed").length, inProgress: tasks.filter((task) => task.status === "In Progress").length, pending: tasks.filter((task) => task.status === "Pending").length, blocked: tasks.filter((task) => task.status === "Blocked").length }), [tasks]);

  const updateProgress = async (task) => {
    const id = task.taskId || task.id;
    const draft = editing || task;
    setSavingId(id);
    setMessage("");
    try {
      await emsApi.updateTaskProgress(id, { status: draft.status, progressPercentage: Number(draft.progressPercentage || 0), remarks: draft.employeeRemarks || "" });
      setData((items) => items.map((item) => (item.taskId || item.id) === id ? { ...item, ...draft } : item));
      setEditing(null);
      setMessage("Task updated.");
    } catch (err) {
      setMessage(err.response?.data?.message || "Unable to update task.");
    } finally {
      setSavingId(null);
    }
  };

  const upload = async (task, file) => {
    if (!file) return;
    const id = task.taskId || task.id;
    setSavingId(id);
    try {
      await emsApi.uploadTaskAttachment(id, file);
      setMessage("Attachment uploaded.");
      await reload();
    } catch (err) {
      setMessage(err.response?.data?.message || "Unable to upload attachment.");
    } finally {
      setSavingId(null);
    }
  };

  return (
    <EmployeeLayout>
      <EmployeeHero title="My Tasks" subtitle="Prioritize assigned work, update progress, add remarks and upload proof of work." />
      {message && <p className="admin-inline-message">{message}</p>}
      <section className="employee-stats"><div className="employee-card employee-stat"><div><p className="employee-stat-label">Total Tasks</p><h3>{stats.total}</h3><small>Assigned</small></div></div><div className="employee-card employee-stat"><div><p className="employee-stat-label">Completed</p><h3>{stats.completed}</h3><small>{stats.total ? Math.round((stats.completed / stats.total) * 100) : 0}%</small></div></div><div className="employee-card employee-stat"><div><p className="employee-stat-label">In Progress</p><h3>{stats.inProgress}</h3><small>Active</small></div></div><div className="employee-card employee-stat"><div><p className="employee-stat-label">Pending</p><h3>{stats.pending}</h3><small>Upcoming</small></div></div><div className="employee-card employee-stat"><div><p className="employee-stat-label">Blocked</p><h3>{stats.blocked}</h3><small>Needs help</small></div></div></section>
      {loading && <LoadingState message="Loading assigned tasks..." />}
      {error && <ErrorState message={error} />}
      {!loading && !error && tasks.length === 0 && <EmptyState message="No tasks assigned yet." />}
      {!loading && !error && tasks.length > 0 && <section className="employee-card employee-section"><div className="employee-section-header"><h2>Task Board</h2></div><table className="employee-table"><thead><tr><th>Task</th><th>Due</th><th>Status</th><th>Progress</th><th>Remarks</th><th>Attachment</th><th>Action</th></tr></thead><tbody>{tasks.map(task => { const id = task.taskId || task.id; const isEditing = editing?.taskId === id; const draft = isEditing ? editing : task; return <tr key={id}><td><strong>{task.title || task.taskName}</strong><p className="employee-stat-label">{task.projectName || "General"} | {task.priority || "Normal"}</p></td><td>{String(task.dueDate || "-").slice(0, 10)}</td><td><select value={draft.status || "Pending"} onChange={(e) => setEditing({ ...draft, taskId: id, status: e.target.value })}><option>Pending</option><option>In Progress</option><option>Blocked</option><option>Completed</option></select></td><td><input style={{ width: 76 }} type="number" min="0" max="100" value={draft.progressPercentage || 0} onChange={(e) => setEditing({ ...draft, taskId: id, progressPercentage: e.target.value })} />%</td><td><input value={draft.employeeRemarks || ""} onChange={(e) => setEditing({ ...draft, taskId: id, employeeRemarks: e.target.value })} /></td><td><input type="file" onChange={(e) => upload(task, e.target.files?.[0])} disabled={savingId === id} /><small>{task.attachmentCount || 0} file(s)</small></td><td><button className="employee-link" disabled={savingId === id} onClick={() => updateProgress(task)}>Save</button><span className={`employee-badge ${statusClass(task.status)}`}>{task.status || "Pending"}</span></td></tr>; })}</tbody></table></section>}
    </EmployeeLayout>
  );
};

export default EmployeeTasks;
