import { useCallback, useEffect, useMemo, useState } from "react";
import PageLayout from "../../components/layout/PageLayout";
import AdminModal from "../../components/common/AdminModal";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import { emsApi } from "../../services/emsApi";
import "../SharedAdminPages.css";

const pageSize = 10;

const Managers = () => {
  const [managers, setManagers] = useState([]);
  const [departments, setDepartments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [search, setSearch] = useState("");
  const [department, setDepartment] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState(null);

  const load = useCallback(async () => {
    setLoading(true); setError("");
    try {
      const [managerData, departmentData] = await Promise.all([emsApi.managers(), emsApi.departments()]);
      setManagers(managerData || []);
      setDepartments(departmentData || []);
    } catch (err) { setError(err.response?.data?.message || "Unable to load managers."); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);
  useEffect(() => { setPage(1); }, [search, department, status]);

  const filtered = useMemo(() => managers.filter((manager) => {
    const query = search.trim().toLowerCase();
    const matchesSearch = !query || [manager.fullName, manager.email, manager.employeeCode, manager.department]
      .some((value) => String(value || "").toLowerCase().includes(query));
    const matchesDepartment = !department || String(manager.departmentId || "") === department;
    const matchesStatus = !status || (status === "Active" ? manager.isActive !== false : manager.isActive === false);
    return matchesSearch && matchesDepartment && matchesStatus;
  }), [managers, search, department, status]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));
  const rows = filtered.slice((page - 1) * pageSize, page * pageSize);

  return <PageLayout>
    <section className="admin-page-header"><div><span className="admin-page-kicker">Read-only system visibility</span><h1>Managers</h1><p>View manager accounts, department ownership and assigned project context.</p></div></section>
    <div className="admin-toolbar"><input placeholder="Search managers" value={search} onChange={(event) => setSearch(event.target.value)} /><select value={department} onChange={(event) => setDepartment(event.target.value)}><option value="">All Departments</option>{departments.map((item) => <option key={item.departmentId || item.id} value={item.departmentId || item.id}>{item.departmentName || item.name}</option>)}</select><select value={status} onChange={(event) => setStatus(event.target.value)}><option value="">All Statuses</option><option>Active</option><option>Inactive</option></select></div>
    {loading && <LoadingState message="Loading managers..." />}
    {error && <ErrorState message={error} onRetry={load} />}
    {!loading && !error && filtered.length === 0 && <EmptyState message="No managers match the current filters." />}
    {!loading && !error && filtered.length > 0 && <section className="admin-card"><div className="admin-card-header"><h3>Manager Register</h3><span className="admin-muted">{filtered.length} managers</span></div><table className="admin-table"><thead><tr><th>Manager</th><th>Code</th><th>Official Email</th><th>Department</th><th>Status</th><th>Action</th></tr></thead><tbody>{rows.map((manager) => <tr key={manager.userId}><td><div className="admin-person"><div className="admin-avatar">{manager.fullName?.[0] || "M"}</div><div><h4>{manager.fullName}</h4><span>{manager.designation || "Manager"}</span></div></div></td><td>{manager.employeeCode || "-"}</td><td>{manager.email}</td><td>{manager.department || "-"}</td><td><span className={"admin-badge " + (manager.isActive === false ? "danger" : "success")}>{manager.isActive === false ? "Inactive" : "Active"}</span></td><td><button className="admin-action-btn" onClick={() => setSelected(manager)}>View details</button></td></tr>)}</tbody></table><div className="admin-pagination"><span>Showing {(page - 1) * pageSize + 1}-{Math.min(page * pageSize, filtered.length)} of {filtered.length}</span><div><button disabled={page === 1} onClick={() => setPage(page - 1)}>Previous</button><button disabled={page === totalPages} onClick={() => setPage(page + 1)}>Next</button></div></div></section>}
    {selected && <AdminModal title={selected.fullName + " details"} onClose={() => setSelected(null)}><div className="admin-list"><div className="admin-list-item"><span>Official email</span><strong>{selected.email}</strong></div><div className="admin-list-item"><span>Manager code</span><strong>{selected.employeeCode || "Not assigned"}</strong></div><div className="admin-list-item"><span>Department</span><strong>{selected.department || "Not assigned"}</strong></div><div className="admin-list-item"><span>Account status</span><strong>{selected.isActive === false ? "Inactive" : "Active"}</strong></div></div></AdminModal>}
  </PageLayout>;
};

export default Managers;
