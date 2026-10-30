namespace ChikagoShop.Domain.Configuration;

public sealed class EconomyConfigException : Exception
{
    public EconomyConfigException(string message) : base(message)
    {
    }

    public EconomyConfigException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
