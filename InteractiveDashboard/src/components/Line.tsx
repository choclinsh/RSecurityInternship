import { Line } from "react-chartjs-2";
import { useEffect, useState } from "react";
import {
  Chart as ChartJS,
  LineElement,
  CategoryScale,
  LinearScale,
  PointElement,
  Title,
  Tooltip,
  Legend,
} from "chart.js";

ChartJS.register(
  LineElement,
  CategoryScale,
  LinearScale,
  PointElement,
  Title,
  Tooltip,
  Legend
);

type LineDataPayload = {
  labels: string[];
  values: number[];
  title?: string;
};

//  Renders a time series of counts per day.
// Expects localStorage key "line-data" with:
// { labels: string[], values: number[], title?: string }
// Typically generated from the 'timestamp' column (YYYY-MM-DD portion).

export default function LineChart() {
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

  const [payload, setPayload] = useState<LineDataPayload | null>(null);
  useEffect(() => {
    const raw = localStorage.getItem("line-data");
    setPayload(raw ? JSON.parse(raw) : null);
  }, []);

  if (!payload) {
    return <div style={{ padding: 24 }}>No line data found</div>;
  }

  const backgroundColors = payload.labels.map(
    (_, i) => COLORS[i % COLORS.length]
  );

  const data = {
    labels: payload.labels,
    datasets: [
      {
        label: "Count",
        backgroundColor: "#d81212ff",
        borderColor: backgroundColors,
        fill: false,
        data: payload.values,
      },
    ],
  };

  const options = {
    responsive: true,
    plugins: {
      title: { display: !!payload.title, text: payload.title || "" },
      legend: { display: true },
    },
  };

  return (
    <div style={{ maxWidth: 900, margin: "40px auto" }}>
      <Line data={data} options={options} />
    </div>
  );
}
