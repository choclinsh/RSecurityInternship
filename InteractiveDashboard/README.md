# Interactive CSV Dashboard

A small React + Chart.js app that imports a CSV, shows a searchable/sortable table, and opens dedicated chart pages (Bar/Line/Pie) in new tabs.
To run go to the cmd, go to project folder: cd "path", ensure you got npm installed and then run with npm run dev. Then paste the url to your browser. After that import the csv file thats on
the project folder- sample_logs_no_status.csv

1. **Import CSV** on `/`:

   First line is treated as headers; remaining lines are data rows.
   The table is client-side rendered and can be searched by keyword and sorted.
   The search will present only the logs that contain the keyword. (Could be in any column).
   The sorting can be based on every column as well.
   Hover tooltips appear over the buttons.

2. **Open charts**:
   Click "Open Bar/Line/Pie Chart". The dashboard computes `{labels, values, title}` and stores it in `localStorage` under:
   `"bar-data"`, `"line-data"`, or `"pie-data"`.
   A new tab opens at `/bar`, `/line`, or `/pie` where the chart component reads the payload and renders with Chart.js.

**Bar/Pie**: `action` Present in a different way how much logs happen per action type.
**Line**: `timestamp` Present how much logs happen per day.

## Limitations

- CSV parsing is naive (no quoted-field support).
- Data is passed via `localStorage'.
- Search dont support words with

## Tech

- React, React Router
- Chart.js + react-chartjs-2
