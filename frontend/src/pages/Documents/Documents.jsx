import { useCallback, useEffect, useState } from "react";
import PageLayout from "../../components/layout/PageLayout";
import AdminModal from "../../components/common/AdminModal";
import { EmptyState, ErrorState, LoadingState } from "../../components/common/ResourceStates";
import useEmsResource from "../../hooks/useEmsResource";
import { emsApi } from "../../services/emsApi";
import { saveBlob } from "../../utils/adminUtils";
import "../SharedAdminPages.css";

const Documents = () => {
  const load = useCallback(() => emsApi.documents(), []);
  const { data = [], loading, error, reload } = useEmsResource(load);
  const [modal, setModal] = useState(null);
  const [file, setFile] = useState(null);
  const [category, setCategory] = useState("General");
  const [query, setQuery] = useState("");
  const [folder, setFolder] = useState("All");
  const [message, setMessage] = useState("");
  const [preview, setPreview] = useState(null);
  const docs = Array.isArray(data) ? data : [];
  const folders = ["All", ...new Set(docs.map((doc) => doc.category).filter(Boolean))];
  const filteredDocs = docs.filter((doc) => (folder === "All" || doc.category === folder) && `${doc.originalFileName} ${doc.category} ${doc.uploadedBy}`.toLowerCase().includes(query.toLowerCase()));

  useEffect(() => () => {
    if (preview?.url) window.URL.revokeObjectURL(preview.url);
  }, [preview]);

  const upload = async (event) => {
    event.preventDefault();
    if (!file) {
      setMessage("Choose a document to upload.");
      return;
    }

    try {
      await emsApi.uploadDocument(file, category);
      setModal(null);
      setFile(null);
      setMessage("Document uploaded.");
      await reload();
    } catch (err) {
      setMessage(err.response?.data?.message || "Upload failed.");
    }
  };

  const download = async (doc) => {
    const res = await emsApi.downloadDocument(doc.documentId);
    saveBlob(res.data, doc.originalFileName);
  };

  const openPreview = async (doc) => {
    try {
      const res = await emsApi.downloadDocument(doc.documentId);
      const url = window.URL.createObjectURL(res.data);
      setPreview({ url, name: doc.originalFileName, contentType: doc.contentType });
    } catch (err) {
      setMessage(err.response?.data?.message || "Preview failed.");
    }
  };

  const closePreview = () => {
    if (preview?.url) window.URL.revokeObjectURL(preview.url);
    setPreview(null);
  };

  const remove = async (doc) => {
    if (!window.confirm(`Delete ${doc.originalFileName}?`)) return;
    await emsApi.deleteDocument(doc.documentId);
    setMessage("Document deleted.");
    await reload();
  };

  return (
    <PageLayout>
      <section className="admin-page-header">
        <div>
          <span className="admin-page-kicker">Document Center</span>
          <h1>Documents</h1>
          <p>Upload, preview, download and delete organization documents with metadata.</p>
        </div>
        <div className="admin-actions"><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search documents"/><select value={folder} onChange={(e) => setFolder(e.target.value)}>{folders.map((item) => <option key={item}>{item}</option>)}</select><button className="admin-primary-btn" onClick={() => setModal("upload")}>Upload Document</button></div>
      </section>

      {message && <p className="admin-inline-message">{message}</p>}
      {loading && <LoadingState message="Loading documents..." />}
      {error && <ErrorState message={error} />}
      {!loading && !error && filteredDocs.length === 0 && <EmptyState message={docs.length ? "No documents match the current filters." : "No documents uploaded yet."} />}
      {!loading && !error && filteredDocs.length > 0 && (
        <section className="admin-card">
          <table className="admin-table">
            <thead>
              <tr><th>Document</th><th>Category</th><th>Type</th><th>Size</th><th>Uploaded</th><th>Actions</th></tr>
            </thead>
            <tbody>
              {filteredDocs.map((doc) => (
                <tr key={doc.documentId}>
                  <td>{doc.originalFileName}</td>
                  <td>{doc.category}</td>
                  <td>{doc.contentType}</td>
                  <td>{Math.round((doc.size || 0) / 1024)} KB</td>
                  <td>{doc.uploadedAt ? new Date(doc.uploadedAt).toLocaleString() : "-"}</td>
                  <td>
                    <div className="admin-actions">
                      <button className="admin-action-btn" onClick={() => openPreview(doc)}>Preview</button>
                      <button className="admin-action-btn" onClick={() => download(doc)}>Download</button>
                      <button className="admin-action-btn danger" onClick={() => remove(doc)}>Delete</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      {modal === "upload" && (
        <AdminModal title="Upload Document" onClose={() => setModal(null)}>
          <form onSubmit={upload}>
            <div className="admin-form-grid">
              <div className="admin-field">
                <label>Category</label>
                <select value={category} onChange={(e) => setCategory(e.target.value)}>
                  <option>General</option><option>HR</option><option>Compliance</option><option>Policy</option><option>Payroll</option>
                </select>
              </div>
              <div className="admin-field">
                <label>File</label>
                <input type="file" onChange={(e) => setFile(e.target.files?.[0] || null)} required />
              </div>
            </div>
            <button className="admin-primary-btn" style={{ marginTop: 18 }}>Upload</button>
          </form>
        </AdminModal>
      )}

      {preview && (
        <AdminModal title={preview.name} onClose={closePreview}>
          <iframe className="admin-file-preview" title="Document Preview" src={preview.url} />
        </AdminModal>
      )}
    </PageLayout>
  );
};

export default Documents;
