using WorkflowEngine.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace WorkflowEngine.Services;

public interface IWorkflowService
{
    Task<WorkflowDefinition> CreateWorkflowDefinitionAsync(WorkflowDefinitionRequest request);
    Task<WorkflowDefinition?> GetWorkflowDefinitionAsync(string id);
    Task<List<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync();
    Task<WorkflowInstance> StartWorkflowInstanceAsync(string definitionId);
    Task<WorkflowInstance?> GetWorkflowInstanceAsync(string id);
    Task<List<WorkflowInstance>> GetAllWorkflowInstancesAsync();
    Task<WorkflowInstance> ExecuteActionAsync(string instanceId, string actionId);
}

public class WorkflowService : IWorkflowService
{
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _definitions = new();
    private readonly ConcurrentDictionary<string, WorkflowInstance> _instances = new();
    private readonly string _dataFilePath = "workflow_data.json";

    public WorkflowService()
    {
        LoadDataFromFile();
    }

    public async Task<WorkflowDefinition> CreateWorkflowDefinitionAsync(WorkflowDefinitionRequest request)
    {
        ValidateWorkflowDefinition(request);

        var definition = new WorkflowDefinition
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            States = request.States.Select(s => new State
            {
                Id = s.Id,
                Name = s.Name,
                IsInitial = s.IsInitial,
                IsFinal = s.IsFinal,
                Enabled = s.Enabled,
                Description = s.Description
            }).ToList(),
            Actions = request.Actions.Select(a => new WorkflowEngine.Models.Action
            {
                Id = a.Id,
                Name = a.Name,
                FromStates = a.FromStates,
                ToState = a.ToState,
                Enabled = a.Enabled
            }).ToList()
        };

        _definitions[definition.Id] = definition;
        await SaveDataToFile();
        
        return definition;
    }

    public async Task<WorkflowDefinition?> GetWorkflowDefinitionAsync(string id)
    {
        await Task.CompletedTask;
        return _definitions.TryGetValue(id, out var definition) ? definition : null;
    }

    public async Task<List<WorkflowDefinition>> GetAllWorkflowDefinitionsAsync()
    {
        await Task.CompletedTask;
        return _definitions.Values.ToList();
    }

    public async Task<WorkflowInstance> StartWorkflowInstanceAsync(string definitionId)
    {
        var definition = await GetWorkflowDefinitionAsync(definitionId);
        if (definition == null)
        {
            throw new ArgumentException($"Workflow definition with id '{definitionId}' not found");
        }

        var initialState = definition.GetInitialState();
        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid().ToString(),
            DefinitionId = definitionId,
            CurrentStateId = initialState.Id,
            History = new List<HistoryEntry>
            {
                new HistoryEntry
                {
                    ActionId = "START",
                    ActionName = "Start Workflow",
                    FromStateId = "",
                    ToStateId = initialState.Id,
                    Timestamp = DateTime.UtcNow
                }
            }
        };

        _instances[instance.Id] = instance;
        await SaveDataToFile();
        
        return instance;
    }

    public async Task<WorkflowInstance?> GetWorkflowInstanceAsync(string id)
    {
        await Task.CompletedTask;
        return _instances.TryGetValue(id, out var instance) ? instance : null;
    }

    public async Task<List<WorkflowInstance>> GetAllWorkflowInstancesAsync()
    {
        await Task.CompletedTask;
        return _instances.Values.ToList();
    }

    public async Task<WorkflowInstance> ExecuteActionAsync(string instanceId, string actionId)
    {
        var instance = await GetWorkflowInstanceAsync(instanceId);
        if (instance == null)
        {
            throw new ArgumentException($"Workflow instance with id '{instanceId}' not found");
        }

        var definition = await GetWorkflowDefinitionAsync(instance.DefinitionId);
        if (definition == null)
        {
            throw new ArgumentException($"Workflow definition not found for instance");
        }

        var action = definition.Actions.FirstOrDefault(a => a.Id == actionId);
        if (action == null)
        {
            throw new ArgumentException($"Action with id '{actionId}' not found in workflow definition");
        }

        if (!action.Enabled)
        {
            throw new ArgumentException($"Action '{actionId}' is disabled");
        }

        var currentState = definition.States.FirstOrDefault(s => s.Id == instance.CurrentStateId);
        if (currentState == null)
        {
            throw new ArgumentException($"Current state '{instance.CurrentStateId}' not found in definition");
        }

        if (currentState.IsFinal)
        {
            throw new ArgumentException($"Cannot execute actions on final state '{currentState.Id}'");
        }

        if (!action.FromStates.Contains(instance.CurrentStateId))
        {
            throw new ArgumentException($"Action '{actionId}' cannot be executed from current state '{instance.CurrentStateId}'");
        }

        var targetState = definition.States.FirstOrDefault(s => s.Id == action.ToState);
        if (targetState == null)
        {
            throw new ArgumentException($"Target state '{action.ToState}' not found in definition");
        }

        // Execute the action
        var historyEntry = new HistoryEntry
        {
            ActionId = action.Id,
            ActionName = action.Name,
            FromStateId = instance.CurrentStateId,
            ToStateId = action.ToState,
            Timestamp = DateTime.UtcNow
        };

        instance.CurrentStateId = action.ToState;
        instance.History.Add(historyEntry);
        instance.LastModified = DateTime.UtcNow;

        await SaveDataToFile();
        
        return instance;
    }

    private void ValidateWorkflowDefinition(WorkflowDefinitionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Workflow name cannot be empty");
        }

        if (request.States == null || request.States.Count == 0)
        {
            throw new ArgumentException("Workflow must have at least one state");
        }

        if (request.Actions == null)
        {
            throw new ArgumentException("Workflow must have actions defined");
        }

        // Check for duplicate state IDs
        var stateIds = request.States.Select(s => s.Id).ToList();
        if (stateIds.Count != stateIds.Distinct().Count())
        {
            throw new ArgumentException("Duplicate state IDs found");
        }

        // Check for duplicate action IDs
        var actionIds = request.Actions.Select(a => a.Id).ToList();
        if (actionIds.Count != actionIds.Distinct().Count())
        {
            throw new ArgumentException("Duplicate action IDs found");
        }

        // Check for exactly one initial state
        var initialStates = request.States.Where(s => s.IsInitial).ToList();
        if (initialStates.Count != 1)
        {
            throw new ArgumentException("Workflow must have exactly one initial state");
        }

        // Validate action references
        var stateIdSet = new HashSet<string>(stateIds);
        foreach (var action in request.Actions)
        {
            if (action.FromStates.Any(fs => !stateIdSet.Contains(fs)))
            {
                throw new ArgumentException($"Action '{action.Id}' references unknown from-state(s)");
            }

            if (!stateIdSet.Contains(action.ToState))
            {
                throw new ArgumentException($"Action '{action.Id}' references unknown to-state '{action.ToState}'");
            }
        }
    }

    private void LoadDataFromFile()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                var json = File.ReadAllText(_dataFilePath);
                var data = JsonSerializer.Deserialize<WorkflowData>(json);
                
                if (data != null)
                {
                    foreach (var definition in data.Definitions)
                    {
                        _definitions[definition.Id] = definition;
                    }
                    
                    foreach (var instance in data.Instances)
                    {
                        _instances[instance.Id] = instance;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log error in production - for now just continue with empty data
            Console.WriteLine($"Error loading data: {ex.Message}");
        }
    }

    private async Task SaveDataToFile()
    {
        try
        {
            var data = new WorkflowData
            {
                Definitions = _definitions.Values.ToList(),
                Instances = _instances.Values.ToList()
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_dataFilePath, json);
        }
        catch (Exception ex)
        {
            // Log error in production
            Console.WriteLine($"Error saving data: {ex.Message}");
        }
    }

    private class WorkflowData
    {
        public List<WorkflowDefinition> Definitions { get; set; } = new();
        public List<WorkflowInstance> Instances { get; set; } = new();
    }
}