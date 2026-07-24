import { useCallback, useState } from "react";
import { EmployeeLayout, EmployeeHero } from "./EmployeeWidgets";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import useEmsResource from "../../hooks/useEmsResource";
import { emsApi } from "../../services/emsApi";

const EmployeeAnnouncements = () => {
  const load = useCallback(() => emsApi.employeeAnnouncements(), []);
  const { data = [], setData, loading, error } = useEmsResource(load);
  const [message, setMessage] = useState("");
  const items = Array.isArray(data) ? data : [];

  const markRead = async (item) => {
    try {
      await emsApi.markNotificationRead(item.id);
      setData((rows) => rows.map((row) => row.id === item.id ? { ...row, isRead: true } : row));
      setMessage("Announcement marked as read.");
    } catch (err) {
      setMessage(err.response?.data?.message || "Unable to mark announcement as read.");
    }
  };

  return (
    <EmployeeLayout>
      <EmployeeHero title="Announcements" subtitle="Company updates, HR notices, events and important messages." />
      {message && <p className="admin-inline-message">{message}</p>}
      {loading && <LoadingState message="Loading announcements..." />}
      {error && <ErrorState message={error} />}
      {!loading && !error && items.length === 0 && <EmptyState message="No announcements are available." />}
      {!loading && !error && items.length > 0 && <section className="employee-card employee-section"><h2>Latest Announcements</h2><div className="event-list">{items.map(item => <div className="event-row" key={item.id}><span className="event-color project" /><div><h4>{item.title}</h4><p>{item.message || item.description || ""}</p><small>{item.createdAt ? new Date(item.createdAt).toLocaleString() : ""}</small></div><button className="employee-link" disabled={item.isRead} onClick={() => markRead(item)}>{item.isRead ? "Read" : "Mark Read"}</button></div>)}</div></section>}
    </EmployeeLayout>
  );
};

export default EmployeeAnnouncements;
