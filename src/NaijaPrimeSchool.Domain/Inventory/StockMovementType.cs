using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Inventory;

public class StockMovementType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    // +1 = inbound (stock goes up), -1 = outbound (stock goes down).
    // The Direction column lets the service layer compute net stock from
    // a stream of movements without keying off the row name.
    public int Direction { get; set; }

    public int DisplayOrder { get; set; }

    public ICollection<StockMovement> Movements { get; set; } = [];
}
