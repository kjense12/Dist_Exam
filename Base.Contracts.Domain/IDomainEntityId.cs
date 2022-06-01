namespace Base.Contracts.Domain;


/// <summary>
/// default Guid based Domain Entity interface
/// </summary>
public interface IDomainEntityId : IDomainEntityId<Guid>
{
}

/// <summary>
/// Universal Domain Entity interface based on generic PK Type
/// </summary>
/// <typeparam name="TKey">Type for primary Key</typeparam>

public interface IDomainEntityId<TKey>
    where TKey: IEquatable<TKey>
{
    public TKey Id { get; set; }
}