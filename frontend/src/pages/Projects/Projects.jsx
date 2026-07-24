import { useCallback, useEffect, useMemo, useState } from "react";
import PageLayout from "../../components/layout/PageLayout";
import AdminModal from "../../components/common/AdminModal";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import { emsApi } from "../../services/emsApi";
import "../SharedAdminPages.css";

const emptyProject = { projectName: "", projectCode: "", description: "", departmentId: "", priority: "Medium", status: "Queued", progressPercentage: 0, startDate: "", endDate: "", managerId: "" };
const pageSize = 8;

const Projects = () => {
  const [projects, setProjects] = useState([]);
  const [managers, setManagers] = useState([]);
  const [departments, setDepartments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [includeArchived, setIncludeArchived] = useState(false);
  const [page, setPage] = useState(1);
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState(emptyProject);

  const load = useCallback(async () => {
    setLoading(true); setError("");
    try {
      const [projectData, managerData, departmentData] = await Promise.all([emsApi.projects({ includeArchived: true }), emsApi.managers(), emsApi.departments()]);
      setProjects(projectData || []);
      setManagers(managerData || []);
      setDepartments((departmentData || []).filter((item) => item.isActive !== false));
    } catch (err) { setError(err.response?.data?.message || "Unable to load projects."); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);
  useEffect(() => { setPage(1); }, [search, status]);

  const filtered = useMemo(() => projects.filter((project) => {
    const query = search.trim().toLowerCase();
    return (includeArchived || !project.isArchived) && (!query || [project.projectName, project.description, project.managerName].some((value) => String(value || "").toLowerCase().includes(query))) && (!status || project.status === status);
  }), [projects, search, status, includeArchived]);

  const rows = filtered.slice((page - 1) * pageSize, page * pageSize);
  const totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));

  const openCreate = () => { setForm(emptyProject); setModal("form"); };
  const openEdit = (project) => { setForm({ ...emptyProject, ...project, startDate: String(project.startDate || "").slice(0, 10), endDate: String(project.endDate || "").slice(0, 10), managerId: project.managerId || "" }); setModal("form"); };

  const submit = async (event) => {
    event.preventDefault();
    if (!form.projectName.trim() || !form.projectCode.trim() || !form.startDate || !form.endDate) { setMessage("Project name, project code, start date and deadline are required."); return; }
    if (new Date(form.endDate) < new Date(form.startDate)) { setMessage("Deadline cannot be before the start date."); return; }
    const payload = { ...form, projectCode: form.projectCode.trim().toUpperCase(), departmentId: form.departmentId ? Number(form.departmentId) : null, managerId: form.managerId ? Number(form.managerId) : null, progressPercentage: Math.max(0, Math.min(100, Number(form.progressPercentage || 0))) };
    try {
      if (form.projectId) await emsApi.updateProject(form.projectId, payload);
      else await emsApi.createProject(payload);
      setModal(null); setMessage(form.projectId ? "Project updated successfully." : "Project created successfully."); await load();
    } catch (err) { setMessage(err.response?.data?.message || "Unable to save project."); }
  };

  const archive = async (project) => {
    if (!window.confirm("Archive this project?")) return;
    try { await emsApi.archiveProject(project.projectId); setMessage("Project archived."); await load(); }
    catch (err) { setMessage(err.response?.data?.message || "Unable to archive project."); }
  };
  return <PageLayout>
    <section className="admin-page-header"><div><span className="admin-page-kicker">Project Tracking</span><h1>Projects</h1><p>Create projects, assign managers and monitor delivery progress. Employee assignment remains a Manager responsibility.</p></div><button className="admin-primary-btn" onClick={openCreate}>Create Project</button></section>
    {message && <p className="admin-inline-message">{message}</p>}
    <div className="admin-toolbar"><input placeholder="Search projects" value={search} onChange={(event) => setSearch(event.target.value)} /><label className="admin-field" style={{ display: "flex", alignItems: "center", gap: 8 }}><input type="checkbox" checked={includeArchived} onChange={(event) => setIncludeArchived(event.target.checked)} /> Include archived</label><select value={status} onChange={(event) => setStatus(event.target.value)}><option value="">All Status</option><option>Queued</option><option>Ongoing</option><option>Review</option><option>Completed</option><option>At Risk</option><option>On Hold</option></select></div>
    {loading && <LoadingState message="Loading projects..." />}
    {error && <ErrorState message={error} onRetry={load} />}
    {!loading && !error && filtered.length === 0 && <EmptyState message="No projects match the current filters." />}
    {!loading && !error && filtered.length > 0 && <section className="admin-card"><div className="admin-card-header"><h3>Project Register</h3><span className="admin-muted">{filtered.length} projects</span></div><table className="admin-table"><thead><tr><th>Project</th><th>Manager</th><th>Progress</th><th>Status</th><th>Start</th><th>Deadline</th><th>Actions</th></tr></thead><tbody>{rows.map((project) => <tr key={project.projectId}><td><strong>{project.projectName}</strong><p className="admin-muted">{project.description || "No description"}</p></td><td>{project.managerName || "Not assigned"}</td><td><div className="admin-progress"><span style={{ width: (project.progressPercentage || 0) + "%" }} /></div><small>{project.progressPercentage || 0}%</small></td><td><span className={"admin-badge " + (project.status === "Completed" ? "success" : project.status === "At Risk" ? "danger" : "warning")}>{project.status}</span></td><td>{String(project.startDate || "-").slice(0, 10)}</td><td>{String(project.endDate || "-").slice(0, 10)}</td><td><div className="admin-actions"><button className="admin-action-btn" onClick={() => openEdit(project)}>Edit</button><button className="admin-action-btn" onClick={() => project.isArchived ? emsApi.restoreProject(project.projectId).then(() => { setMessage("Project restored."); load(); }) : archive(project)}>{project.isArchived ? "Restore" : "Archive"}</button></div></td></tr>)}</tbody></table><div className="admin-pagination"><span>Showing {(page - 1) * pageSize + 1}-{Math.min(page * pageSize, filtered.length)} of {filtered.length}</span><div><button disabled={page === 1} onClick={() => setPage(page - 1)}>Previous</button><button disabled={page === totalPages} onClick={() => setPage(page + 1)}>Next</button></div></div></section>}
    {modal === "form" && <AdminModal title={form.projectId ? "Edit Project" : "Create Project"} onClose={() => setModal(null)}><form onSubmit={submit}><div className="admin-form-grid"><div className="admin-field"><label>Project name</label><input value={form.projectName} onChange={(event) => setForm({ ...form, projectName: event.target.value })} required /></div><div className="admin-field"><label>Project code</label><input value={form.projectCode} onChange={(event) => setForm({ ...form, projectCode: event.target.value.toUpperCase() })} required /></div><div className="admin-field"><label>Department</label><select value={form.departmentId || ""} onChange={(event) => setForm({ ...form, departmentId: event.target.value })}><option value="">No department</option>{departments.map((item) => <option key={item.departmentId || item.id} value={item.departmentId || item.id}>{item.departmentName || item.name}</option>)}</select></div><div className="admin-field"><label>Manager</label><select value={form.managerId || ""} onChange={(event) => setForm({ ...form, managerId: event.target.value })}><option value="">Select manager</option>{managers.map((manager) => <option key={manager.userId} value={manager.userId}>{manager.fullName}</option>)}</select></div><div className="admin-field"><label>Priority</label><select value={form.priority} onChange={(event) => setForm({ ...form, priority: event.target.value })}><option>Low</option><option>Medium</option><option>High</option><option>Critical</option></select></div><div className="admin-field"><label>Status</label><select value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}><option>Queued</option><option>Ongoing</option><option>Review</option><option>Completed</option><option>At Risk</option><option>On Hold</option></select></div><div className="admin-field"><label>Progress (%)</label><input type="number" min="0" max="100" value={form.progressPercentage} onChange={(event) => setForm({ ...form, progressPercentage: event.target.value })} /></div><div className="admin-field"><label>Start date</label><input type="date" value={form.startDate} onChange={(event) => setForm({ ...form, startDate: event.target.value })} required /></div><div className="admin-field"><label>Deadline</label><input type="date" value={form.endDate} onChange={(event) => setForm({ ...form, endDate: event.target.value })} required /></div><div className="admin-field" style={{ gridColumn: "1 / -1" }}><label>Description</label><textarea value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></div></div><button className="admin-primary-btn" style={{ marginTop: 18 }}>Save Project</button></form></AdminModal>}
  </PageLayout>;
};

export default Projects;
