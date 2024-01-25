using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace TGS.Services.Operator.PlayerRegisterFlow;

internal class RegisterFlowDefinition(ILogger<RegisterFlow> logger)
{
	private readonly ILogger<RegisterFlow> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
	private readonly List<IRegisterFlowStep> _stepDefinitions = [];

	public void AddStep(IRegisterFlowStep step)
	{
		_stepDefinitions.Add(step);
	}

	public RegisterFlow BuildRegisterFlow()
		=> new(_stepDefinitions, _logger);
}
