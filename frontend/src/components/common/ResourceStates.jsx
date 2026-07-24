export const LoadingState = ({ message = "Loading data..." }) => (
  <div className="admin-card"><p className="admin-muted">{message}</p></div>
);

export const EmptyState = ({ message = "No records found." }) => (
  <div className="admin-card"><p className="admin-muted">{message}</p></div>
);

export const ErrorState = ({ message }) => (
  <div className="admin-card"><p className="admin-muted">{message || "Something went wrong while loading data."}</p></div>
);
