try {
  $r = Invoke-WebRequest -Uri 'http://localhost:5173/' -UseBasicParsing -TimeoutSec 10
  Write-Output "HTTP $($r.StatusCode) OK"
} catch {
  Write-Output "FAILED: $($_.Exception.Message)"
}