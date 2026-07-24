import { useEffect, useState } from "react";
import "./AttendanceChart.css";

import {
  Chart as ChartJS,
  ArcElement,
  Tooltip,
  Legend,
} from "chart.js";

import { Doughnut } from "react-chartjs-2";
import { emsApi } from "../../services/emsApi";

ChartJS.register(
  ArcElement,
  Tooltip,
  Legend
);

const options = {
  responsive: true,

  maintainAspectRatio: false,

  cutout: "72%",

  plugins: {
    legend: {
      position: "bottom",

      labels: {
        usePointStyle: true,

        padding: 20,
      },
    },
  },
};

const AttendanceChart = () => {
  const [summary, setSummary] = useState({
    presentEmployees: 0,
    absentEmployees: 0,
    pendingLeaves: 0,
    totalEmployees: 0,
  });

  useEffect(() => {
    loadAttendanceSummary();
  }, []);

  const loadAttendanceSummary = async () => {
    try {
      const response = await emsApi.dashboardSummary();

      setSummary({
        presentEmployees: response.presentEmployees || 0,
        absentEmployees: response.absentEmployees || 0,
        pendingLeaves: response.pendingLeaves || 0,
        totalEmployees: response.totalEmployees || 0,
      });
    } catch (error) {
      console.error("Failed to load attendance summary:", error);
    }
  };

  const data = {
    labels: ["Present", "Absent", "Leave"],

    datasets: [
      {
        data: [
          summary.presentEmployees,
          summary.absentEmployees,
          summary.pendingLeaves,
        ],

        backgroundColor: [
          "#10B981",
          "#EF4444",
          "#F59E0B",
        ],

        borderWidth: 0,
      },
    ],
  };

  return (
    <div className="attendance-card">
      <div className="attendance-header">
        <h4>Today's Attendance</h4>

        <span>{summary.totalEmployees} Employees</span>
      </div>

      <div className="attendance-chart">
        <Doughnut
          data={data}
          options={options}
        />
      </div>
    </div>
  );
};

export default AttendanceChart;