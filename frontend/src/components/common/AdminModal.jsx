const AdminModal = ({ title, children, onClose }) => (
  <div className="admin-modal-backdrop" role="dialog" aria-modal="true">
    <div className="admin-modal">
      <div className="admin-modal-header">
        <h3>{title}</h3>
        <button className="admin-close-btn" type="button" onClick={onClose}>x</button>
      </div>
      {children}
    </div>
  </div>
);

export default AdminModal;
