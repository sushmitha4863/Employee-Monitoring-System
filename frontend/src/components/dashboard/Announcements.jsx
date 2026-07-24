import { useEffect, useState } from "react";
import "./Announcements.css";
import { emsApi } from "../../services/emsApi";

const Announcements = () => {
  const [announcements, setAnnouncements] = useState([]);

  useEffect(() => {
    loadAnnouncements();
  }, []);

  const loadAnnouncements = async () => {
    try {
      const response = await emsApi.notifications();
      setAnnouncements(response || []);
    } catch (error) {
      console.error(error);
    }
  };

  return (
    <div className="announcement-card">
      <div className="announcement-header">
        <h3>Announcements</h3>

        <button>View All</button>
      </div>

      {announcements.length === 0 ? (
        <div
          style={{
            textAlign: "center",
            padding: "25px",
            color: "#6B7280",
          }}
        >
          No announcements available
        </div>
      ) : (
        announcements.slice(0, 5).map((item) => (
          <div
            key={item.id}
            className="announcement-item"
          >
            <div>
              <h5>{item.title}</h5>

              <span>
                {item.date ||
                  item.createdDate ||
                  item.createdAt}
              </span>
            </div>

            <label>
              {item.type || "Announcement"}
            </label>
          </div>
        ))
      )}
    </div>
  );
};

export default Announcements;