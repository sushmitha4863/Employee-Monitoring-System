import React from "react";
import "./Topbar.css";

import {
  MdSearch,
  MdSettings,
  MdKeyboardArrowDown
} from "react-icons/md";

import { useAuth } from "../../context/AuthContext";
import NotificationBell from "../common/NotificationBell";
import ThemeToggle from "../common/ThemeToggle";

const Topbar = () => {
  const { user } = useAuth();

  return (
    <header className="topbar">
      <div className="topbar-left">
        <div className="search-box">
          <MdSearch />
          <input type="text" placeholder="Search employees, projects..." />
        </div>
      </div>

      <div className="topbar-right">
        <ThemeToggle />
        <NotificationBell />
        <button className="icon-btn"><MdSettings /></button>
        <div className="top-profile">
          <div className="top-avatar">{user?.fullName ? user.fullName.charAt(0) : "A"}</div>
          <div><h5>{user?.fullName || "Administrator"}</h5><span>{user?.role || "Admin"}</span></div>
          <MdKeyboardArrowDown />
        </div>
      </div>
    </header>
  );
};

export default Topbar;