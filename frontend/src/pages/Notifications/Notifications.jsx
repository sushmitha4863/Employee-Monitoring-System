import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import PageLayout from "../../components/layout/PageLayout";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import { emsApi } from "../../services/emsApi";
import "../SharedAdminPages.css";

const pageSize = 10;
const listOf = (value) => Array.isArray(value) ? value : value?.items || value?.data || [];

export default function Notifications() {
  const navigate = useNavigate();
  const [data, setData] = useState(null); const [loading, setLoading] = useState(true); const [error, setError] = useState("");
  const [read, setRead] = useState(""); const [type, setType] = useState(""); const [page, setPage] = useState(1); const [busy, setBusy] = useState("");
  const load = useCallback(async () => { setLoading(true); setError(""); try { setData(await emsApi.notifications({ isRead: read === "" ? undefined : read === "read", type: type || undefined, page, pageSize })); } catch (e) { setError(e.response?.data?.message || "Unable to load notifications."); } finally { setLoading(false); } }, [read, type, page]);
  useEffect(() => { load(); }, [load]);
  useEffect(() => { setPage(1); }, [read, type]);
  const rows = useMemo(() => listOf(data), [data]);
  const total = data?.totalCount ?? rows.length; const pages = Math.max(1, Math.ceil(total / pageSize));
  const changed = () => window.dispatchEvent(new CustomEvent("ems:notifications-changed"));
  const markRead = async (id) => { setBusy(id); try { await emsApi.markNotificationRead(id); changed(); await load(); } catch (e) { setError(e.response?.data?.message || "Unable to update notification."); } finally { setBusy(""); } };
  const markAll = async () => { setBusy("all"); try { await emsApi.markAllNotificationsRead(); changed(); await load(); } catch (e) { setError(e.response?.data?.message || "Unable to update notifications."); } finally { setBusy(""); } };
  const openRelated = (item) => { const target = item.relatedUrl || item.actionUrl; if (target?.startsWith("/admin/")) navigate(target); };
  return <PageLayout>
    <section className="admin-page-header"><div><span className="admin-page-kicker">System Inbox</span><h1>Notifications</h1><p>Security, configuration, assignment, project and company-calendar notifications for your account.</p></div><button className="admin-primary-btn" disabled={busy === "all"} onClick={markAll}>Mark all read</button></section>
    <div className="admin-toolbar"><select value={read} onChange={e => setRead(e.target.value)}><option value="">All notifications</option><option value="unread">Unread</option><option value="read">Read</option></select><input placeholder="Filter by type" value={type} onChange={e => setType(e.target.value)} /><button className="admin-action-btn" onClick={load}>Refresh</button></div>
    {loading && <LoadingState message="Loading notifications..." />}{!loading && error && <><ErrorState message={error} /><button className="admin-primary-btn" onClick={load}>Retry</button></>}
    {!loading && !error && rows.length === 0 && <EmptyState message="No notifications match the current filters." />}
    {!loading && !error && rows.length > 0 && <section className="admin-card"><div className="admin-list">{rows.map(item => { const id=item.notificationId||item.id; return <article className="admin-list-item" key={id}><div><h4>{item.title || item.type || "System notification"}</h4><p className="admin-muted">{item.message}</p><small className="admin-muted">{item.createdAt ? new Date(item.createdAt).toLocaleString() : ""} · {item.type || "General"}</small></div><div className="admin-actions"><span className={`admin-badge ${item.isRead ? "success" : "warning"}`}>{item.isRead ? "Read" : "Unread"}</span>{!item.isRead && <button className="admin-action-btn" disabled={busy===id} onClick={()=>markRead(id)}>Mark read</button>}{(item.relatedUrl||item.actionUrl) && <button className="admin-action-btn" onClick={()=>openRelated(item)}>Open</button>}</div></article>; })}</div><div className="admin-pagination"><span>{total} notifications</span><div><button disabled={page<=1} onClick={()=>setPage(p=>p-1)}>Previous</button><button disabled={page>=pages} onClick={()=>setPage(p=>p+1)}>Next</button></div></div></section>}
  </PageLayout>;
}