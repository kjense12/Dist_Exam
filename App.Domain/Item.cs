using App.Domain.Identity;
using Base.Domain;

namespace App.Domain;

public class Item : DomainEntityMetaId
{
    public string ItemName { get; set; } = default!;
    
    public Guid AppUserId { get; set; }
    public AppUser? AppUser { get; set; }
}