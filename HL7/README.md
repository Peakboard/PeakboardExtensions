# Peakboard Extension: HL7

Receives **HL7 v2** messages from a hospital information system, lab system or
interface engine (Mirth/NextGen Connect, Cloverleaf, Rhapsody, Orion …) and shows
them on a board. The Peakboard Box acts as an **MLLP server**, the TCP transport
every HL7 v2 interface speaks, and acknowledges each message.

You choose what to keep:

- **Message types**, e.g. only `ADT` (admissions, transfers, discharges) or only `ORU^R01` (results)
- **Segment types**, e.g. only `OBX` (observations). Every row says which message and which patient it belongs to
- **Patients**, by patient ID, with wildcards

No third-party libraries: the package is one DLL plus `Extension.xml`.

## Data sources

Two, sharing one listener per port. Add the ones you need with the same `Port`;
the port opens with the first and closes with the last.

### HL7 - Segments

**One row per stored segment.** The best fit for tables: with `KeepMshPid` off,
`SegmentTypes = OBX` gives one row per observation and `SegmentTypes = PV1` one row
per ADT event (location in `Field3`), each carrying the patient.

| Property | Default | Description |
|---|---|---|
| `Port` | `2575` | TCP port to listen on. 2575 is the IANA port for HL7 over MLLP. |
| `Encoding` | `UTF-8` | `UTF-8`, `ISO-8859-1` or `ASCII`. Ask the sender; older systems often send ISO-8859-1. |
| `MessageTypes` | *(all)* | See [Filters](#filters). |
| `SegmentTypes` | *(all)* | See [Filters](#filters). |
| `PatientIds` | *(all)* | See [Filters](#filters). |
| `KeepMshPid` | `true` | Store the MSH and PID of every message as rows of their own. Turn off to get only the segments `SegmentTypes` names; their rows still carry the message and patient columns. HL7 - Segments only. |
| `Mode` | `History` | `History` keeps the received rows; `Latest` shows only the newest message and overwrites it. See [Rows and retention](#rows-and-retention). |
| `MaxRows` | `1000` | The oldest rows are removed beyond this. Ignored in `Latest` mode. |

| Column | Type | Description |
|---|---|---|
| `ReceivedAt` | String | When the Box received the message, `yyyy-MM-dd HH:mm:ss`. |
| `MessageControlId` | String | MSH-10. Groups the rows of one message. |
| `MessageType` | String | `ADT^A01`, `ORU^R01` … |
| `MessageCode`, `TriggerEvent` | String | The two halves: `ADT` and `A01`. |
| `SendingApplication`, `SendingFacility` | String | MSH-3 and MSH-4. |
| `PatientId` | String | PID-3.1, the first patient identifier. |
| `PatientName` | String | PID-5 as `Family, Given Middle`. |
| `SegmentType` | String | `MSH`, `PID`, `OBX` … |
| `SegmentIndex` | Number | Position in the message; MSH is 1. |
| `Field1` … `Field20` | String | The segment's fields **by HL7 number**: OBX-5 is `Field5`, MSH-9 is `Field9`. A plain value has escape sequences resolved; a field with components (`HGB^Haemoglobin`) is shown as received. |
| `Segment` | String | The whole segment as received. |

For `OBX` the useful fields are `Field3` (observation ID^text), `Field5` (value),
`Field6` (units), `Field7` (reference range), `Field8` (abnormal flag), `Field11`
(result status) and `Field14` (observation time).

### HL7 - Messages

**One row per message**, with the MSH and PID lines stored as received and their
common fields already picked out.

Properties as above; `MaxRows` defaults to `200`.

| Column | Type | Description |
|---|---|---|
| `ReceivedAt`, `MessageControlId`, `MessageType`, `MessageCode`, `TriggerEvent` | String | As above. |
| `Version` | String | MSH-12, e.g. `2.5`. |
| `MessageDateTime` | String | MSH-7 as sent, e.g. `20260929101500`. |
| `SendingApplication`, `SendingFacility`, `ReceivingApplication`, `ReceivingFacility` | String | MSH-3 to MSH-6. |
| `PatientId`, `PatientName` | String | As above. |
| `PatientBirthDate`, `PatientSex` | String | PID-7 and PID-8. |
| `SegmentCount` | Number | Number of stored segments. |
| `SegmentTypes` | String | Their types in order, e.g. `MSH,PID,OBX,OBX`. |
| `MSH` | String | The MSH line. |
| `PID` | String | The PID line, empty if the message has none. |
| `Segments` | String | The stored segments, one per line. |
| `RemoteEndpoint` | String | IP and port of the sender. |

## Filters

All three are comma- or semicolon-separated lists and case-insensitive. Empty means
no restriction. They combine: a message must pass all of them.

| Property | Examples | Matches |
|---|---|---|
| `MessageTypes` | `ADT` · `ADT^A01, ADT^A03` · `ORU_R01` · `ADT, ORU` | MSH-9. `ADT` matches every ADT event; `ADT^A01` only admissions. `ADT_A01` is accepted too. |
| `SegmentTypes` | `OBX` · `OBR, OBX, NTE` · `PV1` | Which segments are stored. MSH and PID are stored in addition unless `KeepMshPid` is off. |
| `PatientIds` | `4711` · `4711, 4712` · `47*` · `A-???` | The patient identifiers in PID-3 (every repetition), PID-2 and PID-4. `*` and `?` are wildcards. |

Two rules worth knowing:

- **With `SegmentTypes` set, a message containing none of those segments is dropped
  entirely**, not stored as a lone MSH and PID. `SegmentTypes = OBX` means "results";
  an ADT without results leaves nothing behind. Likewise, with `KeepMshPid` off and
  `SegmentTypes` empty, a message of nothing but MSH and PID leaves nothing behind.
- **With `PatientIds` set, messages without a PID segment are dropped**, since they
  cannot be attributed to a patient.

Filtering decides what the board stores, not what it accepts: **every well-formed
message is acknowledged with `AA`**, including the ones filtered out. A negative
acknowledgement would make the sender queue and retry messages the board is simply
not interested in. Text that is not HL7 at all is answered with `AR`. Incoming
`ACK` messages are never acknowledged.

## Lua functions

Available on HL7 - Segments and HL7 - Messages.

| Function | Returns | Description |
|---|---|---|
| `GetValue(text, path)` | String | Reads one value from a message or a single segment, escape sequences resolved. See below. |
| `SetPatientIds(ids)` | – | Replaces the `PatientIds` filter at runtime, e.g. from a patient picker. Applies to messages received afterwards; call `Clear` first to drop the other patients' rows. Pass `''` for all patients. |
| `ProcessMessage(message)` | String | Runs a message through this list's filter as if it had been received: `OK n` (rows added), `FILTERED`, or the parse error. For testing a board. |

`GetValue` paths:

| Path | Means |
|---|---|
| `PID-5.1` | PID field 5, component 1 (family name) |
| `MSH-9.2` | Trigger event |
| `OBX[2]-5` | Field 5 of the **second** OBX |
| `PID-3(2).1` | Component 1 of the **second repetition** of PID-3 |
| `5.1` | Field 5, component 1 of a single segment, e.g. a row's `Segment` column |

```lua
-- Data sources named HL7Segments (SegmentTypes = OBX) and HL7Messages.

-- The observation's text from an OBX row
local row = data.HL7Segments[0]
local text = data.HL7Segments.GetValue(row.Segment, '3.2')   -- "Haemoglobin"

-- The patient's second identifier from a message row
local msg = data.HL7Messages[0]
local insuranceId = data.HL7Messages.GetValue(msg.PID, 'PID-3(2).1')

-- The visit number, if SegmentTypes keeps PV1
local visit = data.HL7Messages.GetValue(msg.Segments, 'PV1-19')

-- Patient picker
data.HL7Segments.Clear()
data.HL7Segments.SetPatientIds(screens['Main'].PatientBox.Text)
```

## Rows and retention

The lists are push lists. **New rows are appended at the bottom**; when `MaxRows` is
reached the oldest row is removed from the top. Sort a table by `ReceivedAt`
descending to show the newest first.

With `Mode` = `Latest` the list holds only the newest accepted message. On HL7 -
Messages that is one row, overwritten by every new message. On HL7 - Segments it
is the segments of that message.

Rows live in memory. They are gone after a restart of the board, and they are not
there in Designer until the listener has received something (or you call
`ProcessMessage`).

Loading the data source preview in Designer opens the port for 10 seconds and shows
the messages that arrive in that time and pass the filter. Send one with
`Send-HL7.ps1` (see below) while the preview loads. The sender gets its ACK as
usual, so these messages do not reach a board on another machine.

## Setting up

1. Add one of the lists with the desired `Port`.
2. Allow the port inbound in the firewall of the machine running the board. On a
   Peakboard Box, ask your administrator; on a PC, Windows Firewall will ask on the
   first run.
3. Configure the sending system with an **MLLP / TCP client** connection to the
   Box's IP and port. Most engines call this an "LLP Sender" or "TCP Sender" with
   MLLP framing.
4. Check the connection from the sending system's side: interface engines show
   whether their MLLP connection is up and whether messages are acknowledged.

If the port cannot be opened, the data source fails with `Cannot listen on port …`
instead of staying empty. The listener runs wherever the board runs. **Designer and
Runtime on the same PC cannot both open the same port**; close the Designer preview
or use another port for testing.

A list that stays empty while the sender reports acknowledged messages is filtering
them out. Clear `MessageTypes`, `SegmentTypes` and `PatientIds` to check, or test a
message with `ProcessMessage`, which returns `FILTERED`.

### Testing without a sending system

`SourceCodeNew\Send-HL7.ps1` sends a sample message and prints the ACK:

```powershell
powershell -ExecutionPolicy Bypass -File SourceCodeNew\Send-HL7.ps1                               # ADT^A01, patient 4711, to 127.0.0.1:2575
powershell -ExecutionPolicy Bypass -File SourceCodeNew\Send-HL7.ps1 -Sample ORU -PatientId 815    # ORU^R01 with two OBX
powershell -ExecutionPolicy Bypass -File SourceCodeNew\Send-HL7.ps1 -Server 192.168.1.50 -File .\message.hl7
```

## What it does not do

- **HL7 v2 only.** Not HL7 v3 or FHIR (FHIR is REST/JSON; Peakboard's JSON data
  source already covers it).
- **No TLS.** MLLP is plain TCP. Keep it on the hospital network or put a VPN or
  TLS-terminating interface engine in front.
- **No persistence and no forwarding.** It is a display endpoint, not an interface
  engine. It must not be the only receiver of messages that have to be kept.
- **No schema validation.** Messages are parsed structurally; missing or unusual
  fields read as empty rather than being rejected.
- Batch files (`FHS`/`BHS`) are not split into their messages.

Patient data is personal health data. The rows are held in memory only, but they are
on screen: place the board and choose the columns accordingly.

## Building

```powershell
powershell -ExecutionPolicy Bypass -File SourceCodeNew\Build.ps1
```

Builds `SourceCodeNew\HL7\PeakboardExtensionHL7.csproj` and writes
`Binary\HL7.zip`. `Peakboard.ExtensionKit` is referenced but not redistributed.
