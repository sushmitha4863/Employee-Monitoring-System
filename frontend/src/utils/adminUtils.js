export const saveBlob = (blob, fileName) => {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
};

export const filterByText = (items, term, fields) => {
  const query = term.trim().toLowerCase();
  if (!query) return items;
  return items.filter((item) => fields.some((field) => String(item[field] || "").toLowerCase().includes(query)));
};
