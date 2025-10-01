using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using LiveCharts;
using LiveCharts.Wpf;
using System.IO;

namespace AnomaliesDetector
{

    public partial class MainForm : Form
    {
        private readonly string _csvPath;
        protected static readonly Dictionary<string, IPAddress> ipCache = new(StringComparer.Ordinal);  // the data from the csv imported
        //  as a string so i cast it to IPAddress type only once


        List<LogRow> LoadCsv(string path)  // a function load the csv from the path given and return a list of rows,
                                           // custom record that will contain the info of the log
        {
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                DetectDelimiter = true,
                HasHeaderRecord = true,
                PrepareHeaderForMatch = args => args.Header.Trim()
            };

            using var sr = new StreamReader(path);
            using var csv = new CsvReader(sr, cfg);  // transform the data according to the configuratioin
            return csv.GetRecords<LogRow>().ToList();
        }


        public MainForm(string csvPath)
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            _csvPath = csvPath;
        }


        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var rows = await Task.Run(() => LoadCsv(_csvPath).ToArray());
            var ct = CancellationToken.None;
            Detector[] detectors = { DetectBruteForceAttempts, DetectSuspisiousIP, DetectGeoHops, DetectHostileCountry };
            // array of the function that will search and find anomalies of the array of logs (rows)

            // Run detectors in parallel
            var resultsArrays = await Task.WhenAll(
                detectors.Select(d => Task.Run(() => d(rows, ct)))
            );
            var results = resultsArrays.SelectMany(x => x).Distinct().ToList();  // insert the anomalies into the list

  
            anomalyGrid.DataSource = results;  // bind the results to the grid
            anomalyGrid.Columns["timestamp"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";

            BuildChart(this, results);  // build the pie chart after found the anomalies

            BuildButton(this, results);  // build the button that enable to download the results into text file
        }


        static IEnumerable<Anomaly> DetectBruteForceAttempts(LogRow[] rows, CancellationToken ct)  // detect consecutive login fails
                                                                                                   // from the same user
        {
            var failsbyUser = rows.Where(r => r.action == "login_failed").GroupBy(r => r.user_id);

            foreach (var g in failsbyUser)
            {
                ct.ThrowIfCancellationRequested();

                var sortedTimes = g.Select(x => x.timestamp).OrderBy(t => t).ToList();  // sort the fails by user by the timestamp

                if (HasAtLeastKWithin(TimeSpan.FromMinutes(5), 5, sortedTimes, out var first, out var count))  // get the result from
                                                                                                               // helper function
                {
                    var firstRowInBurst = g.Where(x => x.timestamp == first).First();

                    yield return new Anomaly(  // yilding the anomalies
                        timestamp: first,
                        user_id: g.Key,
                        ip_address: "first fail with this address: " + firstRowInBurst.ip_address,
                        reason: $"Brute force: over 5 fails within a minute",
                        mitigation: "Lock account; check source IPs"
                    );
                }
            }
        }


        static IEnumerable<Anomaly> DetectSuspisiousIP(LogRow[] rows, CancellationToken ct)  // find all kinds of suspicious
                                                                                             // activities by their ip address
        {
            foreach (var r in rows)
            {
                ct.ThrowIfCancellationRequested();
                string mitigate = string.Empty;
                string classification = string.Empty;

                var ip = ParseCached(r.ip_address);

                if (ip == null) 
                {
                    mitigate = "Drop, Invalid address";
                    classification = "IP is malformed";
                }

                else if (IsBogon(ip, out var oclassification, out var omitigation))  // helper function that classify
                                                                                     // the ip and return the verdict
                {
                    mitigate = omitigation;
                    classification = oclassification;
                }

                if (mitigate.Length > 0 && classification.Length > 0)
                {

                    yield return new Anomaly(  // yilding the anomalies
                        timestamp: r.timestamp,
                        user_id: r.user_id,
                        ip_address: r.ip_address,
                        reason: $"Suspicious IP: " + classification,
                        mitigation: mitigate
                    );
                }
            }
        }


        // Detects "impossible travel" (geo-hops) by the same user
        // when login locations switch between countries in a short time window.
        static IEnumerable<Anomaly> DetectGeoHops(LogRow[] rows, CancellationToken ct)
        {
            // Group log entries by user_id so we check each user separately
            var byUser = rows.GroupBy(r => r.user_id);

            // Define the time window in which a change of country is considered impossible
            var window = TimeSpan.FromHours(3);

            foreach (var g in byUser)
            {
                ct.ThrowIfCancellationRequested(); // allow cancellation if needed

                // Sort the user's log entries by timestamp
                var ordered = g.OrderBy(x => x.timestamp).ToList();

                // Remember last seen IP, country, and timestamp
                IPAddress? prevIp = null;
                string prevCountry = "";
                DateTime prevTime = default;

                foreach (var r in ordered)
                {
                    // Try to parse the IP address
                    var ip = ParseCached(r.ip_address);
                    if (ip == null) continue; // skip invalid IPs

                    // Look up the IP’s country
                    GeoIpCountryModel cc = Geo.LookupCountryCode(ip);
                    if (cc == null) continue; // skip if no country found

                    // If no ISO code but country name is present, update context and skip detection
                    if (string.IsNullOrEmpty(cc.CountryIsoCode))
                    {
                        prevIp = ip;
                        prevCountry = cc.CountryName;
                        prevTime = r.timestamp;
                        continue;
                    }

                    // If previous country exists and differs from current,
                    // and the time between events is shorter than the allowed window,
                    // then raise an anomaly for impossible travel
                    if (!string.IsNullOrEmpty(prevCountry) &&
                        !cc.CountryName.Equals(prevCountry, StringComparison.OrdinalIgnoreCase) &&
                        (r.timestamp - prevTime) <= window)
                    {
                        yield return new Anomaly(
                            timestamp: r.timestamp,
                            user_id: r.user_id,
                            ip_address: r.ip_address,
                            reason: $"Impossible travel: from {prevCountry} to {cc.CountryName} in {(r.timestamp - prevTime).TotalMinutes:N0} min",
                            mitigation: "Step-up auth; review sessions; consider session revoke."
                        );
                    }

                    // Update "previous" values for next iteration
                    prevIp = ip;
                    prevCountry = cc.CountryName;
                    prevTime = r.timestamp;
                }
            }
        }



        // Detects events where IP addresses geolocate to hostile countries
        static IEnumerable<Anomaly> DetectHostileCountry(LogRow[] rows, CancellationToken ct)
        {
            // list of hostile/high-risk countries by ISO code
            var risky = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "IR", "CN", "RU", "SY", "LB" };

            foreach (var r in rows)
            {
                ct.ThrowIfCancellationRequested(); // support cancellation

                // Parse IP address 
                var ip = ParseCached(r.ip_address);
                if (ip == null) continue;

                // Get country info from GeoIP
                GeoIpCountryModel cc = Geo.LookupCountryCode(ip);
                if (cc == null || string.IsNullOrEmpty(cc.CountryIsoCode)) continue;

                // If the IP’s country is on the hostile list, create anomaly
                if (risky.Contains(cc.CountryIsoCode))
                {
                    yield return new Anomaly(
                        timestamp: r.timestamp,
                        user_id: r.user_id,
                        ip_address: r.ip_address,
                        reason: $"High-risk geolocation ({cc.CountryName})",
                        mitigation: "Require MFA / block per policy; review logs."
                    );
                }
            }
        }


        // Helper: parses an IP string once and caches the result for reuse
        protected static IPAddress? ParseCached(string s)
        {
            // If IP already parsed, return cached result
            if (ipCache.TryGetValue(s, out var cached))
                return cached;

            // Try to parse the string into an IP address
            if (IPAddress.TryParse(s, out var ip))
            {
                ipCache[s] = ip; // store in cache
                return ip;
            }

            return null;
        }


        // Helper: checks if there are at least K timestamps within a given time window
        static bool HasAtLeastKWithin(
            TimeSpan window,        // size of the time window, e.g. 5 minutes
            int k,                  // how many events must fall into that window
            IReadOnlyList<DateTime> times,  // sorted timestamps
            out DateTime start,     // output: first timestamp in the qualifying window
            out int count)          // output: how many timestamps fell in that window
        {
            var end = start = default;
            count = 0;
            if (times.Count == 0) return false;

            // two-pointer sliding window approach
            int i = 0;
            for (int j = 0; j < times.Count; j++)
            {
                // shrink window if it exceeds the allowed size
                while (times[j] - times[i] > window)
                    i++;

                // size of current window
                int size = j - i + 1;

                // check if it meets or exceeds K
                if (size >= k)
                {
                    start = times[i]; // mark first timestamp
                    count = size;     // number of events inside window
                    return true;
                }
            }
            return false;
        }


        private static bool IsBogon(IPAddress ip, out string classification, out string mitigation)  // check bunch of suspicious
                                                                                                     // categories and update the
                                                                                                     // classification and mitigation
        {
            classification = string.Empty;
            mitigation = string.Empty;

            if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4
            {
                var b = ip.GetAddressBytes();

                // 0.0.0.0/8
                if (b[0] == 0)
                {
                    classification = "IPv4 bogon (0.0.0.0/8)";
                    mitigation = "Drop traffic; invalid source.";
                    return true;
                }

                // 127.0.0.0/8 (loopback)
                if (b[0] == 127)
                {
                    classification = "Loopback (127/8)";
                    mitigation = "Investigate spoofing/misconfig.";
                    return true;
                }

                // 169.254.0.0/16 (link-local)
                if (b[0] == 169 && b[1] == 254)
                {
                    classification = "Link-local (169.254/16)";
                    mitigation = "Block at perimeter; valid only for local hosts.";
                    return true;
                }

                // 100.64.0.0/10 (carrier-grade NAT)
                if (b[0] == 100 && b[1] >= 64 && b[1] <= 127)
                {
                    classification = "CGNAT (100.64/10)";
                    mitigation = "Treat as untrusted; verify NAT/proxy config.";
                    return true;
                }

                // Documentation ranges
                if ((b[0] == 192 && b[1] == 0 && b[2] == 2) ||
                    (b[0] == 198 && b[1] == 51 && b[2] == 100) ||
                    (b[0] == 203 && b[1] == 0 && b[2] == 113))
                {
                    classification = "Documentation/test net";
                    mitigation = "Drop; should never appear in real traffic.";
                    return true;
                }

                // 224.0.0.0/4 (multicast)
                if (b[0] >= 224 && b[0] <= 239)
                {
                    classification = "Multicast (224/4)";
                    mitigation = "Block on WAN; only valid inside LAN.";
                    return true;
                }

                // 240.0.0.0/4 (reserved)
                if (b[0] >= 240)
                {
                    classification = "Reserved (240/4)";
                    mitigation = "Drop; not routable.";
                    return true;
                }
            }
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // IPv6 bogons
                if (ip.IsIPv6LinkLocal)
                {
                    classification = "IPv6 link-local (fe80::/10)";
                    mitigation = "Drop on WAN; only valid on local link.";
                    return true;
                }

                if (ip.IsIPv6Multicast)
                {
                    classification = "IPv6 multicast";
                    mitigation = "Drop on WAN; only valid inside LAN.";
                    return true;
                }

                if (ip.IsIPv6SiteLocal)
                {
                    classification = "IPv6 site-local (deprecated)";
                    mitigation = "Drop; legacy only.";
                    return true;
                }

                if (ip.IsIPv6UniqueLocal)
                {
                    classification = "IPv6 unique-local (fc00::/7)";
                    mitigation = "Drop if external; valid only internally.";
                    return true;
                }

                if (IPAddress.IsLoopback(ip))
                {
                    classification = "IPv6 loopback (::1)";
                    mitigation = "Investigate spoofing/misconfig.";
                    return true;
                }

                if (ip.ToString().StartsWith("2001:db8", StringComparison.OrdinalIgnoreCase))
                {
                    classification = "IPv6 documentation (2001:db8::/32)";
                    mitigation = "Drop; should never appear in real traffic.";
                    return true;
                }
            }

            var infraSuspicious = new Dictionary<string, string>
            {
                { "8.8.8.8",  "Google Public DNS" },
                { "8.8.4.4",  "Google Public DNS" },
                { "1.1.1.1",  "Cloudflare Public DNS" },
                { "1.0.0.1",  "Cloudflare Public DNS" },
                { "5.5.5.5",  "Level3" },
                { "9.9.9.9",  "Quad9 DNS" },
                { "4.2.2.2",  "Legacy Level3 DNS" }
            };

            if (infraSuspicious.TryGetValue(ip.ToString(), out var infraName))
            {
                classification = $"Public infrastructure ({infraName})";
                mitigation = "Not inherently malicious, but unusual as client source; verify logs and apply rate-limits or MFA.";
                return true;
            }

            return false;
        }

        private static void BuildChart(Form form, List<Anomaly> rows)
        {
            
            var byUser = rows  // aggregate the function by user, as users with more anomalies first
                .GroupBy(r => r.user_id)
                .Select(g => new { User = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            var top5 = byUser.Take(5).ToList();

            if (byUser.Count > 5)  // put all other users in "Other" category
            {
                var otherCount = byUser.Skip(5).Sum(x => x.Count);
                top5.Add(new { User = "Other", Count = otherCount });
            }

            var group = new GroupBox
            {
                Dock = DockStyle.Bottom,
                Width = form.ClientSize.Width / 2,
                Text = "Anomalies per User_ID" 
            };
            group.Width = 700;
            group.Height = 400;

            
            var pieChart = new LiveCharts.WinForms.PieChart
            {
                Dock = DockStyle.Fill
            };

            var pieSeries = new SeriesCollection();
            foreach (var item in top5)
            {
                {
                    pieSeries.Add(new PieSeries
                    {
                        Title = item.User ?? "(null)",
                        Values = new ChartValues<int> { item.Count },
                        DataLabels = true
                    });
                }
                pieChart.Series = pieSeries;

                form.Controls.Add(pieChart);
            }
            pieChart.Series = pieSeries;
            group.Controls.Add(pieChart);
            form.Controls.Add(group);
        }

        private static void BuildButton(Form form, List<Anomaly> rows)  // the button will be in the bottom of the screen.
        {
            var btnCreateFile = new Button
            {
                Text = "Download Anomalies chart in text file",
                Dock = DockStyle.Bottom,   
                Height = 40
            };
            form.Controls.Add(btnCreateFile);

            btnCreateFile.Click += (s, e) =>
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "Text File|*.txt",
                    FileName = "anomaliesTable.txt"
                };

                if (sfd.ShowDialog(form) == DialogResult.OK)
                {
                    
                    var lines = rows.Select(a =>
                        $"[{a.timestamp:yyyy-MM-dd HH:mm:ss}] User={a.user_id}, IP={a.ip_address}, Reason={a.reason}, Mitigation={a.mitigation}"
                    );

                    File.WriteAllLines(sfd.FileName, lines);

                    MessageBox.Show("Anomalies file created successfully!",
                                    "Success",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            };

        }

    }
}
