import { useEffect, useState } from "react";
import "./ActivityChart.css";

import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Tooltip,
  Legend,
  Filler,
} from "chart.js";

import { Line } from "react-chartjs-2";
import { emsApi } from "../../services/emsApi";

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Tooltip,
  Legend,
  Filler
);

const options = {
  responsive: true,

  maintainAspectRatio: false,

  plugins: {
    legend: {
      display: false,
    },
  },

  scales: {
    y: {
      beginAtZero: true,
      max: 100,
    },
  },
};

const ActivityChart = () => {
  const [chartData, setChartData] = useState({
    labels: ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
    values: [0, 0, 0, 0, 0, 0, 0],
  });

  useEffect(() => {
    loadActivityChart();
  }, []);

  const loadActivityChart = async () => {
    try {
      const response = await emsApi.attendanceChart();

      if (
        response &&
        Array.isArray(response.labels) &&
        Array.isArray(response.values)
      ) {
        setChartData({
          labels: response.labels,
          values: response.values,
        });
      }
    } catch (error) {
      console.error("Failed to load activity chart:", error);
    }
  };

  const data = {
    labels: chartData.labels,

    datasets: [
      {
        label: "Employee Activity",

        data: chartData.values,

        borderColor: "#F79A1E",

        backgroundColor: "rgba(247,154,30,.15)",

        fill: true,

        tension: 0.4,

        pointRadius: 4,
      },
    ],
  };

  return (
    <div className="activity-card">
      <div className="activity-header">
        <div>
          <h4>Employee Activity Overview</h4>

          <p>Last 7 Days</p>
        </div>

        <button>Weekly</button>
      </div>

      <div style={{ height: "350px" }}>
        <Line
          data={data}
          options={options}
        />
      </div>
    </div>
  );
};

export default ActivityChart;