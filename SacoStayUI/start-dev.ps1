# PowerShell script để start development server
Write-Host "Starting SacoStay UI Development Server..." -ForegroundColor Green
Write-Host "Make sure you have run: npm install" -ForegroundColor Yellow
Write-Host ""

# Try to start Angular dev server
try {
    ng serve --open
} catch {
    Write-Host "Error starting server. Make sure Angular CLI is installed." -ForegroundColor Red
    Write-Host "Run: npm install -g @angular/cli" -ForegroundColor Yellow
}

Read-Host "Press Enter to exit"
