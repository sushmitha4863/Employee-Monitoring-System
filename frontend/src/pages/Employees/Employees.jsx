import { useCallback, useEffect, useMemo, useState } from "react";
import PageLayout from "../../components/layout/PageLayout";
import AdminModal from "../../components/common/AdminModal";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import { emsApi } from "../../services/emsApi";
import "../SharedAdminPages.css";

const emptyForm = { fullName: "", email: "", username: "", password: "", role: "Employee", employeeCode: "", phoneNumber: "", designation: "", dateOfJoining: "", departmentId: "", managerId: "", isActive: true };
const pageSize = 10;

const Employees = () => {
  const [employees, setEmployees] = useState([]);
  const [departments, setDepartments] = useState([]);
  const [managers, setManagers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [search, setSearch] = useState("");
  const [department, setDepartment] = useState("");
  const [status, setStatus] = useState("");
  const [page, setPage] = useState(1);
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState(emptyForm);
  const [resetPassword, setResetPassword] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const [employeeData, departmentData, managerData] = await Promise.all([emsApi.employees(), emsApi.departments(), emsApi.managers()]);
      setEmployees(employeeData || []);
      setDepartments(departmentData || []);
      setManagers(managerData || []);
    } catch (err) {
      setError(err.response?.data?.message || "Unable to load employees.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const filtered = useMemo(() => employees.filter((employee) => {
    const q = search.toLowerCase();
    const matchesSearch = !q || [employee.fullName, employee.email, employee.employeeCode, employee.designation].some((value) => String(value || "").toLowerCase().includes(q));
    const matchesDepartment = !department || String(employee.departmentId || "") === department;
    const matchesStatus = !status || (status === "Active" ? employee.isActive !== false : employee.isActive === false);
    return matchesSearch && matchesDepartment && matchesStatus;
  }), [employees, search, department, status]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));
  const rows = filtered.slice((page - 1) * pageSize, page * pageSize);

  useEffect(() => { setPage(1); }, [search, department, status]);

  const openAdd = () => { setForm(emptyForm); setModal("form"); };
  const openEdit = (employee) => { setForm({ ...emptyForm, ...employee, departmentId: employee.departmentId || "", managerId: employee.managerId || "", password: "" }); setModal("form"); };
  const openView = (employee) => { setForm({ ...emptyForm, ...employee }); setModal("view"); };
  const openReset = (employee) => { setForm({ ...emptyForm, ...employee }); setResetPassword(""); setModal("reset"); };

  const submit = async (event) => {
    event.preventDefault();
    setMessage("");
    const payload = { ...form, role: "Employee", departmentId: form.departmentId ? Number(form.departmentId) : null, managerId: form.managerId ? Number(form.managerId) : null };
    if (!payload.fullName || !payload.email || !payload.username) { setMessage("Name, email and username are required."); return; }
    try {
      if (form.userId) await emsApi.updateUser(form.userId, payload);
      else {
        if (!payload.password) { setMessage("Password is required for new employees."); return; }
        await emsApi.createUser(payload);
      }
      setModal(null);
      setMessage(form.userId ? "Employee updated." : "Employee created.");
      await load();
    } catch (err) {
      setMessage(err.response?.data?.message || "Unable to save employee.");
    }
  };

  const remove = async (employee) => {
    if (!window.confirm(`Delete ${employee.fullName}?`)) return;
    await emsApi.deleteUser(employee.userId);
    setMessage("Employee deleted.");
    await load();
  };

  const toggleStatus = async (employee) => {
    await emsApi.setUserStatus(employee.userId, employee.isActive === false);
    setMessage(employee.isActive === false ? "Employee activated." : "Employee deactivated.");
    await load();
  };

  const submitReset = async (event) => {
    event.preventDefault();
    if (resetPassword.length < 6) { setMessage("Password must be at least 6 characters."); return; }
    await emsApi.resetUserPassword(form.userId, resetPassword);
    setModal(null);
    setMessage("Password reset successfully.");
  };

  return (
    <PageLayout>
      <section className="admin-page-header"><div><span className="admin-page-kicker">Employee Management</span><h1>Employees</h1><p>Manage employee records, departments, reporting managers and account access.</p></div><button className="admin-primary-btn" onClick={openAdd}>Add Employee</button></section>
      {message && <p className="admin-inline-message">{message}</p>}
      <div className="admin-toolbar"><input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Search employees" /><select value={department} onChange={(e) => setDepartment(e.target.value)}><option value="">All Departments</option>{departments.map((item) => <option key={item.departmentId || item.id} value={item.departmentId || item.id}>{item.departmentName || item.name}</option>)}</select><select value="Employee" disabled><option>Employee</option></select><select value={status} onChange={(e) => setStatus(e.target.value)}><option value="">All Status</option><option>Active</option><option>Inactive</option></select></div>
      {loading && <LoadingState message="Loading employees..." />}
      {error && <ErrorState message={error} />}
      {!loading && !error && filtered.length === 0 && <EmptyState message="No employees match the current filters." />}
      {!loading && !error && filtered.length > 0 && <section className="admin-card"><table className="admin-table"><thead><tr><th>Employee</th><th>Department</th><th>Manager</th><th>Designation</th><th>Status</th><th>Actions</th></tr></thead><tbody>{rows.map((employee) => <tr key={employee.userId}><td><div className="admin-person"><div className="admin-avatar">{employee.fullName?.[0] || "E"}</div><div><h4>{employee.fullName}</h4><span>{employee.email}</span></div></div></td><td>{employee.department || employee.departmentName || "-"}</td><td>{employee.manager || "-"}</td><td>{employee.designation || "-"}</td><td><span className={`admin-badge ${employee.isActive === false ? "danger" : "success"}`}>{employee.isActive === false ? "Inactive" : "Active"}</span></td><td><div className="admin-actions"><button className="admin-action-btn" onClick={() => openView(employee)}>View</button><button className="admin-action-btn" onClick={() => openEdit(employee)}>Edit</button><button className="admin-action-btn" onClick={() => openReset(employee)}>Reset</button><button className="admin-action-btn success" onClick={() => toggleStatus(employee)}>{employee.isActive === false ? "Activate" : "Deactivate"}</button><button className="admin-action-btn danger" onClick={() => remove(employee)}>Delete</button></div></td></tr>)}</tbody></table><div className="admin-pagination"><span>Showing {(page - 1) * pageSize + 1}-{Math.min(page * pageSize, filtered.length)} of {filtered.length}</span><div><button disabled={page === 1} onClick={() => setPage(page - 1)}>Previous</button> <button disabled={page === totalPages} onClick={() => setPage(page + 1)}>Next</button></div></div></section>}
      {modal === "form" && <AdminModal title={form.userId ? "Edit Employee" : "Add Employee"} onClose={() => setModal(null)}><form onSubmit={submit}><div className="admin-form-grid"><Field label="Full Name" value={form.fullName} onChange={(v) => setForm({ ...form, fullName: v })} required /><Field label="Email" type="email" value={form.email} onChange={(v) => setForm({ ...form, email: v })} required /><Field label="Username" value={form.username} onChange={(v) => setForm({ ...form, username: v })} required />{!form.userId && <Field label="Password" type="password" value={form.password} onChange={(v) => setForm({ ...form, password: v })} required />}<Field label="Employee Code" value={form.employeeCode || ""} onChange={(v) => setForm({ ...form, employeeCode: v })} /><Field label="Phone" value={form.phoneNumber || ""} onChange={(v) => setForm({ ...form, phoneNumber: v })} /><Field label="Designation" value={form.designation || ""} onChange={(v) => setForm({ ...form, designation: v })} /><Field label="Joining Date" type="date" value={String(form.dateOfJoining || "").slice(0, 10)} onChange={(v) => setForm({ ...form, dateOfJoining: v })} /><div className="admin-field"><label>Department</label><select value={form.departmentId || ""} onChange={(e) => setForm({ ...form, departmentId: e.target.value })}><option value="">Select Department</option>{departments.map((item) => <option key={item.departmentId || item.id} value={item.departmentId || item.id}>{item.departmentName || item.name}</option>)}</select></div><div className="admin-field"><label>Manager</label><select value={form.managerId || ""} onChange={(e) => setForm({ ...form, managerId: e.target.value })}><option value="">Select Manager</option>{managers.map((item) => <option key={item.userId} value={item.userId}>{item.fullName}</option>)}</select></div></div>{message && <p className="admin-inline-message">{message}</p>}<button className="admin-primary-btn" style={{ marginTop: 18 }}>Save Employee</button></form></AdminModal>}
      {modal === "view" && <AdminModal title="Employee Details" onClose={() => setModal(null)}><div className="admin-grid-2"><div><p><strong>Name:</strong> {form.fullName}</p><p><strong>Email:</strong> {form.email}</p><p><strong>Employee Code:</strong> {form.employeeCode || "-"}</p><p><strong>Phone:</strong> {form.phoneNumber || "-"}</p></div><div><p><strong>Department:</strong> {form.department || "-"}</p><p><strong>Manager:</strong> {form.manager || "-"}</p><p><strong>Designation:</strong> {form.designation || "-"}</p><p><strong>Status:</strong> {form.isActive === false ? "Inactive" : "Active"}</p></div></div></AdminModal>}
      {modal === "reset" && <AdminModal title="Reset Password" onClose={() => setModal(null)}><form onSubmit={submitReset}><div className="admin-field"><label>New Password</label><input type="password" value={resetPassword} onChange={(e) => setResetPassword(e.target.value)} required minLength={6} /></div><button className="admin-primary-btn" style={{ marginTop: 18 }}>Reset Password</button></form></AdminModal>}
    </PageLayout>
  );
};

const Field = ({ label, value, onChange, type = "text", required = false }) => <div className="admin-field"><label>{label}</label><input type={type} value={value} onChange={(e) => onChange(e.target.value)} required={required} /></div>;

export default Employees;
