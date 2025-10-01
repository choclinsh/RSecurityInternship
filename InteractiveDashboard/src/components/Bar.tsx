import { useEffect, useState } from "react";
import { Bar } from "react-chartjs-2";
import {
  Chart as ChartJS,
  BarElement,
  CategoryScale,
  LinearScale,
  Title,
  Tooltip,
  Legend,
} from "chart.js";

ChartJS.register(
  BarElement,
  CategoryScale,
  LinearScale,
  Title,
  Tooltip,
  Legend
);

type BarDataPayload = {
  labels: string[];
  values: number[];
  title?: string;
};

// Renders a categorical frequency chart using Chart.js via react-chartjs-2.
// Expects localStorage key "bar-data" with:
// { labels: string[], values: number[], title?: string }

export default function BarChart() {
  const COLORS = [
    "#FF6384",
    "#36A2EB",
    "#FFCE56",
    "#4BC0C0",
    "#9966FF",
    "#FF9F40",
    "#C9CBCF",
    "#8DD17E",
    "#E377C2",
    "#17BECF",
    "#FFD700",
    "#A52A2A",
  ];

  const [payload, setPayload] = useState<BarDataPayload | null>(null);

  useEffect(() => {
    // Defines and stores the data shape expected from localStorage
    const raw = localStorage.getItem("bar-data");
    if (raw) {
      // On mount, load and parse the data. If missing/invalid, show a message.
      try {
        setPayload(JSON.parse(raw));
      } catch {
        setPayload(null);
      }
    }
  }, []);

  if (!payload) {
    return <div style={{ padding: 24 }}>No bar data found.</div>;
  }

  const backgroundColors = payload.labels.map(
    (_, i) => COLORS[i % COLORS.length]
  );

  const data = {
    labels: payload.labels,
    datasets: [
      {
        label: "Count",
        backgroundColor: backgroundColors,
        borderWidth: 1,
        hoverOffset: 5,
        data: payload.values,
      },
    ],
  };

  const options = {
    responsive: true,
    plugins: {
      title: { display: !!payload.title, text: payload.title || "" },
      legend: { display: false },
    },
  };

  return (
    <div style={{ maxWidth: 900, margin: "40px auto" }}>
      <Bar data={data} options={options} />
    </div>
  );
}
