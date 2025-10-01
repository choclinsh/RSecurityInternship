import React, { useState } from "react";
import "./App.css";
import { useEffect } from "react";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import LineChart from "./components/Line";
import BarChart from "./components/Bar";
import PieChart from "./components/Pie";

function App() {
  const [file, setFile] = useState<File | undefined>(undefined); // the selected CSV file object
  const [array, setArray] = useState<CSVRow[]>([]); // parsed CSV data as array of objects
  const [filteredArray, setFilteredArray] = useState<CSVRow[]>([]); // filtered data for display
  const [csvImported, setCsvImported] = useState(false); // toggles dashboard actions once data exists.
  const [sortKey, setSortKey] = useState<string>(""); // user inputs for sorting/searching.
  const [searchKey, setSearchKey] = useState<string>(""); // user inputs for sorting/searching.

  const fileReader = new FileReader();
  useEffect(() => {
    setFilteredArray(array);
  }, [array]);

  const handleOnChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0]); // Stores the file the user picked.
    }
  };

  type CSVRow = Record<string, string>; // row object type

  const csvFileToArray = (csvString: string) => {
    // Parses the CSV manually: firstline is the header and the rest are rows.
    const csvHeader: string[] = csvString
      .slice(0, csvString.indexOf("\n"))
      .split(",");
    const csvRows: string[] = csvString
      .slice(csvString.indexOf("\n") + 1)
      .split("\n")
      .filter((r) => r.trim().length > 0);

    const array: CSVRow[] = csvRows.map((row) => {
      const values = row.split(",");
      const obj = csvHeader.reduce<Record<string, string>>(
        (object, header, index) => {
          object[header] = values[index];
          return object;
        },
        {}
      );
      return obj;
    });

    setArray(array);
    setFilteredArray(array);
  };

  const handleOnSubmit = (
    // called when IMPORT CSV button is clicked
    e: React.MouseEvent<HTMLButtonElement, MouseEvent>
  ) => {
    e.preventDefault();

    if (file) {
      fileReader.onload = function (event) {
        const text = event.target && event.target.result;
        if (typeof text === "string") {
          csvFileToArray(text);
          setCsvImported(true);
        }
      };

      fileReader.readAsText(file);
    }
  };

  function makeBarPieData(rows: CSVRow[]) {
    // counts by "action" column. Will be presented in 2 ways: bar and pie.
    const counts: Record<string, number> = {};
    for (const r of rows) {
      const a = r.action ?? "(unknown)";
      counts[a] = (counts[a] || 0) + 1;
    }
    const labels = Object.keys(counts);
    const values = labels.map((l) => counts[l]);
    return { labels, values, title: "Events by Action" };
  }

  function makeLineData(rows: CSVRow[]) {
    //  counts by "timestamp" column (date part only)
    const counts: Record<string, number> = {};

    for (const r of rows) {
      const ts = (r.timestamp ?? "").trim();
      if (!ts) continue;
      // take the date part (YYYY-MM-DD) robustly
      const day = ts.split(/\s+/)[0];
      if (!day) continue;
      counts[day] = (counts[day] || 0) + 1;
    }

    // sort the labels chronologically
    const labels = Object.keys(counts).sort(
      (a, b) => new Date(a).getTime() - new Date(b).getTime()
    );
    const values = labels.map((d) => counts[d]);

    return { labels, values, title: "Events over Time" };
  }

  function sortArrayByKey() {
    // Sorts by chosen column
    if (!sortKey) return;

    // validate the column name, should be one of the header keys
    if (!headerKeys.includes(sortKey)) {
      alert(
        `Column "${sortKey}" not found. Available: ${headerKeys.join(", ")}`
      );
      return;
    }

    const sorted = [...array].sort((a, b) => {
      const av = (a[sortKey] ?? "").trim();
      const bv = (b[sortKey] ?? "").trim();

      // try date first (e.g., timestamp)  type detection
      const ad = Date.parse(av);
      const bd = Date.parse(bv);
      if (!Number.isNaN(ad) && !Number.isNaN(bd)) return ad - bd;

      // then try numeric
      const an = Number(av),
        bn = Number(bv);
      if (!Number.isNaN(an) && !Number.isNaN(bn)) return an - bn;

      // fallback to string (natural) compare
      return av.localeCompare(bv, undefined, {
        numeric: true,
        sensitivity: "base",
      });
    });

    setArray(sorted);
  }

  function searchArrayByKey() {
    // show the rows containing the keyword in any column
    if (!searchKey) return;

    const q = searchKey.trim().toLowerCase();

    if (!q) {
      // reset: show original imported CSV
      setFilteredArray(array);
      return;
    }

    const filtered = array.filter((row) =>
      Object.values(row).some((val) => val?.toLowerCase().includes(q))
    );
    setFilteredArray(filtered);
  }

  const headerKeys = Object.keys(Object.assign({}, ...array));

  return (
    <Router>
      <Routes>
        {/* Dashboard route (shows upload + table + open button) */}
        <Route
          path="/"
          element={
            <div style={{ textAlign: "center", color: "#136bdfff" }}>
              <h1 style={{ color: "#0d376dff" }}>Web Dashboard</h1>

              <form>
                <input
                  type="file"
                  id="csvFileInput"
                  accept=".csv"
                  onChange={handleOnChange}
                />
                <button
                  className="btn tip"
                  data-tip="Imports the CSV and places it in a table"
                  onClick={handleOnSubmit}
                >
                  IMPORT CSV
                </button>
              </form>

              <br />

              {csvImported && ( // these buttons only appear with the table once a csv is imported
                <button // open bar chart in new tab
                  className="btn tip"
                  data-tip="Open a bar chart in a pop up window that shows counts of logs by type of action"
                  onClick={() => {
                    const barPayload = makeBarPieData(array);
                    localStorage.setItem(
                      "bar-data",
                      JSON.stringify(barPayload)
                    );
                    window.open("/bar", "_blank");
                  }}
                >
                  Open Bar Chart
                </button>
              )}

              {csvImported && (
                <button // open line chart in new tab
                  className="btn tip"
                  data-tip="Open a line chart in a pop up window that shows counts of logs by time (day)"
                  onClick={() => {
                    const linePayload = makeLineData(array);
                    localStorage.setItem(
                      "line-data",
                      JSON.stringify(linePayload)
                    );
                    window.open("/line", "_blank");
                  }}
                >
                  Open Line Chart
                </button>
              )}

              {csvImported && (
                <button // open pie chart in new tab
                  className="btn tip"
                  data-tip="Open a pie chart in a pop up window that shows counts of logs by type of action"
                  onClick={() => {
                    const piePayload = makeBarPieData(array);
                    localStorage.setItem(
                      "pie-data",
                      JSON.stringify(piePayload)
                    );
                    window.open("/pie", "_blank");
                  }}
                >
                  Open Pie Chart
                </button>
              )}

              {csvImported && ( // search box. the value will be the search keyword
                <div style={{ marginTop: 12 }}>
                  <input
                    type="text"
                    value={searchKey}
                    onChange={(e) => setSearchKey(e.target.value)}
                    placeholder={`search by keyword (to default press space and Search)`}
                    style={{ padding: 6, width: 398, marginRight: 8 }}
                  />
                  <button // button that provoke the search action
                    className="btn tip"
                    data-tip="Searches all columns for the keyword. To reset, press space in the box and press Search."
                    onClick={(e) => {
                      e.preventDefault();
                      searchArrayByKey();
                    }}
                  >
                    Search
                  </button>
                </div>
              )}

              {csvImported && ( // sort box. the value will be the column name to sort by
                <div style={{ marginTop: 12 }}>
                  <input
                    type="text"
                    value={sortKey}
                    onChange={(e) => setSortKey(e.target.value)}
                    placeholder={`column to sort by (e.g. ${
                      headerKeys[0] ?? "action"
                    })`}
                    style={{ padding: 6, width: 410, marginRight: 8 }}
                  />
                  <button // button that provoke the sort action
                    className="btn tip"
                    data-tip="Sorts the table by the specified column"
                    onClick={(e) => {
                      e.preventDefault();
                      sortArrayByKey();
                    }}
                  >
                    Sort
                  </button>
                </div>
              )}

              <br />

              <table>
                <thead>
                  <tr key="header">
                    {headerKeys.map(
                      (
                        key // table header
                      ) => (
                        <th key={key}>{key}</th>
                      )
                    )}
                  </tr>
                </thead>
                <tbody>
                  {filteredArray.map(
                    (
                      item,
                      i // the table body, the current state is the filtered array
                    ) => (
                      <tr key={i}>
                        {Object.values(item).map((val, j) => (
                          <td key={j}>{val}</td>
                        ))}
                      </tr>
                    )
                  )}
                </tbody>
              </table>
            </div>
          }
        />

        {/* Chart-only route (no upload UI) */}
        <Route path="/bar" element={<BarChart />} />
        <Route path="/line" element={<LineChart />} />
        <Route path="/pie" element={<PieChart />} />
      </Routes>
    </Router>
  );
}

export default App;
