using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WorkflowEngine.Models;
using WorkflowEngine.Services;

var builder = WebApplication.CreateBuilder(args);

// services
builder.Services.AddSingleton<IWorkflowService, WorkflowService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

// HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Workflow Definition endpoints
app.MapPost("/api/workflows", async (WorkflowDefinitionRequest request, IWorkflowService service) =>
{
    try
    {
        var definition = await service.CreateWorkflowDefinitionAsync(request);
        return Results.Created($"/api/workflows/{definition.Id}", definition);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/workflows/{id}", async (string id, IWorkflowService service) =>
{
    var definition = await service.GetWorkflowDefinitionAsync(id);
    return definition != null ? Results.Ok(definition) : Results.NotFound();
});

app.MapGet("/api/workflows", async (IWorkflowService service) =>
{
    var definitions = await service.GetAllWorkflowDefinitionsAsync();
    return Results.Ok(definitions);
});

// Workflow Instance endpoints
app.MapPost("/api/workflows/{definitionId}/instances", async (string definitionId, IWorkflowService service) =>
{
    try
    {
        var instance = await service.StartWorkflowInstanceAsync(definitionId);
        return Results.Created($"/api/instances/{instance.Id}", instance);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/api/instances/{id}", async (string id, IWorkflowService service) =>
{
    var instance = await service.GetWorkflowInstanceAsync(id);
    return instance != null ? Results.Ok(instance) : Results.NotFound();
});

app.MapGet("/api/instances", async (IWorkflowService service) =>
{
    var instances = await service.GetAllWorkflowInstancesAsync();
    return Results.Ok(instances);
});

app.MapPost("/api/instances/{id}/execute", async (string id, ExecuteActionRequest request, IWorkflowService service) =>
{
    try
    {
        var instance = await service.ExecuteActionAsync(id, request.ActionId);
        return Results.Ok(instance);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();

// Request/Response models
public record WorkflowDefinitionRequest(string Name, List<StateRequest> States, List<ActionRequest> Actions);
public record StateRequest(string Id, string Name, bool IsInitial, bool IsFinal, bool Enabled = true, string? Description = null);
public record ActionRequest(string Id, string Name, List<string> FromStates, string ToState, bool Enabled = true);
public record ExecuteActionRequest(string ActionId);
