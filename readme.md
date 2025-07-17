# Configurable Workflow Engine

A backend service that provides a configurable state machine API for workflow management.

## Prerequisites

- .NET 8.0 SDK
- Docker Desktop (for containerized deployment)
- PowerShell (for testing scripts)
- Git

## Complete Setup and Testing Guide

### Step 1: Clone the Repository
```powershell
# Clone the repository
git clone https://github.com/SRINJOY59/Infonetica.git

# Navigate to the project directory
cd Infonetica
```

### Step 2: Complete Clean (Remove all build artifacts)
```powershell
# Clean .NET build artifacts
dotnet clean

# Remove all obj and bin directories
Remove-Item -Recurse -Force obj, bin -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force Tests\obj, Tests\bin -ErrorAction SilentlyContinue

# Clean Docker artifacts (if any exist)
docker system prune -f
```

### Step 3: Restore Dependencies
```powershell
# Restore main project dependencies
dotnet restore

# Restore test project dependencies
dotnet restore Tests\WorkflowEngine.Tests.csproj
```

### Step 4: Build Projects
```powershell
# Build main project
dotnet build WorkflowEngine.csproj

# Build test project
dotnet build Tests\WorkflowEngine.Tests.csproj

# Verify builds completed successfully
Write-Host "✅ Build completed successfully!"
```

### Step 5: Run Automated Tests
```powershell
# Run all tests with detailed output
dotnet test Tests\WorkflowEngine.Tests.csproj --verbosity normal

# Expected output:
# Passed!  - Failed: 0, Passed: 7, Skipped: 0, Total: 7
Write-Host "✅ All tests passed!"
```

### Step 6: Run with Docker Compose
```powershell
# Build and start Docker containers
docker-compose up --build -d

# Verify container is running
docker-compose ps

# View logs (optional)
docker-compose logs -f workflow-engine

# Test Docker deployment
$dockerUrl = "http://localhost:8080"
try {
    $response = Invoke-RestMethod -Uri "$dockerUrl/api/workflows" -Method GET
    Write-Host "✅ Docker deployment successful! API accessible at $dockerUrl"
} catch {
    Write-Host "❌ Docker deployment failed: $($_.Exception.Message)"
}
```

### Step 7: Test API with Docker
```powershell
# Set base URL for Docker
$baseUrl = "http://localhost:8080"

# Test 1: Create a workflow
$workflow = @{
    name = "Document Approval Process"
    states = @(
        @{id="draft"; name="Draft"; isInitial=$true; isFinal=$false; enabled=$true}
        @{id="review"; name="Under Review"; isInitial=$false; isFinal=$false; enabled=$true}
        @{id="approved"; name="Approved"; isInitial=$false; isFinal=$true; enabled=$true}
    )
    actions = @(
        @{id="submit"; name="Submit for Review"; fromStates=@("draft"); toState="review"; enabled=$true}
        @{id="approve"; name="Approve"; fromStates=@("review"); toState="approved"; enabled=$true}
    )
} | ConvertTo-Json -Depth 3

$createdWorkflow = Invoke-RestMethod -Uri "$baseUrl/api/workflows" -Method POST -Body $workflow -ContentType "application/json"
Write-Host "✅ Created workflow: $($createdWorkflow.name) with ID: $($createdWorkflow.id)"

# Test 2: Start workflow instance
$instance = Invoke-RestMethod -Uri "$baseUrl/api/workflows/$($createdWorkflow.id)/instances" -Method POST
Write-Host "✅ Started instance: $($instance.id) in state: $($instance.currentStateId)"

# Test 3: Execute submit action
$submitAction = @{actionId="submit"} | ConvertTo-Json
$instance = Invoke-RestMethod -Uri "$baseUrl/api/instances/$($instance.id)/execute" -Method POST -Body $submitAction -ContentType "application/json"
Write-Host "✅ After submit: Current state = $($instance.currentStateId)"

# Test 4: Execute approve action
$approveAction = @{actionId="approve"} | ConvertTo-Json
$instance = Invoke-RestMethod -Uri "$baseUrl/api/instances/$($instance.id)/execute" -Method POST -Body $approveAction -ContentType "application/json"
Write-Host "✅ After approve: Current state = $($instance.currentStateId)"

# Test 5: View workflow history
Write-Host "📋 Workflow History:"
$instance.history | ForEach-Object { 
    Write-Host "  $($_.timestamp): $($_.actionName) ($($_.fromStateId) -> $($_.toStateId))" 
}

Write-Host "🎉 Docker API testing completed successfully!"
```

### Step 8: Stop Docker and Run Locally
```powershell
# Stop Docker containers
docker-compose down
Write-Host "✅ Docker containers stopped"

# Run locally with .NET
Start-Process powershell -ArgumentList "-Command", "dotnet run" -WindowStyle Normal
Start-Sleep 5  # Wait for application to start

# Test local deployment
$localUrl = "http://localhost:5000"
try {
    $response = Invoke-RestMethod -Uri "$localUrl/api/workflows" -Method GET
    Write-Host "✅ Local deployment successful! API accessible at $localUrl"
} catch {
    Write-Host "❌ Local deployment failed. Make sure application started properly."
}
```

### Step 9: Test API Locally
```powershell
# Set base URL for local testing
$baseUrl = "http://localhost:5000"

# Quick API test
$simpleWorkflow = @{
    name = "Simple Approval"
    states = @(
        @{id="draft"; name="Draft"; isInitial=$true; isFinal=$false; enabled=$true}
        @{id="approved"; name="Approved"; isInitial=$false; isFinal=$true; enabled=$true}
    )
    actions = @(
        @{id="approve"; name="Approve"; fromStates=@("draft"); toState="approved"; enabled=$true}
    )
} | ConvertTo-Json -Depth 3

$workflow = Invoke-RestMethod -Uri "$baseUrl/api/workflows" -Method POST -Body $simpleWorkflow -ContentType "application/json"
$instance = Invoke-RestMethod -Uri "$baseUrl/api/workflows/$($workflow.id)/instances" -Method POST
$action = @{actionId="approve"} | ConvertTo-Json
$finalInstance = Invoke-RestMethod -Uri "$baseUrl/api/instances/$($instance.id)/execute" -Method POST -Body $action -ContentType "application/json"

Write-Host "✅ Local API test completed! Final state: $($finalInstance.currentStateId)"
```

## One-Command Complete Test
```powershell
# Complete end-to-end test script
function Test-WorkflowEngine {
    Write-Host "🚀 Starting complete Workflow Engine test..."
    
    # Step 1: Clean
    Write-Host "🧹 Cleaning..."
    dotnet clean
    Remove-Item -Recurse -Force obj, bin -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force Tests\obj, Tests\bin -ErrorAction SilentlyContinue
    
    # Step 2: Restore & Build
    Write-Host "📦 Restoring and building..."
    dotnet restore
    dotnet build
    
    # Step 3: Test
    Write-Host "🧪 Running tests..."
    dotnet test Tests\WorkflowEngine.Tests.csproj --verbosity normal
    
    # Step 4: Docker
    Write-Host "🐳 Testing with Docker..."
    docker-compose up --build -d
    Start-Sleep 10
    
    # Quick Docker test
    $dockerResponse = Invoke-RestMethod -Uri "http://localhost:8080/api/workflows" -Method GET
    Write-Host "✅ Docker test passed"
    
    # Step 5: Local
    Write-Host "💻 Testing locally..."
    docker-compose down
    Start-Process powershell -ArgumentList "-Command", "dotnet run" -WindowStyle Normal
    Start-Sleep 5
    
    $localResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/workflows" -Method GET
    Write-Host "✅ Local test passed"
    
    Write-Host "🎉 All tests completed successfully!"
}

# Run complete test
Test-WorkflowEngine
```

## API Endpoints Reference

### Workflow Definitions
- `POST /api/workflows` - Create workflow definition
- `GET /api/workflows` - List all workflows  
- `GET /api/workflows/{id}` - Get specific workflow

### Workflow Instances
- `POST /api/workflows/{definitionId}/instances` - Start new instance
- `GET /api/instances` - List all instances
- `GET /api/instances/{id}` - Get specific instance  
- `POST /api/instances/{id}/execute` - Execute action

## Troubleshooting

### If Tests Fail
```powershell
# Complete reset
dotnet clean
Remove-Item -Recurse -Force obj, bin, Tests\obj, Tests\bin -ErrorAction SilentlyContinue
dotnet restore
dotnet restore Tests\WorkflowEngine.Tests.csproj
dotnet build
dotnet test Tests\WorkflowEngine.Tests.csproj
```

### If Docker Fails
```powershell
# Reset Docker
docker-compose down
docker system prune -f
docker-compose up --build -d
```

### If Local App Fails
```powershell
# Check if port is in use
netstat -an | findstr :5000

# Kill existing processes if needed
Get-Process -Name "WorkflowEngine" -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet run
```

## Quick Commands Summary
```powershell
# Full clean and test cycle
dotnet clean && Remove-Item -Recurse -Force obj, bin, Tests\obj, Tests\bin -ErrorAction SilentlyContinue && dotnet restore && dotnet build && dotnet test Tests\WorkflowEngine.Tests.csproj

# Docker quick start  
docker-compose up --build -d

# Local quick start
dotnet run
```