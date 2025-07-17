using WorkflowEngine.Models;
using WorkflowEngine.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WorkflowEngine.Tests;

[TestClass]
public class WorkflowServiceTests
{
    private WorkflowService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new WorkflowService();
    }

    [TestMethod]
    public async Task CreateWorkflowDefinition_ValidDefinition_ReturnsDefinition()
    {
        // Arrange
        var request = new WorkflowDefinitionRequest(
            "Test Workflow",
            new List<StateRequest>
            {
                new("draft", "Draft", true, false, true),
                new("final", "Final", false, true, true)
            },
            new List<ActionRequest>
            {
                new("complete", "Complete", new List<string> { "draft" }, "final", true)
            }
        );

        // Act
        var result = await _service.CreateWorkflowDefinitionAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Workflow", result.Name);
        Assert.AreEqual(2, result.States.Count);
        Assert.AreEqual(1, result.Actions.Count);
    }

    [TestMethod]
    public async Task CreateWorkflowDefinition_NoInitialState_ThrowsException()
    {
        // Arrange
        var request = new WorkflowDefinitionRequest(
            "Invalid Workflow",
            new List<StateRequest>
            {
                new("state1", "State 1", false, false, true),
                new("state2", "State 2", false, true, true)
            },
            new List<ActionRequest>()
        );

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.CreateWorkflowDefinitionAsync(request)
        );
    }

    [TestMethod]
    public async Task CreateWorkflowDefinition_DuplicateStateIds_ThrowsException()
    {
        // Arrange
        var request = new WorkflowDefinitionRequest(
            "Invalid Workflow",
            new List<StateRequest>
            {
                new("duplicate", "State 1", true, false, true),
                new("duplicate", "State 2", false, true, true)
            },
            new List<ActionRequest>()
        );

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.CreateWorkflowDefinitionAsync(request)
        );
    }

    [TestMethod]
    public async Task StartWorkflowInstance_ValidDefinition_ReturnsInstance()
    {
        // Arrange
        var definition = await CreateTestWorkflowDefinition();

        // Act
        var instance = await _service.StartWorkflowInstanceAsync(definition.Id);

        // Assert
        Assert.IsNotNull(instance);
        Assert.AreEqual(definition.Id, instance.DefinitionId);
        Assert.AreEqual("draft", instance.CurrentStateId);
        Assert.AreEqual(1, instance.History.Count);
    }

    [TestMethod]
    public async Task ExecuteAction_ValidAction_UpdatesState()
    {
        // Arrange
        var definition = await CreateTestWorkflowDefinition();
        var instance = await _service.StartWorkflowInstanceAsync(definition.Id);

        // Act
        var updatedInstance = await _service.ExecuteActionAsync(instance.Id, "complete");

        // Assert
        Assert.AreEqual("final", updatedInstance.CurrentStateId);
        Assert.AreEqual(2, updatedInstance.History.Count);
    }

    [TestMethod]
    public async Task ExecuteAction_InvalidAction_ThrowsException()
    {
        // Arrange
        var definition = await CreateTestWorkflowDefinition();
        var instance = await _service.StartWorkflowInstanceAsync(definition.Id);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.ExecuteActionAsync(instance.Id, "nonexistent")
        );
    }

    [TestMethod]
    public async Task ExecuteAction_OnFinalState_ThrowsException()
    {
        // Arrange
        var definition = await CreateTestWorkflowDefinition();
        var instance = await _service.StartWorkflowInstanceAsync(definition.Id);
        await _service.ExecuteActionAsync(instance.Id, "complete"); // Move to final state

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => _service.ExecuteActionAsync(instance.Id, "complete")
        );
    }

    private async Task<WorkflowDefinition> CreateTestWorkflowDefinition()
    {
        var request = new WorkflowDefinitionRequest(
            "Test Workflow",
            new List<StateRequest>
            {
                new("draft", "Draft", true, false, true),
                new("final", "Final", false, true, true)
            },
            new List<ActionRequest>
            {
                new("complete", "Complete", new List<string> { "draft" }, "final", true)
            }
        );

        return await _service.CreateWorkflowDefinitionAsync(request);
    }
}