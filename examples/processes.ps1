#------------------------------------------------------------
# Shows a list of running process in minitor, with status
# based on process' handles count:
#
# - error (red)      if more than 1000
# - warning (yellow) if more than 300
# - normal (white)   otherwise
#
#---------------------------------------------------

# Set host and port, Debug builds run on port 12345
# while Release builds run on default port 80
$base = "http://localhost:12345"
#$base = "http://localhost"

# Set tree location to 'Processes'
$base = $base + "/set/Processes?"

# Dead processes will turn to unknown status (blue)
# within 30 seconds, then will disappear after 2 minutes
$base = $base + "val=30s&exp=2m"

# Loop until interrupted
"Sending processes, press Ctrl+C to stop."
while($true)
{
    foreach($proc in Get-Process)
    {
        # Build URL with monitor name as process name and id
        $url = $base + "&mon=$($proc.Name):$($proc.Id)&status="

        # Add status based on handles count
        if ($proc.Handles -gt 1000)
            { $url = $url + "error&text=$($proc.Handles)%20handles%20is%20too%20much" }

        elseif ($proc.Handles -gt 300)
            { $url = $url + "warning" }

        else
            { $url = $url + "normal" }

        # Do a simple GET on the built URL, ignore result
        $null = invoke-webrequest $url
    }

    # Rinse and repeat
    start-sleep -seconds 10
    "Refreshing..."
}
