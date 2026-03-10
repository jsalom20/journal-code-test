namespace Library.Domain.Enums;

public enum CopyEventType
{
    Created = 1,
    StatusChanged = 2,
    CheckedOut = 3,
    Returned = 4,
    ReservationPlaced = 5,
    ReservationAssigned = 6,
    ReservationCancelled = 7,
    InventoryChecked = 8,
    ConditionUpdated = 9
}
