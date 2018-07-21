#------------------------------------------------------------
# Shows a list of running process in minitor, with status
# based on process' working set memory
#
# - error (red)      if more than 200MB
# - warning (yellow) if more than 100MB
# - normal (white)   otherwise
#
#---------------------------------------------------

# Set URL, Debug builds run on port 12345 while Release builds run on default port 80
$url = "http://localhost:12345/set/Processes"
#url = "http://localhost/set/Processes"

#---------------------------------------------------
# Utility function
function Get-DisplaySize($bytes)
{
    $sizes = "B,KiB,MiB,GiB,TiB,PiB,EiB,ZiB" -split ","

    $i = 0
    while (($bytes -ge 1024.0) -and ($i -lt $sizes.Count)) { $bytes /= 1024.0; $i++; }

    $n = 2
    if ($i -eq 0) { $n = 0 }
    "{0:N$($n)} {1}" -f $bytes, $sizes[$i]
}

#---------------------------------------------------
# Loop until interrupted
"Sending processes, press Ctrl+C to stop."
while($true)
{
    foreach($proc in Get-Process)
    {
        $body = @{}

        # Monitor name based on process name and id
        $body.monitor = "$($proc.Name):$($proc.Id)";

        # Dead processes will turn to unknown status (blue)
        # within 30 seconds, then will disappear after 2 minutes
        $body.validity = "30s";
        $body.expiration = "2m";

        # Add status based on working set memory size
        if ($proc.WorkingSet -ge 200MB)
            { $body.status = "error"; $body.text = "$(Get-DisplaySize $proc.WorkingSet) is too much" }

        elseif ($proc.WorkingSet -ge 100MB)
            { $body.status = "warning"; $body.text = "$(Get-DisplaySize $proc.WorkingSet) is a lot" }

        else
            { $body.status = "normal"; $body.text = Get-DisplaySize $proc.WorkingSet }

        # Do a simple GET on the built URL, ignore answer
        $null = invoke-restmethod $url -body $body
    }

    # Rinse and repeat
    start-sleep -seconds 10
    "Refreshing..."
}
