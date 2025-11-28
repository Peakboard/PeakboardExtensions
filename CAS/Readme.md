# Peakboard Extension for CAS Scales

This extension connects CAS scales to a Peakboard application. It supports the **PDN series** (POS scales) via the ECR protocol and the **PB2 series** (portable scales) via Serial or Bluetooth Low Energy (BLE).

The extension provides four custom lists to support different protocols and connection types, ensuring seamless integration of weight data into your Peakboard logic.

## Supported Devices

* **CAS PDN Series:** Supports ECR Type 12 and ECR Type 14 protocols via Serial Port (RS232/USB).
* **CAS PB2 Series:** Supports connection via Serial Port (RS232/USB) or Bluetooth Low Energy (BLE).

---

## 1. CAS PDN Series (POS Scales)

There are two data sources available for the PDN series, depending on the ECR protocol configured on your scale: **PDN ECR Typ 12** and **PDN ECR Typ 14**.

### Configuration

The following parameters can be set in the extension's settings panel. Note that PDN scales often default to 7 DataBits and Odd Parity.

| Parameter | Description | Default Value |
| :--- | :--- | :--- |
| `SerialPort` | The COM port the scale is connected to. | `COM3` |
| `Baudrate` | The baud rate required by your scale. | `9600` |
| `Parity` | The parity scheme. Options: `None`, `Odd`, `Even`, `Mark`, `Space`. | `Odd` |
| `StopBits` | The stop bits. Options: `None`, `One`, `Two`, `OnePointFive`. | `One` |
| `DataBits` | The data bits. Options: `8`, `7`, `6`, `5`. | `7` |

### Data Fields (Columns)

The available columns depend strictly on the selected ECR protocol type.

#### **A. PDN ECR Typ 12**
This data source provides detailed transaction data.

| Column | Type | Description |
| :--- | :--- | :--- |
| `Weight` | Number | The measured weight (automatically converted, e.g., to kg). |
| `Unit` | String | The weight unit (kg, g, lb, oz). |
| `UnitPrice` | Number | The unit price set on the scale. |
| `TotalPrice` | Number | The calculated total price. |
| `Timestamp` | Number | Unix timestamp of the last measurement. |

#### **B. PDN ECR Typ 14**
This data source is streamlined and provides only the weight.

> **Note:** When using ECR Type 14, the scale automatically sends the data as soon as the weight is stable. No polling command or manual trigger is required.

| Column | Type | Description |
| :--- | :--- | :--- |
| `Weight` | Number | The measured weight (in kg). |

### Lua Functions (Type 12 only)

For the **Type 12** protocol, you can trigger commands directly from Peakboard.

```lua
-- Requests the current weight from the scale
data.PdnEcr12.GetData()

-- Resets the scale to zero
data.PdnEcr12.SetZero()
```

## <br />2. CAS PB2 Series (Portable Scales)

The PB2 series can be connected either via a cable (**PB2-Serial**) or wirelessly (**PB2-BLE**).

### A. PB2-Serial Configuration
Used for RS232 or USB serial connections. Note that PB2 scales typically use **8 DataBits** and **None Parity**.

| Parameter | Description | Default Value |
| :--- | :--- | :--- |
| `SerialPort` | The COM port of the scale. | `COM3` |
| `Baudrate` | Baud rate of the scale. | `9600` |
| `Parity` | Parity setting. Options: `None`, `Odd`, `Even`, `Mark`, `Space`. | `None` |
| `StopBits` | The stop bits. Options: `None`, `One`, `Two`, `OnePointFive`. | `One` |
| `DataBits` | The data bits. Options: `8`, `7`, `6`, `5`. | `8` |

### B. PB2-BLE Configuration
Used for Bluetooth Low Energy connections.

| Parameter | Description | Default Value |
| :--- | :--- | :--- |
| `DeviceIdentifier` | The Bluetooth name or ID of the scale. | `CAS-BLE` |
| `AutoConnect` | If `true`, attempts to connect automatically on startup. | `true` |

### Data Fields (PB2 Columns)

Both PB2 data sources (Serial & BLE) share the same column structure.

| Column | Type | Description |
| :--- | :--- | :--- |
| `Status` | String | Scale status (e.g., "Stable", "Unstable") or connection info. |
| `Weight` | Number | The current weight. |
| `Battery` | String | Battery level info (updated via `GetBattery`). |
| `Timestamp` | Number | Unix timestamp of the last update. |

### Lua Functions (PB2)

You can send commands to the PB2 scale using Lua scripts.

```lua
-- Requests the current weight (Works for Serial & BLE)
data.PB2Source.GetData()

-- Sets the scale to zero (Works for Serial & BLE)
data.PB2Source.SetZero()

-- Tares the scale (subtracts current weight) (Works for Serial & BLE)
data.PB2Source.Tare()

-- Requests the battery status (Works for Serial & BLE)
data.PB2Source.GetBattery()

-- Manually connects to the device (BLE Only - if AutoConnect is false)
data.PB2Source.Connect()
```

## <br />3. Hints

* **Push-Only Data:** All data sources in this extension are configured as **Push-Only**. This means you do not need to define a reload interval. The data is updated automatically in real-time as soon as the scale sends a stable weight or responds to a command.
* **Asynchronous Commands:** Functions like `SetZero`, `Tare`, or `Connect` are executed asynchronously. The Lua script will not wait for the scale to respond immediately. Instead, the success or failure message (e.g., "Zero Set", "Failed (NAK)") will appear in the **Status** column of the data source shortly after execution.
* **Unit Conversion:** For PDN scales (ECR protocols), the extension automatically divides the raw weight value by 1000 to provide the weight in **kilograms (kg)**.
* **Bluetooth Auto-Connect:** If you use the **PB2-BLE** source with `AutoConnect` enabled, the extension attempts to find and connect to the scale immediately when the board starts. You do not need to call the `Connect()` function manually in this case.