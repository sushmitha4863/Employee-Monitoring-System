import { useEffect, useState } from "react";
import "./PendingLeaves.css";
import { emsApi } from "../../services/emsApi";

const PendingLeaves = () => {
  const [requests, setRequests] = useState([]);

  useEffect(() => {
    loadPendingLeaves();
  }, []);

  const loadPendingLeaves = async () => {
    try {
      const response = await emsApi.leaves();

      // Show only pending requests
      const pending = (response || []).filter(
        (leave) =>
          leave.status &&
          leave.status.toLowerCase() === "pending"
      );

      setRequests(pending.slice(0, 5));
    } catch (error) {
      console.error("Failed to load pending leaves:", error);
    }
  };

  return (
    <div className="leave-card">
      <div className="leave-header">
        <h4>Pending Leaves</h4>

        <span>{requests.length}</span>
      </div>

      {requests.length === 0 ? (
        <p className="empty-message">
          No pending leave requests
        </p>
      ) : (
        requests.map((request) => (
          <div
            key={request.id}
            className="leave-item"
          >
            <div className="leave-avatar">
              {(request.employeeName || "?").charAt(0)}
            </div>

            <div className="leave-info">
              <h6>{request.employeeName}</h6>

              <p>{request.leaveType}</p>
            </div>

            <div className="leave-days">
              {request.numberOfDays} Day
              {request.numberOfDays > 1 ? "s" : ""}
            </div>
          </div>
        ))
      )}

      <button className="leave-btn">
        Review Requests
      </button>
    </div>
  );
};

export default PendingLeaves;