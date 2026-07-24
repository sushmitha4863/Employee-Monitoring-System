import { useState } from "react";
import { EmployeeLayout, EmployeeHero } from "./EmployeeWidgets";
import { emsApi } from "../../services/emsApi";
import { saveBlob } from "../../utils/adminUtils";

const reportTypes = [
  { key: "attendance", name: "Attendance Report" },
  { key: "leave", name: "Leave Report" },
  { key: "projects", name: "Projects Report" },
  { key: "performance", name: "Performance Report" },
  { key: "timesheets", name: "Timesheets Report" },
];

const ext = (format) => format === "excel" ? "xlsx" : format;

const EmployeeReports = () => {
  const [exporting, setExporting] = useState("");
  const [message, setMessage] = useState("");

  const download = async (type, format) => {
    const key = `${type}-${format}`;
    setExporting(key);
    setMessage("");
    try {
      const response = await emsApi.exportMyReport(type, format);
      saveBlob(response.data, `my-${type}-report.${ext(format)}`);
    } catch (err) {
      setMessage(err.response?.data?.message || "Unable to download report.");
    } finally {
      setExporting("");
    }
  };

  return (
    <EmployeeLayout>
      <EmployeeHero title="Reports" subtitle="Download personal attendance, leave, project, performance and timesheet reports." />
      {message && <p className="admin-inline-message">{message}</p>}
      <section className="employee-card employee-section"><div className="employee-section-header"><h2>My Reports</h2></div><table className="employee-table"><thead><tr><th>Report</th><th>Status</th><th>PDF</th><th>Excel</th><th>CSV</th></tr></thead><tbody>{reportTypes.map(report => <tr key={report.key}><td>{report.name}</td><td><span className="employee-badge success">Ready</span></td><td><button className="employee-link" disabled={exporting === `${report.key}-pdf`} onClick={() => download(report.key, "pdf")}>PDF</button></td><td><button className="employee-link" disabled={exporting === `${report.key}-excel`} onClick={() => download(report.key, "excel")}>Excel</button></td><td><button className="employee-link" disabled={exporting === `${report.key}-csv`} onClick={() => download(report.key, "csv")}>CSV</button></td></tr>)}</tbody></table></section>
    </EmployeeLayout>
  );
};

export default EmployeeReports;
