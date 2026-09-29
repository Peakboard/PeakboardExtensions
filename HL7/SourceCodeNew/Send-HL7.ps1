# Send an HL7 v2 message to the extension over MLLP and print the ACK.
# For commissioning a board without a real sending system.
#
#   powershell -ExecutionPolicy Bypass -File SourceCodeNew\Send-HL7.ps1
#   powershell -ExecutionPolicy Bypass -File SourceCodeNew\Send-HL7.ps1 -Server 192.168.1.50 -Sample ORU -PatientId 4711
#   powershell -ExecutionPolicy Bypass -File SourceCodeNew\Send-HL7.ps1 -File .\message.hl7

param(
    [string]$Server = "127.0.0.1",
    [int]$Port = 2575,
    [ValidateSet("ADT", "ORU")] [string]$Sample = "ADT",
    [string]$PatientId = "4711",
    [string]$File
)

$ErrorActionPreference = "Stop"
$now = Get-Date -Format "yyyyMMddHHmmss"
$id = "TEST" + (Get-Date -Format "yyyyMMddHHmmssfff")

if ($File) {
    $message = (Get-Content -LiteralPath $File -Raw) -replace "`r?`n", "`r"
}
elseif ($Sample -eq "ADT") {
    $message = @(
        "MSH|^~\&|HIS|WARD3|PEAKBOARD|PB|$now||ADT^A01^ADT_A01|$id|P|2.5"
        "EVN|A01|$now"
        "PID|1||$PatientId^^^HOSP^MR||Mustermann^Max||19800101|M"
        "PV1|1|I|W3^301^B"
    ) -join "`r"
}
else {
    $message = @(
        "MSH|^~\&|LAB|LABFAC|PEAKBOARD|PB|$now||ORU^R01|$id|P|2.5"
        "PID|1||$PatientId^^^HOSP^MR||Mustermann^Max||19800101|M"
        "OBR|1||ORD$now|CBC^Blood count"
        "OBX|1|NM|HGB^Haemoglobin||13.5|g/dL|12-16|N|||F|||$now"
        "OBX|2|NM|WBC^Leukocytes||7.2|10*9/L|4-10|N|||F|||$now"
    ) -join "`r"
}

$client = New-Object System.Net.Sockets.TcpClient($Server, $Port)
try {
    $stream = $client.GetStream()
    $stream.ReadTimeout = 10000
    $body = [System.Text.Encoding]::UTF8.GetBytes($message)
    $frame = [byte[]](@(0x0B) + $body + @(0x1C, 0x0D))
    $stream.Write($frame, 0, $frame.Length)

    $buffer = New-Object byte[] 4096
    $ack = ""
    while ($ack.IndexOf([char]0x1C) -lt 0) {
        $n = $stream.Read($buffer, 0, $buffer.Length)
        if ($n -eq 0) { break }
        $ack += [System.Text.Encoding]::UTF8.GetString($buffer, 0, $n)
    }
    Write-Host "Sent $id to ${Server}:$Port" -ForegroundColor Cyan
    Write-Host ($ack.Trim([char]0x0B, [char]0x1C, "`r") -replace "`r", "`n")
}
finally {
    $client.Close()
}
