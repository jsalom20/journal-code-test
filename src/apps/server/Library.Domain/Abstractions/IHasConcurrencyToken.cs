namespace Library.Domain.Abstractions;

public interface IHasConcurrencyToken
{
    Guid ConcurrencyToken { get; set; }
}
