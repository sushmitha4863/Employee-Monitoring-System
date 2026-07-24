import axios from "axios";
import { getToken, clearStorage } from "../utils/storage";

const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || (process.env.NODE_ENV === "development" ? "http://localhost:5072/api" : "https://localhost:7015/api");

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

// Attach JWT Token
api.interceptors.request.use(
  (config) => {
    const token = getToken();

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => Promise.reject(error)
);

// Handle Unauthorized Response
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && getToken() && !String(error.config?.url || "").includes("/Auth/")) {
      clearStorage();
      window.location.href = "/login";
    }

    return Promise.reject(error);
  }
);

export default api;
