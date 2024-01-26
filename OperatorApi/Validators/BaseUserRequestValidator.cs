using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using FluentValidation;
using TGS.Apis.OperatorApi.Models.Messages;

namespace TGS.Apis.OperatorApi.Validators;

public abstract class BaseUserRequestValidator<T> : AbstractValidator<T> where T : BaseUserRequest
{
	protected BaseUserRequestValidator()
	{
		_ = RuleFor(x => x.Username)
			.NotEmpty()
			.Length(6, 32);
	}

	protected override void OnRuleAdded(IValidationRule<T> rule)
	{
		rule.PropertyName = GetJsonPropertyName(rule.Expression, rule.PropertyName);
		base.OnRuleAdded(rule);
	}

	private static TAttribute GetAttribute<TAttribute>(LambdaExpression expression) where TAttribute : Attribute
	{
		MemberExpression memberExpression = expression.Body as MemberExpression;

		return (memberExpression.Member is PropertyInfo element) ? element.GetCustomAttribute<TAttribute>() : null;
	}

	private static string GetJsonPropertyName(LambdaExpression expression, string defaultPropertyName)
		=> GetAttribute<JsonPropertyNameAttribute>(expression)?.Name ?? defaultPropertyName;
}
