import { useCallback, useEffect, useState } from "react";
import { MdLogin, MdLogout, MdTimer, MdLocationOn } from "react-icons/md";
import { emsApi } from "../../services/emsApi";
import "./EmployeePanel.css";

const fmt = (value) => value ? new Date(value).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : "-";

const PunchCard = ({ onChanged }) => {
  const [attendance, setAttendance] = useState(null);
  const [loading, setLoading] = useState(false);
  const [workSummary, setWorkSummary] = useState("");
  const [message, setMessage] = useState("");

  const loadToday = useCallback(async () => {
    const data = await emsApi.todayAttendance();
    setAttendance(data);
  }, []);

  useEffect(() => { loadToday(); }, [loadToday]);

  const runAction = async (action, success) => {
    setLoading(true);
    setMessage("");
    try {
      const response = await action();
      setMessage(response.data?.message || success);
      await loadToday();
      if (onChanged) await onChanged();
    } catch (error) {
      setMessage(error.response?.data?.message || "Attendance action failed.");
    } finally {
      setLoading(false);
    }
  };

  const checkedIn = Boolean(attendance?.checkInTime);
  const checkedOut = Boolean(attendance?.checkOutTime);
  const onBreak = Boolean(attendance?.breakStartTime && !attendance?.breakEndTime);

  const checkout = () => {
    if (!workSummary || workSummary.trim().length < 20) {
      setMessage("Checkout requires at least 20 characters of work summary.");
      return;
    }
    runAction(() => emsApi.checkOut(workSummary.trim()), "Checked out successfully");
  };

  return (
    <section className="employee-card employee-section punch-card">
      <div className="employee-section-header">
        <h2>Attendance</h2>
        <span className={`employee-badge ${checkedIn && !checkedOut ? "success" : "warning"}`}>{checkedIn && !checkedOut ? onBreak ? "On Break" : "Checked In" : "Out"}</span>
      </div>
      <div className="punch-status">
        <div className="punch-clock"><MdTimer /><strong>{attendance?.effectiveHours || attendance?.totalHours || 0}h</strong><span>effective</span></div>
        <div>
          <p><MdLocationOn /> Office Network</p>
          <p>In: {fmt(attendance?.checkInTime)} | Out: {fmt(attendance?.checkOutTime)}</p>
          <p>Break: {attendance?.breakMinutes || 0} min | Status: {attendance?.attendanceStatus || attendance?.status || "Not marked"}</p>
        </div>
      </div>
      {checkedIn && !checkedOut && <textarea className="punch-summary" value={workSummary} onChange={(event) => setWorkSummary(event.target.value)} placeholder="Work summary for checkout..." />}
      <div className="admin-actions" style={{ marginTop: 12 }}>
        {!checkedIn && <button className="punch-btn in" onClick={() => runAction(emsApi.checkIn, "Checked in successfully")} disabled={loading}><MdLogin />{loading ? "Please wait..." : "Check In"}</button>}
        {checkedIn && !checkedOut && !onBreak && <button className="employee-link" onClick={() => runAction(emsApi.startBreak, "Break started")} disabled={loading}>Start Break</button>}
        {checkedIn && !checkedOut && onBreak && <button className="employee-link" onClick={() => runAction(emsApi.endBreak, "Break ended")} disabled={loading}>End Break</button>}
        {checkedIn && !checkedOut && <button className="punch-btn out" onClick={checkout} disabled={loading || onBreak}><MdLogout />{loading ? "Please wait..." : "Check Out"}</button>}
      </div>
      {message && <p className="punch-message">{message}</p>}
    </section>
  );
};

export default PunchCard;
