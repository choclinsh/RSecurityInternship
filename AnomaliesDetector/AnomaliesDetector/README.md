# Anomalies Detector (Windows Forms App)

This application can help you detect anomalies in log files, such as brute-force attacks, suspicious IP addresses, impossible travel (geo-hops), and logins from hostile countries.  
The results are shown in a simple Windows desktop interface with a table and a pie chart. You can also export anomalies to a text file.


# Features
 Load log data from a CSV file.
 Detect anomalies:
  - Too many failed logins in a short time (brute force) by the same user.
  - Malformed or suspicious IP addresses (bogons, test networks, public DNS servers).
  - Impossible travel between different countries in a short time (geo-hop). I chose 3 hours.
  - Access from **high-risk countries** (Iran, China, Russia, Syria, Lebanon).
  - Show the country for each IP using the **MaxMind GeoLite2 database**.
  - Present results:
  - **Grid/Table** with details about anomalies.
  - **Pie chart** showing how many anomalies each user caused.
  Export anomalies to a text file.
  Mitigation given to every anomaly.


# How to install
1. Make sure you have **Windows** with .NET (Framework or .NET 6+) installed.
2. Download and install the required packages (done automatically if you build in Visual Studio):
   - CsvHelper,
   - LiveCharts for the pie chart
   - MaxMind.GeoIP2
3. Download the free **GeoLite2-Country database** from MaxMind: 
   - [GeoLite2 Download](https://dev.maxmind.com/geoip/geolite2-free-geolocation-data)
   - Place the file `GeoLite2-Country.mmdb` inside a folder named `resources` in the application directory. (where it is in my project)
   - you have to have an account in order to access their service

List of Bogon / Special IP Ranges that i detect:
IPv4

0.0.0.0/8
Reason: IPv4 bogon (invalid source).
Mitigation: Drop traffic; invalid source.

127.0.0.0/8
Reason: Loopback (localhost).
Mitigation: Investigate spoofing/misconfiguration.

169.254.0.0/16
Reason: Link-local (used only when DHCP fails).
Mitigation: Block at perimeter; valid only for local hosts.

100.64.0.0/10
Reason: Carrier-grade NAT (CGNAT).
Mitigation: Treat as untrusted; verify NAT/proxy configuration.

192.0.2.0/24, 198.51.100.0/24, 203.0.113.0/24
Reason: Documentation/test networks.
Mitigation: Drop; should never appear in real traffic.

224.0.0.0/4
Reason: Multicast range.
Mitigation: Block on WAN; only valid inside LAN.

240.0.0.0/4
Reason: Reserved range.
Mitigation: Drop; not routable.

IPv6

fe80::/10
Reason: IPv6 link-local.
Mitigation: Drop on WAN; only valid on local link.

IPv6 multicast addresses
Reason: IPv6 multicast.
Mitigation: Drop on WAN; only valid inside LAN.

IPv6 site-local (deprecated)
Reason: Legacy addresses.
Mitigation: Drop; legacy only.

fc00::/7
Reason: IPv6 unique-local.
Mitigation: Drop if external; valid only internally.

::1
Reason: IPv6 loopback.
Mitigation: Investigate spoofing/misconfiguration.

2001:db8::/32
Reason: IPv6 documentation/test range.
Mitigation: Drop; should never appear in real traffic.	