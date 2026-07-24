import { useCallback, useState } from "react";
import { EmployeeLayout, EmployeeHero } from "./EmployeeWidgets";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import useEmsResource from "../../hooks/useEmsResource";
import { emsApi } from "../../services/emsApi";
import { saveBlob } from "../../utils/adminUtils";

const EmployeeDocuments = () => {
  const load = useCallback(() => emsApi.documents(), []);
  const { data = [], loading, error } = useEmsResource(load);
  const [message, setMessage] = useState("");
  const [query, setQuery] = useState("");
  const [folder, setFolder] = useState("All");
  const docs = Array.isArray(data) ? data : [];
  const folders = ["All", ...new Set(docs.map((doc) => doc.category).filter(Boolean))];
  const filtered = docs.filter((doc) => (folder === "All" || doc.category === folder) && `${doc.originalFileName} ${doc.category}`.toLowerCase().includes(query.toLowerCase()));

  const download = async (doc) => {
    try {
      const res = await emsApi.downloadDocument(doc.documentId);
      saveBlob(res.data, doc.originalFileName);
    } catch (err) {
      setMessage(err.response?.data?.message || "Unable to download document.");
    }
  };

  return (
    <EmployeeLayout>
      <EmployeeHero title="Documents" subtitle="Offer letters, salary slips, tax documents, policies and certificates available to download." />
      {message && <p className="admin-inline-message">{message}</p>}
      {loading && <LoadingState message="Loading documents..." />}
      {error && <ErrorState message={error} />}
      {!loading && !error && filtered.length === 0 && <EmptyState message={docs.length ? "No documents match the current filters." : "No employee documents are available."} />}
      {!loading && !error && filtered.length > 0 && <section className="employee-card employee-section"><div className="employee-section-header"><h2>Document Center</h2><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search documents"/><select value={folder} onChange={(e) => setFolder(e.target.value)}>{folders.map((item) => <option key={item}>{item}</option>)}</select></div><table className="employee-table"><thead><tr><th>Document</th><th>Category</th><th>Type</th><th>Updated</th><th>Action</th></tr></thead><tbody>{filtered.map(doc => <tr key={doc.documentId}><td>{doc.originalFileName}</td><td>{doc.category}</td><td>{doc.contentType}</td><td>{doc.uploadedAt ? new Date(doc.uploadedAt).toLocaleDateString() : "-"}</td><td><button className="employee-link" onClick={() => download(doc)}>Download</button></td></tr>)}</tbody></table></section>}
    </EmployeeLayout>
  );
};

export default EmployeeDocuments;
