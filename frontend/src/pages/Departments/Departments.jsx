import { useCallback, useEffect, useMemo, useState } from "react";
import PageLayout from "../../components/layout/PageLayout";
import AdminModal from "../../components/common/AdminModal";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import { emsApi } from "../../services/emsApi";
import "../SharedAdminPages.css";

const Departments = () => {
  const [departments, setDepartments] = useState([]);
  const [members, setMembers] = useState([]);
  const [eligibleManagers, setEligibleManagers] = useState([]);
  const [eligibleEmployees, setEligibleEmployees] = useState([]);
  const [loading, setLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [modal, setModal] = useState(null);
  const [selected, setSelected] = useState(null);
  const [selectedManager, setSelectedManager] = useState("");
  const [selectedEmployee, setSelectedEmployee] = useState("");
  const [form, setForm] = useState({ name: "", departmentCode: "", description: "" });

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try { setDepartments((await emsApi.departments()) || []); }
    catch (err) { setError(err.response?.data?.message || "Unable to load departments."); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { load(); }, [load]);

  const filtered = useMemo(() => departments.filter((item) => {
    const q = search.trim().toLowerCase();
    return (!status || (status === "Active" ? item.isActive !== false : item.isActive === false)) && (!q || [item.departmentName, item.name, item.description, item.departmentHead, item.headName]
      .some((value) => String(value || "").toLowerCase().includes(q)));
  }), [departments, search, status]);

  const openCreate = () => { setForm({ name: "", departmentCode: "", description: "" }); setModal("form"); };
  const openEdit = (item) => {
    setForm({ id: item.departmentId || item.id, name: item.departmentName || item.name || "", departmentCode: item.departmentCode || "", description: item.description || "" });
    setModal("form");
  };

  const openDetails = async (item) => {
    const id = item.departmentId || item.id;
    setSelected(item);
    setModal("details");
    setDetailLoading(true);
    try { setMembers((await emsApi.departmentMembers(id)) || []); }
    catch (err) { setMessage(err.response?.data?.message || "Unable to load department members."); }
    finally { setDetailLoading(false); }
  };

  const openManagerModal = async (item) => {
    setSelected(item);
    setSelectedManager("");
    setModal("manager");
    setEligibleManagers((await emsApi.eligibleDepartmentManagers()) || []);
  };

  const openEmployeeModal = async (item) => {
    setSelected(item);
    setSelectedEmployee("");
    setModal("employee");
    setEligibleEmployees((await emsApi.eligibleDepartmentEmployees()) || []);
  };

  const submit = async (event) => {
    event.preventDefault();
    if (!form.name.trim()) { setMessage("Department name is required."); return; }
    try {
      if (form.id) await emsApi.updateDepartment(form.id, form);
      else await emsApi.createDepartment(form);
      setModal(null);
      setMessage(form.id ? "Department updated successfully." : "Department created successfully.");
      await load();
    } catch (err) { setMessage(err.response?.data?.message || "Unable to save department."); }
  };

  const assignManager = async (event) => {
    event.preventDefault();
    if (!selectedManager) return;
    try {
      await emsApi.assignDepartmentManager(selected.departmentId || selected.id, Number(selectedManager));
      setMessage("Manager assigned successfully.");
      setModal(null);
      await load();
    } catch (err) { setMessage(err.response?.data?.message || "Unable to assign manager."); }
  };

  const removeManager = async () => {
    if (!window.confirm("Remove the assigned manager from this department?")) return;
    try {
      await emsApi.removeDepartmentManager(selected.departmentId || selected.id);
      setMessage("Manager removed successfully.");
      await load();
    } catch (err) { setMessage(err.response?.data?.message || "Unable to remove manager."); }
  };

  const assignEmployee = async (event) => {
    event.preventDefault();
    if (!selectedEmployee) return;
    try {
      await emsApi.assignDepartmentEmployee(selected.departmentId || selected.id, Number(selectedEmployee));
      setMessage("Employee assigned successfully.");
      setModal(null);
      await load();
    } catch (err) { setMessage(err.response?.data?.message || "Unable to assign employee."); }
  };

  const removeEmployee = async (employeeId) => {
    if (!window.confirm("Remove this employee from the department?")) return;
    try {
      await emsApi.removeDepartmentEmployee(selected.departmentId || selected.id, employeeId);
      setMembers((items) => items.filter((item) => item.userId !== employeeId));
      setMessage("Employee removed successfully.");
      await load();
    } catch (err) { setMessage(err.response?.data?.message || "Unable to remove employee."); }
  };

  return <PageLayout>
    <section className="admin-page-header">
      <div><span className="admin-page-kicker">Organization Structure</span><h1>Departments</h1><p>Manage department metadata, manager ownership and existing employee membership.</p></div>
      <button className="admin-primary-btn" onClick={openCreate}>Add Department</button>
    </section>
    {message && <p className="admin-inline-message">{message}</p>}
    <div className="admin-toolbar"><input placeholder="Search departments" value={search} onChange={(event) => setSearch(event.target.value)} /><select value={status} onChange={(event) => setStatus(event.target.value)}><option value="">All statuses</option><option>Active</option><option>Inactive</option></select></div>
    {loading && <LoadingState message="Loading departments..." />}
    {error && <ErrorState message={error} onRetry={load} />}
    {!loading && !error && filtered.length === 0 && <EmptyState message="No departments match the current search." />}
    {!loading && !error && filtered.length > 0 && <section className="admin-card">
      <div className="admin-card-header"><h3>Department Register</h3><span className="admin-muted">{filtered.length} departments</span></div>
      <table className="admin-table"><thead><tr><th>Department</th><th>Code</th><th>Manager</th><th>Employees</th><th>Status</th><th>Actions</th></tr></thead>
        <tbody>{filtered.map((department) => {
          const id = department.departmentId || department.id;
          const name = department.departmentName || department.name;
          return <tr key={id}><td><strong>{name}</strong><p className="admin-muted">{department.description || "No description"}</p></td><td>{department.departmentCode || ("DEPT-" + id)}</td><td>{department.departmentHead || department.headName || "Not assigned"}</td><td>{department.employeeCount || 0}</td><td><span className={"admin-badge " + (department.isActive === false ? "danger" : "success")}>{department.isActive === false ? "Inactive" : "Active"}</span></td><td><div className="admin-actions"><button className="admin-action-btn" onClick={() => openDetails(department)}>View</button><button className="admin-action-btn" onClick={() => openEdit(department)}>Edit</button><button className="admin-action-btn" disabled={department.isActive === false} onClick={() => openManagerModal(department)}>Assign Manager</button><button className="admin-action-btn" disabled={department.isActive === false} onClick={() => openEmployeeModal(department)}>Assign Employee</button><button className="admin-action-btn" onClick={async () => { if (!window.confirm((department.isActive === false ? "Activate" : "Deactivate") + " this department?")) return; try { await emsApi.setDepartmentStatus(id, department.isActive === false); setMessage("Department status updated."); await load(); } catch (err) { setMessage(err.response?.data?.message || "Unable to update department status."); } }}>{department.isActive === false ? "Activate" : "Deactivate"}</button></div></td></tr>;
        })}</tbody>
      </table>
    </section>}
    {modal === "form" && <AdminModal title={form.id ? "Edit Department" : "Add Department"} onClose={() => setModal(null)}><form onSubmit={submit}><div className="admin-form-grid"><div className="admin-field"><label>Name</label><input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} required /></div><div className="admin-field"><label>Department code</label><input value={form.departmentCode} onChange={(event) => setForm({ ...form, departmentCode: event.target.value.toUpperCase() })} required /></div><div className="admin-field admin-span-2"><label>Description</label><input value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} /></div></div><button className="admin-primary-btn" style={{ marginTop: 18 }}>Save Department</button></form></AdminModal>}
    {modal === "manager" && <AdminModal title={"Assign Manager - " + (selected?.departmentName || selected?.name)} onClose={() => setModal(null)}><form onSubmit={assignManager}><div className="admin-field"><label>Eligible manager</label><select value={selectedManager} onChange={(event) => setSelectedManager(event.target.value)} required><option value="">Select manager</option>{eligibleManagers.map((manager) => <option key={manager.userId} value={manager.userId}>{manager.fullName} | {manager.email}</option>)}</select></div><button className="admin-primary-btn" style={{ marginTop: 18 }}>Assign Manager</button></form><button className="admin-action-btn danger" style={{ marginTop: 12 }} onClick={removeManager}>Remove current manager</button></AdminModal>}
    {modal === "employee" && <AdminModal title={"Assign Employee - " + (selected?.departmentName || selected?.name)} onClose={() => setModal(null)}><form onSubmit={assignEmployee}><div className="admin-field"><label>Eligible employee</label><select value={selectedEmployee} onChange={(event) => setSelectedEmployee(event.target.value)} required><option value="">Select employee</option>{eligibleEmployees.map((employee) => <option key={employee.userId} value={employee.userId}>{employee.fullName} | {employee.email}</option>)}</select></div><button className="admin-primary-btn" style={{ marginTop: 18 }}>Assign Employee</button></form></AdminModal>}
    {modal === "details" && <AdminModal title={(selected?.departmentName || selected?.name) + " details"} onClose={() => setModal(null)}>{detailLoading ? <LoadingState message="Loading members..." /> : members.length === 0 ? <EmptyState message="No employees are assigned to this department." /> : <table className="admin-table"><thead><tr><th>Employee</th><th>Email</th><th>Role</th><th>Actions</th></tr></thead><tbody>{members.map((member) => <tr key={member.userId}><td>{member.fullName}</td><td>{member.email}</td><td>{member.role}</td><td><button className="admin-action-btn danger" onClick={() => removeEmployee(member.userId)}>Remove</button></td></tr>)}</tbody></table>}</AdminModal>}
  </PageLayout>;
};

export default Departments;

