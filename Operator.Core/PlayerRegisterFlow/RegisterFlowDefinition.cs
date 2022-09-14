using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class RegisterFlowDefinition
{
    private readonly ILogger<RegisterFlow> _logger;
    private List<IRegisterFlowStep> _stepDefinitions = new();

    public RegisterFlowDefinition(ILogger<RegisterFlow> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void AddStep(IRegisterFlowStep step)
    {
        _stepDefinitions.Add(step);
    }

    public RegisterFlow BuildRegisterFlow()
        => new RegisterFlow(_stepDefinitions, _logger);
}
