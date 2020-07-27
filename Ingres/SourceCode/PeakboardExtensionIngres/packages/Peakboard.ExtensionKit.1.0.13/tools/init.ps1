param($installPath, $toolsPath, $package, $project)

$pbek = "Peakboard.ExtensionKit"

"Init.ps1"

foreach ($reference in $project.Object.References)
{
    if($reference.Name -eq $pbek)
    {
        if($reference.CopyLocal -eq $true)
        {
            $reference.CopyLocal = $false;
        }
        else
        {
            $reference.CopyLocal = $true;
        }

		"Updated CopyLocal for $pbek reference"
    }
}
