# Configurable Workflow Engine

A minimal backend service that provides a configurable state machine API for workflow management.

## Prerequisites

- .NET 8.0 SDK
- Docker Desktop (for containerized deployment)
- PowerShell (for testing scripts)
- Git

## Getting Started

### Step 0: Clone the Repository
```powershell
# Clone the repository
git clone https://github.com/SRINJOY59/Infonetica.git

# Navigate to the project directory
cd Infonetica
```

### Option 1: Run Locally with .NET

#### Step 1: Clean and Build
```powershell
# Clean previous builds (if any)
dotnet clean
Remove-Item -Recurse -Force obj -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force bin -ErrorAction SilentlyContinue

# Restore dependencies and build
dotnet restore
dotnet build
```

#### Step 2: Run the Application
```powershell
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

### Option 2: Run with Docker

#### Step 1: Build Docker Image
```powershell
# Clean everything first
dotnet clean
Remove-Item -Recurse -Force obj -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force bin -ErrorAction SilentlyContinue

# Build Docker image
docker build -t workflow-engine .
```

#### Step 2: Run with Docker Compose (Recommended)
```powershell
# Start the application
docker-compose up -d

# View logs
docker-compose logs -f

# Stop the application
docker-compose down
```

The API will be available at: `http://localhost:8080`

#### Alternative: Run Docker Container Directly
```powershell
# Run container
docker run -d -p 8080:8080 -v ${PWD}/data:/app/data --name workflow-engine workflow-engine

# View logs
docker logs workflow-engine

# Stop container
docker stop workflow-engine
docker rm workflow-engine
```

## Quick Start Guide

### 1. Clone and Run
```powershell
# Clone the repo
git clone https://github.com/SRINJOY59/Infonetica.git
cd Infonetica

# Option A: Run locally
dotnet run

# Option B: Run with Docker
docker-compose up -d
```

### 2. Test the API
```powershell
# Test if API is running (use port 5000 for local, 8080 for Docker)
$baseUrl = "http://localhost:5000"  # or "http://localhost:8080" for Docker

# Create a simple workflow
$workflow = @{
    name = "Document Approval"
    states = @(
        @{id="draft"; name="Draft"; isInitial=$true; isFinal=$false; enabled=$true}
        @{id="approved"; name="Approved"; isInitial=$false; isFinal=$true; enabled=$true}
    )
    actions = @(
        @{id="approve"; name="Approve"; fromStates=@("draft"); toState="approved"; enabled=$true}
    )
} | ConvertTo-Json -Depth 3

$createdWorkflow = Invoke-RestMethod -Uri "$baseUrl/api/workflows" -Method POST -Body $workflow -ContentType "application/json"
Write-Host "Created workflow: $($createdWorkflow.name) with ID: $($createdWorkflow.id)"

# Start an instance
$instance = Invoke-RestMethod -Uri "$baseUrl/api/workflows/$($createdWorkflow.id)/instances" -Method POST
Write-Host "Started instance: $($instance.id) in state: $($instance.currentStateId)"

# Execute action
$action = @{actionId="approve"} | ConvertTo-Json
$updatedInstance = Invoke-RestMethod -Uri "$baseUrl/api/instances/$($instance.id)/execute" -Method POST -Body $action -ContentType "application/json"
Write-Host "After approval: $($updatedInstance.currentStateId)"
```

### Run Automated Tests
```powershell
# Run tests
dotnet test Tests\WorkflowEngine.Tests.csproj --verbosity normal
```

## API Endpoints

### Workflow Definitions
- `POST /api/workflows` - Create workflow definition
- `GET /api/workflows` - List all workflows
- `GET /api/workflows/{id}` - Get specific workflow

### Workflow Instances  
- `POST /api/workflows/{definitionId}/instances` - Start new instance
- `GET /api/instances` - List all instances
- `GET /api/instances/{id}` - Get specific instance
- `POST /api/instances/{id}/execute` - Execute action

## Complete Example Workflow

### 1. Create Complex Workflow
```powershell
$complexWorkflow = @{
    name = "Purchase Order Process"
    states = @(
        @{id="created"; name="Created"; isInitial=$true; isFinal=$false; enabled=$true}
        @{id="pending"; name="Pending Approval"; isInitial=$false; isFinal=$false; enabled=$true}
        @{id="approved"; name="Approved"; isInitial=$false; isFinal=$false; enabled=$true}
        @{id="rejected"; name="Rejected"; isInitial=$false; isFinal=$true; enabled=$true}
        @{id="completed"; name="Completed"; isInitial=$false; isFinal=$true; enabled=$true}
    )
    actions = @(
        @{id="submit"; name="Submit"; fromStates=@("created"); toState="pending"; enabled=$true}
        @{id="approve"; name="Approve"; fromStates=@("pending"); toState="approved"; enabled=$true}
        @{id="reject"; name="Reject"; fromStates=@("pending"); toState="rejected"; enabled=$true}
        @{id="complete"; name="Complete"; fromStates=@("approved"); toState="completed"; enabled=$true}
    )
} | ConvertTo-Json -Depth 3

$baseUrl = "http://localhost:5000"  # Change to 8080 for Docker
$workflow = Invoke-RestMethod -Uri "$baseUrl/api/workflows" -Method POST -Body $complexWorkflow -ContentType "application/json"
```

### 2. Execute Full Workflow
```powershell
# Start instance
$instance = Invoke-RestMethod -Uri "$baseUrl/api/workflows/$($workflow.id)/instances" -Method POST

# Submit for approval
$submit = @{actionId="submit"} | ConvertTo-Json
$instance = Invoke-RestMethod -Uri "$baseUrl/api/instances/$($instance.id)/execute" -Method POST -Body $submit -ContentType "application/json"

# Approve
$approve = @{actionId="approve"} | ConvertTo-Json  
$instance = Invoke-RestMethod -Uri "$baseUrl/api/instances/$($instance.id)/execute" -Method POST -Body $approve -ContentType "application/json"

# Complete
$complete = @{actionId="complete"} | ConvertTo-Json
$instance = Invoke-RestMethod -Uri "$baseUrl/api/instances/$($instance.id)/execute" -Method POST -Body $complete -ContentType "application/json"

# View final state and history
Write-Host "Final state: $($instance.currentStateId)"
$instance.history | ForEach-Object { Write-Host "$($_.actionName): $($_.fromStateId) -> $($_.toStateId)" }
```

## Troubleshooting

### Common Issues

**Build Errors:**
```powershell
# Clean everything and rebuild
dotnet clean
Remove-Item -Recurse -Force obj, bin -ErrorAction SilentlyContinue
dotnet restore
dotnet build
```

**Docker Issues:**
```powershell
# Rebuild Docker image
docker-compose down
docker rmi workflow-engine
docker-compose up --build -d
```

**Port Conflicts:**
- Local .NET app uses ports 5000/5001
- Docker uses port 8080
- Change ports in `docker-compose.yml` if needed

**Test Issues:**
- Make sure `Tests\WorkflowEngine.Tests.csproj` exists
- Run tests with: `dotnet test Tests\WorkflowEngine.Tests.csproj`