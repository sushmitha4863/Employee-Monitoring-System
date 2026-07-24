import { useCallback, useEffect, useState } from "react";

const useEmsResource = (loader) => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const reload = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const result = await loader();
      setData(result);
    } catch (err) {
      setError(err.response?.data?.message || err.message || "Unable to load data.");
    } finally {
      setLoading(false);
    }
  }, [loader]);

  useEffect(() => {
    reload();
  }, [reload]);

  return { data, setData, loading, error, reload };
};

export default useEmsResource;
