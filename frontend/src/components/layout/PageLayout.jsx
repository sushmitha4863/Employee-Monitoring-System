import Sidebar from "./Sidebar";
import Topbar from "./Topbar";
import "./PageLayout.css";

const PageLayout = ({ children }) => {
  return (
    <div className="app-layout">
      <Sidebar />

      <div className="main-content">
        <Topbar />

        <div className="page-container">
          {children}
        </div>
      </div>
    </div>
  );
};

export default PageLayout;