# WebSocket Client

Connects Peakboard to any WebSocket server (`ws://` or `wss://`) — Node-RED, OCPP back ends,
GraphQL subscriptions, machine gateways, home-grown services. Incoming messages appear live as rows
of a table; Lua scripts and Building Blocks send messages over the same connection.

The data source keeps the connection open for as long as the board runs, reconnects on its own
after a drop, and keeps the WebSocket alive with a protocol-level heartbeat.

## Installation

1. Download [`Binary/WebSocketClient.zip`](Binary/WebSocketClient.zip).
2. In Peakboard Designer: **Data → Add data source → Manage extensions → Add custom extension**,
   pick the ZIP, restart the Designer.
3. **Add data source → Extensions → WebSocket Client → WebSocket**.

The data source is a push data source: the Designer sets its reload state to **Push**, because
every message reaches the table the moment it arrives — a periodic reload is not needed.

## Properties

| Property | Default | Meaning |
|---|---|---|
| `WsUrl` | `ws://127.0.0.1:1880/ws` | Complete WebSocket address, `ws://` or `wss://`. |
| `CustomHeaders` | *(empty)* | Extra HTTP headers for the handshake: `Key1:Val1;Key2:Val2`, e.g. `Authorization:Bearer xyz`. Only the first `:` separates key and value. Masked, because it usually carries a token. |
| `SubProtocol` | *(empty)* | WebSocket sub-protocol, e.g. `ocpp1.6` or `graphql-ws`. Several, comma-separated, are offered in that order. |
| `IgnoreSslErrors` | `False` | Accept self-signed or otherwise invalid certificates on `wss://`. Only for trusted internal networks. |
| `BufferMode` | `RingBuffer` | `RingBuffer` keeps the newest `MaxRows` messages; `SingleRow` always overwrites row 1 with the latest state. |
| `MaxRows` | `100` | Rows kept in `RingBuffer` mode (1 – 100000). |
| `ParseTopLevelJson` | `False` | `False`: fixed envelope columns. `True`: one column per top-level JSON key (see below). |
| `SampleJson` | `{}` | Example message for `ParseTopLevelJson = True`, so the columns exist before the first live message. `{}` means "take the first live message". |
| `IncludeSentMessages` | `False` | Also log messages Peakboard sends (`TX` rows). Envelope mode only. |
| `OnConnectPayload` | *(empty)* | Text/JSON sent right after every successful connect — also after a reconnect (e.g. a subscribe command). |
| `AutoReconnect` | `True` | Reconnect automatically after the connection drops or cannot be opened. |
| `ReconnectIntervalMs` | `5000` | Wait before a reconnect attempt, in milliseconds. A single connect attempt gives up after 10 s (a host that silently drops packets would otherwise hold it for ~21 s). |
| `KeepAliveSeconds` | `30` | Protocol-level heartbeat interval; `0` switches it off. |

## Columns

### Envelope mode (`ParseTopLevelJson = False`)

Works with every payload — JSON, CSV, plain text.

| Column | Type | Content |
|---|---|---|
| `SeqId` | Number | Running number since the data source started (1, 2, 3 …). |
| `Timestamp` | String | Local time, `yyyy-MM-dd HH:mm:ss.fff`. |
| `Direction` | String | `RX` received, `TX` sent, `SYS` connection status. |
| `ConnectionState` | String | `Connecting`, `Connected`, `Disconnected` or `Error`. |
| `Payload` | String | The message text; for `SYS` rows a description (e.g. the error). |

In `SingleRow` mode a connection change only updates `ConnectionState` of row 1, so the last
payload stays visible.

### Dynamic JSON mode (`ParseTopLevelJson = True`)

`SeqId`, `Timestamp` and `ConnectionState`, plus one column per top-level key of `SampleJson` —
or, without a sample, of the first JSON message that arrives:

`{"machine": "CNC-01", "temp": 42.5, "alarm": false}` → `machine` (String), `temp` (Number),
`alarm` (Boolean).

- A message that is a JSON **array** of objects becomes one row per object.
- Nested objects/arrays land as compact JSON text in a String column.
- A key missing in a message gives `""` / `0` / `false`; keys not in the schema are ignored.
- Messages that are not JSON objects are skipped (and logged).
- Connection changes create no row here; in `SingleRow` they update `ConnectionState` of row 1.

Set `SampleJson` whenever you can: the Designer then knows the columns without a live server, and
the columns do not depend on which message happens to arrive first.

**Load data in the Designer:** while the board is not running, the Designer connects for about
3 seconds and shows what arrived (in dynamic mode without a sample it waits up to 5 seconds for a
first JSON message to derive the columns from).

## Functions

| Function | Parameters | Returns | Meaning |
|---|---|---|---|
| `SendText` | `Message` (String) | `Success` (Boolean) | Sends any string or JSON. |
| `SendKeyValueJson` | `Key`, `Value` (String) | `Success` (Boolean) | Sends `{"<Key>": "<Value>"}`, correctly escaped. |
| `ClearBuffer` | – | – | Empties the table immediately. |
| `Reconnect` | – | – | Closes the session cleanly and connects again right away (also resumes after `Disconnect`). |
| `Disconnect` | – | – | Closes the WebSocket and pauses `AutoReconnect` until `Reconnect` is called. |

`Success` is `false` when no connection is open — nothing is queued.

```lua
data.MyWebSocket:SendText('{"cmd":"subscribe","topic":"line1"}')
data.MyWebSocket:SendKeyValueJson('setpoint', '42')
data.MyWebSocket:ClearBuffer()
```

## Notes

- Binary frames are read as UTF-8 text.
- Connection errors land in a `SYS` row (`ConnectionState = Error`) with the actual cause, e.g.
  `The remote certificate is invalid because of errors in the certificate chain: UntrustedRoot.
  For a self-signed certificate set IgnoreSslErrors to True.`
- The short **Load data** connection in the Designer is a real connection: it also sends
  `CustomHeaders`, `SubProtocol` and `OnConnectPayload`.
- In the dynamic mode without `SampleJson`, the Designer takes the columns from the first message it
  sees, and the running board takes them from the first message after its start. If differently
  shaped messages arrive on the channel, set `SampleJson` so both use the same columns.
- On .NET 8 the heartbeat is sent as an unsolicited Pong frame (RFC 6455 allows this as a one-way
  heartbeat). It keeps proxies and NAT routers from dropping an idle connection; a dead server is
  detected when the TCP connection fails.
