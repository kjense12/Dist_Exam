using Base.Domain;

namespace App.Public.DTO.v1;

public class ItemDTO : DomainEntityId
{
    public string ItemName { get; set; } = default!;
}