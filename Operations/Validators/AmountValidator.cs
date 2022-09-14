using TGS.Core.Models.Exceptions;

namespace TGS.Services.Operations.Validators;

public interface IAmountValidator
{
    void ThrowIfAmountIsNotPositive(string name, decimal amount);
    void ThrowIfAmountIsPositive(string name, decimal amount);
    void ThrowIfAmountIsNegative(string name, decimal amount);
    void IgnoreAmount(string name, decimal amount);
}

public sealed class AmountValidator : IAmountValidator
{
    public void IgnoreAmount(string name, decimal amount)
    {
    }

    public void ThrowIfAmountIsNegative(string name, decimal amount)
    {
        if (amount < 0)
        {
            throw new InvalidAmountException($"The {name} must be zero or positive but is {amount}");
        }
    }

    public void ThrowIfAmountIsNotPositive(string name, decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidAmountException($"The {name} must be positive but is {amount}");
        }
    }

    public void ThrowIfAmountIsPositive(string name, decimal amount)
    {
        if (amount > 0)
        {
            throw new InvalidAmountException($"The {name} must be zero or negative but is {amount}");
        }
    }
}