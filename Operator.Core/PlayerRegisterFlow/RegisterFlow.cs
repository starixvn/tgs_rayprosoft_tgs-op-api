using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using static TGS.Services.Operator.PlayerRegisterFlow.CreatePlayerRegisterFlowStep;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class RegisterFlow : IRegisterFlowStep
{
    private readonly Queue<IRegisterFlowStep> _flowSteps = new Queue<IRegisterFlowStep>();
    private readonly ILogger<RegisterFlow> _logger;

    public string? CurrentStep => _flowSteps.Count == 0 ? null : _flowSteps.Peek().Type;

    public string Type => "RegisterFlow";

    public RegisterFlow(IEnumerable<IRegisterFlowStep> steps, ILogger<RegisterFlow> logger)
    {
        foreach (var step in steps)
            _flowSteps.Enqueue(step);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> ConfirmCompletedAsync(string? playerCode, CancellationToken cancellationToken = default)
    {
        if (_flowSteps.Count == 0)
            return true;

        while (_flowSteps.Count > 0)
        {
            var isCompleted = await _flowSteps.Peek()
                .ConfirmCompletedAsync(playerCode, cancellationToken)
                .ConfigureAwait(false);

            if (isCompleted)
                _flowSteps.Dequeue();
            else
                return false;
        }

        return true;
    }

    public async ValueTask<RegisterFlowProcessResult> ProcessInputAsync(
        Guid brandId,
        JsonDocument input,
        PlayerLocationInfo locationInfo,
        CancellationToken cancellationToken = default)
    {
        var currentStep = _flowSteps.Peek();

        var result = await currentStep
            .ProcessInputAsync(brandId, input, locationInfo, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Process result: {result}", result);

        if (result.IsCompleted)
        {
            _flowSteps.Dequeue();

            var isCompleted = await ConfirmCompletedAsync(locationInfo.PlayerCode).ConfigureAwait(false);

            if (!isCompleted && _flowSteps.Peek().Type == "Final")
            {
                _logger.LogInformation("Register Flow Final Step Process Start");

                if (currentStep.Type == "CreatePlayer" && result.StepResponse is not null)
                {
                    var createPlayerResponse = result.StepResponse.Deserialize<CreatePlayerResponse>()!;
                    var finalResult = await ProcessInputAsync(
                        brandId,
                        input,
                        locationInfo with { PlayerCode = createPlayerResponse.PlayerCode },
                        cancellationToken).ConfigureAwait(false);

                    return finalResult with
                    {
                        StepResponse = result.StepResponse
                    };
                }

                return await ProcessInputAsync(
                    brandId,
                    input,
                    locationInfo,
                    cancellationToken).ConfigureAwait(false);
            }

            return new RegisterFlowProcessResult(
                isCompleted,
                isCompleted ? null : _flowSteps.Peek().Type,
                result.StepResponse);
        }
        else
            return new RegisterFlowProcessResult(
                false,
                currentStep.Type,
                result.StepResponse);
    }
}
