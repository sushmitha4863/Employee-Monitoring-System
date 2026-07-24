import { useCallback, useState } from "react";
import PageLayout from "../../components/layout/PageLayout";
import AdminModal from "../../components/common/AdminModal";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import useEmsResource from "../../hooks/useEmsResource";
import { emsApi } from "../../services/emsApi";
import "../SharedAdminPages.css";

const Announcements = () => {
  const load = useCallback(() => emsApi.notifications(), []);
  const { data = [], loading, error, reload } = useEmsResource(load);
  const [modal, setModal] = useState(null);
  const [form, setForm] = useState({ title: "", message: "", role: "Employee", scheduledAt: "" });
  const [status, setStatus] = useState("");
  const [filter, setFilter] = useState("");
  const notifications = Array.isArray(data) ? data : [];
  const visibleNotifications = notifications.filter((item) => !filter || (filter === "Unread" ? !item.isRead : item.isRead));
  const markRead = async (id) => { try { await emsApi.markNotificationRead(id); await reload(); } catch (err) { setStatus(err.response?.data?.message || "Unable to mark notification as read."); } };
  const markAllRead = async () => { try { await emsApi.markAllNotificationsRead(); await reload(); } catch (err) { setStatus(err.response?.data?.message || "Unable to mark notifications as read."); } };

  const submit = async (event) => {
    event.preventDefault(); setStatus("");
    if (!form.title || !form.message) { setStatus("Title and message are required."); return; }
    try {
      await emsApi.createNotification({ title: `Announcement: ${form.title}`, message: form.scheduledAt ? `${form.message} Scheduled: ${form.scheduledAt}` : form.message, role: form.role || null });
      setModal(null); setStatus("Announcement broadcast saved."); setForm({ title: "", message: "", role: "Employee", scheduledAt: "" }); await reload();
    } catch (err) { setStatus(err.response?.data?.message || "Unable to send announcement."); }
  };

  return <PageLayout><section className="admin-page-header"><div><span className="admin-page-kicker">Notifications</span><h1>Announcements</h1><p>Create announcements, broadcast role-based notifications and review notification history.</p></div><button className="admin-soft-btn" onClick={markAllRead}>Mark all read</button></section>{status && <p className="admin-inline-message">{status}</p>}<section className="admin-stats-grid"><div className="admin-stat-card"><p className="admin-stat-label">History</p><h3>{notifications.length}</h3><span>Latest records</span></div><div className="admin-stat-card"><p className="admin-stat-label">Unread</p><h3>{notifications.filter((item) => !item.isRead).length}</h3><span>Across current user view</span></div><div className="admin-stat-card"><p className="admin-stat-label">Read</p><h3>{notifications.filter((item) => item.isRead).length}</h3><span>Marked read</span></div><div className="admin-stat-card"><p className="admin-stat-label">Broadcast To</p><h3>3</h3><span>Employees Managers HR</span></div></section>{loading && <LoadingState message="Loading notifications..." />}{error && <ErrorState message={error} />}{!loading && !error && notifications.length === 0 && <EmptyState message="No notifications have been created yet." />}{!loading && !error && visibleNotifications.length > 0 && <section className="admin-card"><div className="admin-card-header"><h3>Notification History</h3><select value={filter} onChange={(event) => setFilter(event.target.value)}><option value="">All</option><option>Unread</option><option>Read</option></select></div><table className="admin-table"><thead><tr><th>Title</th><th>Message</th><th>Status</th><th>Created</th><th>Actions</th></tr></thead><tbody>{visibleNotifications.map((item) => <tr key={item.id || item.notificationId}><td>{item.title}</td><td>{item.message}</td><td><span className={`admin-badge ${item.isRead ? "success" : "warning"}`}>{item.isRead ? "Read" : "Unread"}</span></td><td>{item.createdAt ? new Date(item.createdAt).toLocaleString() : "-"}</td><td>{!item.isRead && <button className="admin-action-btn" onClick={() => markRead(item.id || item.notificationId)}>Mark read</button>}</td></tr>)}</tbody></table></section>}{modal === "form" && <AdminModal title="Create Announcement" onClose={() => setModal(null)}><form onSubmit={submit}><div className="admin-form-grid"><div className="admin-field"><label>Title</label><input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required /></div><div className="admin-field"><label>Send To</label><select value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}><option value="Employee">Employees</option><option value="Manager">Managers</option><option value="HR">HR</option><option value="">Everyone</option></select></div><div className="admin-field"><label>Schedule</label><input type="datetime-local" value={form.scheduledAt} onChange={(e) => setForm({ ...form, scheduledAt: e.target.value })} /></div><div className="admin-field" style={{ gridColumn: "1 / -1" }}><label>Message</label><textarea value={form.message} onChange={(e) => setForm({ ...form, message: e.target.value })} required /></div></div><button className="admin-primary-btn" style={{ marginTop: 18 }}>Broadcast Announcement</button></form></AdminModal>}</PageLayout>;
};
export default Announcements;
