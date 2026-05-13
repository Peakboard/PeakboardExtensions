# Peakboard Extension: Ping

This extension lets Peakboard ping a device (by IP address or hostname) and report whether it is reachable. Each refresh executes an ICMP echo request and returns a single row with the device and its status.

## Data Sources

### Ping

Sends an ICMP echo request (timeout 2 seconds) to each configured device and returns one row per device.

| Parameter | Description |
|-----------|-------------|
| Device | One or more IP addresses or hostnames to ping, separated by commas. Example: `192.168.0.1, example.com, 8.8.8.8` |

## Output Columns

The result set contains one row per device.

| Column | Type | Description |
|--------|------|-------------|
| Device | String | The device that was pinged (trimmed value from the parameter) |
| Result | String | `OK` if the device responded successfully, `NOK` otherwise |

## Notes

- The ping uses a 2 second timeout. Devices that do not respond within this window are reported as `NOK`.
- Sending ICMP packets may require elevated privileges or firewall rules on the host running Peakboard.
- Any DNS resolution failure, network error, or non-success ping reply is reported as `NOK`.
