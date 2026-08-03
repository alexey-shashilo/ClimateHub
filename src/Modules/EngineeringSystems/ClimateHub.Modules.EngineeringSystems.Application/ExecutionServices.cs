using ClimateHub.Modules.Commands.Contracts;
using ClimateHub.Modules.Devices.Contracts;
using ClimateHub.Modules.EngineeringSystems.Domain;
using ClimateHub.Modules.EngineeringSystems.Domain.Repositories;
using ClimateHub.SharedKernel.Primitives;

namespace ClimateHub.Modules.EngineeringSystems.Application;

public class EngineeringDeviceSelector
{
    private readonly IDevicesModule _devicesModule;
    private readonly ICapabilityCatalog _capabilityCatalog;

    public EngineeringDeviceSelector(IDevicesModule devicesModule, ICapabilityCatalog capabilityCatalog)
    {
        _devicesModule = devicesModule; _capabilityCatalog = capabilityCatalog;
    }

    public async Task<DeviceSelectionResult> SelectByBindingAsync(
        EngineeringSystem system, string role, string deviceCapabilityCode,
        CancellationToken ct = default)
    {
        var bindings = system.DeviceBindings
            .Where(b => b.Role == role)
            .OrderBy(b => b.Priority)
            .ToList();

        if (bindings.Count == 0)
            return new DeviceSelectionResult(null, EngineeringErrors.RequiredDeviceNotFound);

        var preferred = bindings.FirstOrDefault(b => b.Enabled);
        if (preferred is null)
            return new DeviceSelectionResult(null, EngineeringErrors.RequiredDeviceNotFound);

        var deviceId = DeviceId.From(preferred.DeviceId);
        var exists = await _devicesModule.DeviceExistsAsync(deviceId, ct);
        if (!exists)
            return new DeviceSelectionResult(null, EngineeringErrors.RequiredDeviceNotFound);

        var hasCap = await _devicesModule.DeviceHasCapabilityAsync(deviceId, deviceCapabilityCode, ct);
        if (!hasCap)
            return new DeviceSelectionResult(null, EngineeringErrors.RequiredDeviceNotFound);

        var def = await _capabilityCatalog.GetDefinitionAsync(deviceCapabilityCode, ct);
        if (def is null || !def.Writable)
            return new DeviceSelectionResult(null, EngineeringErrors.RequiredDeviceNotFound);

        return new DeviceSelectionResult(deviceId, null);
    }

    public async Task<DeviceSelectionResult> SelectByRoleAsync(
        EngineeringSystem system, string role, string deviceCapabilityCode,
        CancellationToken ct = default)
    {
        return await SelectByBindingAsync(system, role, deviceCapabilityCode, ct);
    }
}

public record DeviceSelectionResult(DeviceId? DeviceId, string? FailureCode)
{
    public bool IsSuccess => DeviceId is not null;
}

public class CommandPlanExecutor
{
    private readonly ICommandsModule _commandsModule;
    private readonly ResourceManager _resourceManager;
    private readonly ICommandPlanRepository _planRepo;
    private readonly IEngineeringSystemRepository _systemRepo;
    private readonly EngineeringDeviceSelector _deviceSelector;
    private readonly HvacSafeStopPlanner _safeStopPlanner;

    public CommandPlanExecutor(
        ICommandsModule commandsModule,
        ResourceManager resourceManager,
        ICommandPlanRepository planRepo,
        IEngineeringSystemRepository systemRepo,
        EngineeringDeviceSelector deviceSelector,
        HvacSafeStopPlanner safeStopPlanner)
    {
        _commandsModule = commandsModule; _resourceManager = resourceManager;
        _planRepo = planRepo; _systemRepo = systemRepo;
        _deviceSelector = deviceSelector; _safeStopPlanner = safeStopPlanner;
    }

    public async Task<ExecutionResult> ExecuteAsync(CommandPlan plan, RoomId roomId,
        EngineeringSystem system, CancellationToken ct = default)
    {
        plan.SetPlanning();
        await _planRepo.UpdateAsync(plan, ct);

        var reservation = await _resourceManager.ReserveAsync(plan, ct);
        if (!reservation.Success)
        {
            plan.Fail(reservation.FailureCode ?? EngineeringErrors.InsufficientResource);
            await _planRepo.UpdateAsync(plan, ct);
            return new ExecutionResult(false, reservation.FailureCode);
        }

        plan.Allocate();
        await _planRepo.UpdateAsync(plan, ct);

        var commands = new List<CommandIdDto>();

        foreach (var step in plan.Steps)
        {
            if (!string.IsNullOrEmpty(step.DeviceRole))
            {
                var selection = await _deviceSelector.SelectByRoleAsync(system, step.DeviceRole, step.CapabilityCode, ct);
                if (!selection.IsSuccess)
                {
                    plan.Fail($"{EngineeringErrors.RequiredDeviceNotFound}_{step.DeviceRole}");
                    await _planRepo.UpdateAsync(plan, ct);
                    await _resourceManager.ReleaseAsync(plan, ct);
                    return new ExecutionResult(false, selection.FailureCode);
                }
                step.DeviceId = selection.DeviceId!.Value.Value;
            }

            step.SetReady();
            await _planRepo.UpdateAsync(plan, ct);

            var parJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                speedPct = step.Operation == "control.fan-speed" || step.RequestedValue <= 100 ? step.RequestedValue : Math.Clamp(step.RequestedValue / 300.0 * 100, 0, 100),
                positionPct = step.Operation == "control.damper-position" || step.CapabilityCode.Contains("damper", StringComparison.OrdinalIgnoreCase) ? step.RequestedValue : Math.Clamp(step.RequestedValue / 300.0 * 100, 0, 100),
                levelPct = step.RequestedValue,
                enabled = step.RequestedValue > 0
            });

            var cmdReq = new CreateCommandRequestDto(
                BuildingId: system.BuildingId.ToString(),
                RoomId: roomId.ToString(),
                DeviceId: step.DeviceId?.ToString() ?? string.Empty,
                CapabilityCode: step.CapabilityCode,
                Operation: step.Operation,
                ParametersJson: parJson,
                CreatedBy: "system:engineering-systems",
                CorrelationId: null);

            try
            {
                var cmd = await _commandsModule.CreateAsync(cmdReq, ct);
                step.Start();
                commands.Add(cmd.CommandId);
            }
            catch (Exception ex)
            {
                step.Fail(ex.GetType().Name);
                plan.Fail($"COMMAND_CREATION_FAILED_{ex.GetType().Name}");
                await _planRepo.UpdateAsync(plan, ct);
                await _resourceManager.ReleaseAsync(plan, ct);
                return new ExecutionResult(false, ex.Message);
            }

            await _planRepo.UpdateAsync(plan, ct);
        }

        plan.StartExecuting();
        await _planRepo.UpdateAsync(plan, ct);
        return new ExecutionResult(true, null, commands);
    }

    public async Task HandleCommandTerminalEventAsync(CommandPlan plan, Guid stepId, string commandStatus, CancellationToken ct = default)
    {
        var step = plan.Steps.FirstOrDefault(s => s.Id == stepId);
        if (step is null) return;

        switch (commandStatus)
        {
            case "Succeeded":
                step.Succeed();
                break;
            case "Failed":
                step.Fail(EngineeringErrors.CommandPlanExecutionFailed);
                break;
            case "Cancelled":
                step.Cancel();
                break;
            case "TimedOut":
                step.Fail(EngineeringErrors.CommandPlanExecutionFailed);
                break;
        }

        if (commandStatus == "Failed" && step.Required)
        {
            plan.Fail(EngineeringErrors.CommandPlanExecutionFailed);
            await _resourceManager.ReleaseAsync(plan, ct);
        }
        else if (commandStatus == "Cancelled")
        {
            plan.Cancel();
            await _resourceManager.ReleaseAsync(plan, ct);
        }
        else
        {
            var allDone = plan.Steps.All(s => s.Status == CommandPlanStepStatus.Succeeded);
            var anyFailed = plan.Steps.Any(s => s.Status == CommandPlanStepStatus.Failed);
            
            if (allDone)
            {
                plan.SetSucceeded();
                await _resourceManager.ReleaseAsync(plan, ct);
            }
            else if (anyFailed)
            {
                var requiredFailed = plan.Steps.Any(s => s.Status == CommandPlanStepStatus.Failed && s.Required);
                if (requiredFailed)
                {
                    plan.Fail(EngineeringErrors.CommandPlanExecutionFailed);
                    await _resourceManager.ReleaseAsync(plan, ct);
                }
                else
                {
                    plan.SetPartiallySucceeded();
                }
            }
        }

        await _planRepo.UpdateAsync(plan, ct);
    }

    public async Task<ExecutionResult> ExecuteHvacPlanAsync(CommandPlan plan, RoomId roomId,
        EngineeringSystem system, VentilationCommandPlanDefinition hvacPlan,
        CancellationToken ct = default)
    {
        var supplyCode = system.GetResource("supply_airflow_capacity") is not null
            ? "supply_airflow_capacity" : "airflow_capacity";
        var exhaustCode = system.GetResource("exhaust_airflow_capacity") is not null
            ? "exhaust_airflow_capacity" : "airflow_capacity";

        plan.SetPlanning();
        await _planRepo.UpdateAsync(plan, ct);

        var supplyResource = system.GetResource(supplyCode);
        if (supplyResource is null || !supplyResource.CanAllocate(hvacPlan.TotalSupplyAirflow))
        {
            plan.Fail(EngineeringErrors.AirflowCapacityInsufficient);
            await _planRepo.UpdateAsync(plan, ct);
            return new ExecutionResult(false, EngineeringErrors.AirflowCapacityInsufficient);
        }
        supplyResource.Reserve(hvacPlan.TotalSupplyAirflow);
        plan.AddResourceAllocation(supplyCode, hvacPlan.TotalSupplyAirflow);

        var exhaustResource = system.GetResource(exhaustCode);
        if (exhaustResource is not null && supplyCode != exhaustCode)
        {
            if (!exhaustResource.CanAllocate(hvacPlan.TotalExhaustAirflow))
            {
                plan.Fail(EngineeringErrors.AirflowCapacityInsufficient);
                await _planRepo.UpdateAsync(plan, ct);
                await _resourceManager.ReleaseAsync(plan, ct);
                return new ExecutionResult(false, EngineeringErrors.AirflowCapacityInsufficient);
            }
            exhaustResource.Reserve(hvacPlan.TotalExhaustAirflow);
            plan.AddResourceAllocation(exhaustCode, hvacPlan.TotalExhaustAirflow);
        }

        plan.Allocate();
        await _systemRepo.UpdateAsync(system, ct);
        await _planRepo.UpdateAsync(plan, ct);

        var commands = new List<CommandIdDto>();
        var currentParallelGroup = new List<CommandPlanStep>();
        ExecutionMode? currentMode = null;

        foreach (var step in plan.Steps.OrderBy(s => s.Sequence))
        {
            if (!string.IsNullOrEmpty(step.DeviceRole))
            {
                var selection = await _deviceSelector.SelectByRoleAsync(system, step.DeviceRole,
                    step.CapabilityCode, ct);
                if (!selection.IsSuccess)
                {
                    await ExecuteSafeStopAsync(plan, system, ct);
                    plan.Fail($"{EngineeringErrors.RequiredDeviceNotFound}_{step.DeviceRole}");
                    await _planRepo.UpdateAsync(plan, ct);
                    await _resourceManager.ReleaseAsync(plan, ct);
                    return new ExecutionResult(false, selection.FailureCode);
                }
                step.DeviceId = selection.DeviceId!.Value.Value;
            }

            if (step.ExecutionMode == ExecutionMode.Parallel)
            {
                currentParallelGroup.Add(step);
                currentMode = ExecutionMode.Parallel;
                continue;
            }

            if (currentMode == ExecutionMode.Parallel && currentParallelGroup.Count > 0)
            {
                var groupResult = await ExecuteParallelGroupAsync(currentParallelGroup, plan, system, roomId, ct);
                commands.AddRange(groupResult);
                currentParallelGroup.Clear();
                currentMode = null;
            }

            var cmdResult = await ExecuteSingleStepAsync(step, plan, system, roomId, ct);
            if (cmdResult is null)
            {
                await ExecuteSafeStopAsync(plan, system, ct);
                plan.Fail($"STEP_FAILED_AT_SEQUENCE_{step.Sequence}");
                await _planRepo.UpdateAsync(plan, ct);
                await _resourceManager.ReleaseAsync(plan, ct);
                return new ExecutionResult(false, step.FailureCode);
            }
            commands.Add(cmdResult.Value);
        }

        if (currentMode == ExecutionMode.Parallel && currentParallelGroup.Count > 0)
        {
            var groupResult = await ExecuteParallelGroupAsync(currentParallelGroup, plan, system, roomId, ct);
            commands.AddRange(groupResult);
        }

        plan.StartExecuting();
        await _planRepo.UpdateAsync(plan, ct);
        return new ExecutionResult(true, null, commands);
    }

    private async Task<List<CommandIdDto>> ExecuteParallelGroupAsync(
        List<CommandPlanStep> steps, CommandPlan plan, EngineeringSystem system,
        RoomId roomId, CancellationToken ct)
    {
        var tasks = steps.Select(step =>
            ExecuteSingleStepAsync(step, plan, system, roomId, ct));
        var results = await Task.WhenAll(tasks);
        return results.Where(r => r.HasValue).Select(r => r!.Value).ToList();
    }

    private async Task<CommandIdDto?> ExecuteSingleStepAsync(
        CommandPlanStep step, CommandPlan plan, EngineeringSystem system,
        RoomId roomId, CancellationToken ct)
    {
        step.SetReady();
        await _planRepo.UpdateAsync(plan, ct);

        var parJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            speedPct = step.RequestedValue,
            positionPct = step.RequestedValue,
            outputPct = step.RequestedValue,
            enabled = step.RequestedValue > 0
        });

        var cmdReq = new CreateCommandRequestDto(
            BuildingId: system.BuildingId.ToString(),
            RoomId: roomId.ToString(),
            DeviceId: step.DeviceId?.ToString() ?? string.Empty,
            CapabilityCode: step.CapabilityCode,
            Operation: step.Operation,
            ParametersJson: parJson,
            CreatedBy: "system:engineering-systems:hvac",
            CorrelationId: null);

        try
        {
            var cmd = await _commandsModule.CreateAsync(cmdReq, ct);
            step.Start();
            await _planRepo.UpdateAsync(plan, ct);
            return cmd.CommandId;
        }
        catch (Exception ex)
        {
            step.Fail(ex.GetType().Name);
            return null;
        }
    }

    private async Task ExecuteSafeStopAsync(CommandPlan plan, EngineeringSystem system, CancellationToken ct)
    {
        var stopSteps = _safeStopPlanner.PlanSafeStop(plan, system);
        foreach (var stopStep in stopSteps)
        {
            var selection = await _deviceSelector.SelectByRoleAsync(system, stopStep.DeviceRole!,
                stopStep.CapabilityCode, ct);
            if (!selection.IsSuccess) continue;

            var parJson = System.Text.Json.JsonSerializer.Serialize(new { speedPct = 0, positionPct = 15, outputPct = 0, enabled = false });
            var cmdReq = new CreateCommandRequestDto(
                BuildingId: system.BuildingId.ToString(),
                RoomId: null,
                DeviceId: selection.DeviceId!.Value.ToString()!,
                CapabilityCode: stopStep.CapabilityCode,
                Operation: stopStep.Operation,
                ParametersJson: parJson,
                CreatedBy: "system:engineering-systems:hvac-safe-stop",
                CorrelationId: null);

            try { await _commandsModule.CreateAsync(cmdReq, ct); }
            catch { }
        }
    }
}

public record ExecutionResult(bool Success, string? FailureCode, List<CommandIdDto>? CommandIds = null);