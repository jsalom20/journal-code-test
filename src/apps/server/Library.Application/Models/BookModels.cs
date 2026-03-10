using Library.Domain.Enums;

namespace Library.Application.Models;

public sealed record BookListItem(
    Guid Id,
    string Title,
    string Isbn13,
    string Language,
    int? PublicationYear,
    string? Publisher,
    IReadOnlyList<string> Authors,
    int TotalCopies,
    int AvailableCopies,
    int OnLoanCopies,
    int ReservedCopies);

public sealed record CopySummary(
    Guid Id,
    string Barcode,
    string InventoryNumber,
    string ShelfLocation,
    CopyConditionStatus ConditionStatus,
    CirculationStatus CirculationStatus,
    DateTime AcquiredAtUtc,
    DateTime? LastInventoryCheckAtUtc,
    string? Notes);

public sealed record ReservationQueueItem(
    Guid Id,
    Guid BorrowerId,
    string BorrowerName,
    string CardNumber,
    ReservationStatus Status,
    DateTime QueuedAtUtc,
    Guid? AssignedCopyId,
    DateTime? ReadyForPickupAtUtc);

public sealed record CopyEventItem(
    Guid Id,
    Guid CopyId,
    CopyEventType EventType,
    string Description,
    DateTime OccurredAtUtc,
    string? BorrowerName);

public sealed record BookDetail(
    Guid Id,
    string Title,
    string Isbn13,
    string Language,
    int? PublicationYear,
    string? Publisher,
    string? Summary,
    IReadOnlyList<string> Authors,
    IReadOnlyList<CopySummary> Copies,
    IReadOnlyList<ReservationQueueItem> ReservationQueue,
    IReadOnlyList<CopyEventItem> RecentEvents);

public sealed record BookUpsertRequest(
    string Title,
    string Isbn13,
    string Language,
    string? Publisher,
    string? Summary,
    int? PublicationYear,
    IReadOnlyList<string> Authors);

public sealed record CopyListItem(
    Guid Id,
    Guid BookId,
    string BookTitle,
    string Barcode,
    string InventoryNumber,
    string ShelfLocation,
    CopyConditionStatus ConditionStatus,
    CirculationStatus CirculationStatus,
    DateTime AcquiredAtUtc,
    DateTime? LastInventoryCheckAtUtc,
    string? Notes);

public sealed record CopyUpsertRequest(
    Guid BookId,
    string Barcode,
    string InventoryNumber,
    string ShelfLocation,
    CopyConditionStatus ConditionStatus,
    CirculationStatus CirculationStatus,
    DateTime AcquiredAtUtc,
    DateTime? LastInventoryCheckAtUtc,
    string? Notes);
