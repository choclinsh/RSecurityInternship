import { Pie } from "react-chartjs-2";
import { useEffect, useState } from "react";
import { Chart as ChartJS, Tooltip, Legend, ArcElement } from "chart.js";

ChartJS.register(Tooltip, Legend, ArcElement);

// Renders a categorical distribution as a pie chart.
// Expects localStorage key "pie-data" with:
// { labels: string[], values: number[], title?: string }
// Uses a small fixed color palette cycling by index.

type PieDataPayload = {
  labels: string[];
  values: number[];
  title?: string;
};

export default function PieChart() {
  // Builds a background color array by cycling a predefined palette
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

  const [payload, setPayload] = useState<PieDataPayload | null>(null);
  useEffect(() => {
    const raw = localStorage.getItem("pie-data");
    if (raw) {
      try {
        setPayload(JSON.parse(raw));
      } catch {
        setPayload(null);
      }
    }
  }, []);

  if (!payload) {
    return <div style={{ padding: 24 }}>No pie data found</div>;
  }

  const backgroundColors = payload.labels.map(
    (_, i) => COLORS[i % COLORS.length]
  );

  const data = {
    labels: payload.labels,
    datasets: [
      {
        label: "Count",
        data: payload.values,
        backgroundColor: backgroundColors,
        hoverOffset: 5,
        borderColor: "#fff",
        borderWidth: 1,
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
      <Pie data={data} options={options} />
    </div>
  );
}
