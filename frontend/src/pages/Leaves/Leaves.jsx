import { useCallback, useMemo, useState } from "react";
import PageLayout from "../../components/layout/PageLayout";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import useEmsResource from "../../hooks/useEmsResource";
import { emsApi } from "../../services/emsApi";
import "../SharedAdminPages.css";

const badgeType = (status) => status === "Approved" ? "success" : status === "Rejected" ? "danger" : "warning";

const Leaves = () => {
  const load = useCallback(async () => {
    const [leaves, settings] = await Promise.all([emsApi.leaves(), emsApi.settings()]);
    return { leaves, settings };
  }, []);
  const { data, setData, loading, error } = useEmsResource(load);
  const [actionId, setActionId] = useState(null);
  const [tab, setTab] = useState("Pending");
  const [search, setSearch] = useState("");
  const [message, setMessage] = useState("");
  const leaves = useMemo(() => Array.isArray(data?.leaves) ? data.leaves : [], [data?.leaves]);
  const settings = Array.isArray(data?.settings) ? data.settings : [];
  const annualPolicy = settings.find((item) => item.settingKey === "AnnualLeaveDays")?.settingValue || "12";

  const rows = useMemo(() => leaves.filter((leave) => {
    const matchesTab = tab === "History" || leave.status === tab;
    const matchesSearch = !search || [leave.employeeName, leave.leaveType, leave.reason].some((value) => String(value || "").toLowerCase().includes(search.toLowerCase()));
    return matchesTab && matchesSearch;
  }), [leaves, tab, search]);

  const updateLeave = async (id, action) => {
    setActionId(id); setMessage("");
    try {
      await (action === "approve" ? emsApi.approveLeave(id) : emsApi.rejectLeave(id));
      setData((current) => ({ ...current, leaves: current.leaves.map((item) => item.leaveId === id ? { ...item, status: action === "approve" ? "Approved" : "Rejected" } : item) }));
      setMessage(action === "approve" ? "Leave approved and employee notified." : "Leave rejected and employee notified.");
    } catch (err) { setMessage(err.response?.data?.message || "Unable to update leave."); }
    finally { setActionId(null); }
  };

  return <PageLayout><section className="admin-page-header"><div><span className="admin-page-kicker">Leave Management</span><h1>Leaves</h1><p>Approve requests, review history, monitor leave policy and balances.</p></div></section>{message && <p className="admin-inline-message">{message}</p>}<section className="admin-stats-grid"><div className="admin-stat-card"><p className="admin-stat-label">Pending</p><h3>{leaves.filter((item) => item.status === "Pending").length}</h3><span>Action required</span></div><div className="admin-stat-card"><p className="admin-stat-label">Approved</p><h3>{leaves.filter((item) => item.status === "Approved").length}</h3><span>History</span></div><div className="admin-stat-card"><p className="admin-stat-label">Rejected</p><h3>{leaves.filter((item) => item.status === "Rejected").length}</h3><span>History</span></div><div className="admin-stat-card"><p className="admin-stat-label">Annual Policy</p><h3>{annualPolicy}</h3><span>Days</span></div></section><div className="admin-tabs">{["Pending", "Approved", "Rejected", "History"].map((item) => <button key={item} className={tab === item ? "active" : ""} onClick={() => setTab(item)}>{item}</button>)}</div><div className="admin-toolbar"><input placeholder="Search leaves" value={search} onChange={(e) => setSearch(e.target.value)} /></div>{loading && <LoadingState message="Loading leave requests..." />}{error && <ErrorState message={error} />}{!loading && !error && rows.length === 0 && <EmptyState message="No leave records match the current view." />}{!loading && !error && rows.length > 0 && <section className="admin-card"><table className="admin-table"><thead><tr><th>Employee</th><th>Type</th><th>From</th><th>To</th><th>Days</th><th>Reason</th><th>Status</th><th>Approved By</th><th>Actions</th></tr></thead><tbody>{rows.map((request) => <tr key={request.leaveId}><td>{request.employeeName || "-"}</td><td>{request.leaveType || "-"}</td><td>{String(request.fromDate || "-").slice(0, 10)}</td><td>{String(request.toDate || "-").slice(0, 10)}</td><td>{request.totalDays || 1}</td><td>{request.reason || "-"}</td><td><span className={`admin-badge ${badgeType(request.status)}`}>{request.status}</span></td><td>{request.approvedBy || "—"}</td><td>{request.status === "Pending" ? <div className="admin-actions"><button className="admin-action-btn success" disabled={actionId === request.leaveId} onClick={() => updateLeave(request.leaveId, "approve")}>Approve</button><button className="admin-action-btn danger" disabled={actionId === request.leaveId} onClick={() => updateLeave(request.leaveId, "reject")}>Reject</button></div> : "-"}</td></tr>)}</tbody></table></section>}</PageLayout>;
};
export default Leaves;
