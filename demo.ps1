$url="http://localhost:12345/set"
#$url="http://localhost/set"
#$url="http://sdcamsxwitnessb.euc.ppg.com/set"

# Invoke-RestMethod "$url/Exchange/Databases" -Body @{ name = "Replication"; extra = "Some longer text ?"; status = "Error"; validity="2h"; expiration="2d"}

Invoke-RestMethod $url/Exchange/Database -Body @{monitor="DB0001"; text="Some longer text ?"; status="Normal"; }
#
Invoke-RestMethod $url/Replication -Body @{monitor="DB0001"; text="Some longer text ?"; status="Warning"; }

Invoke-RestMethod $url -Body @{monitor="SRV1"; text="Ok"; status="Normal"; }
Invoke-RestMethod $url -Body @{monitor="SRV2"; text="Some information"; status="Warning"; }
Invoke-RestMethod $url -Body @{monitor="SRV3"; text="Normal"; }
Invoke-RestMethod $url -Body @{monitor="SRV4"; text="Normal"; }
Invoke-RestMethod $url -Body @{monitor="SRV5"; text="Unknown"; status="Unknown"; }
Invoke-RestMethod $url -Body @{monitor="SRV6"; text="Warning"; status ="Warning"; }
Invoke-RestMethod $url -Body @{monitor="SRV7"; text="Error"; status="Error"; }
Invoke-RestMethod $url -Body @{monitor="SRV8"; text="Dead"; status="Dead"; validity="10s"; }
Invoke-RestMethod $url -Body @{monitor="SRV9"; status="Normal"; }

Invoke-WebRequest "$url/Exchange?monitor=SRV3&status=normal&v=100s"
#>
#Invoke-WebRequest "$($url)?monitor=SRV3&status=warning&validity=5s&expiration=120s"
Invoke-WebRequest "$($url)?monitor=SRV1&validity=5s&expiration=120s"

Invoke-WebRequest "$($url)?m=SRV1&s=dead&v=120s&e=120s"