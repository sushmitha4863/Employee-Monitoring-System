import { useEffect, useState } from "react";
import "./RecentProjects.css";
import { emsApi } from "../../services/emsApi";
import { useNavigate } from "react-router-dom";

const RecentProjects = () => {
  const navigate = useNavigate();
  const [projects, setProjects] = useState([]);

  useEffect(() => {
    loadProjects();
  }, []);

  const loadProjects = async () => {
    try {
      const response = await emsApi.projects();

      if (Array.isArray(response)) {
        setProjects(response.slice(0, 5));
      } else {
        setProjects([]);
      }
    } catch (error) {
      console.error("Failed to load projects:", error);
    }
  };

  return (
    <div className="projects-card">
      <div className="projects-header">
        <h3>Recent Projects</h3>

        <button onClick={() => navigate("/admin/projects")}>View All</button>
      </div>

      {projects.length === 0 ? (
        <div
          style={{
            textAlign: "center",
            padding: "30px",
            color: "#6B7280",
          }}
        >
          No projects available
        </div>
      ) : (
        projects.map((project) => (
          <div
            key={project.projectId || project.id}
            className="project-item"
          >
            <div className="project-top">
              <div>
                <h5>
                  {project.projectName || project.name}
                </h5>

                <span>
                  {project.managerName || project.manager}
                </span>
              </div>

              <strong>
                {project.progressPercentage ?? project.progress ?? 0}%
              </strong>
            </div>

            <div className="project-progress">
              <div
                className="project-progress-bar"
                style={{
                  width: `${project.progressPercentage ?? project.progress ?? 0}%`,
                }}
              ></div>
            </div>

            <small>
              {project.status || "Not Started"}
            </small>
          </div>
        ))
      )}
    </div>
  );
};

export default RecentProjects;