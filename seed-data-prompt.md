# Seed Data Prompt

Use the prompt below with an LLM when generating or regenerating seed data for this project.

---

You are generating seed data for a library circulation app built with:

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQLite

This seed data is for a code test. The goals are:

1. Make the app feel realistic immediately.
2. Exercise the important circulation flows.
3. Give the UI enough variety to look credible under demos, filters, and edge cases.
4. Preserve physical-book traceability at the copy level.

## Domain model

Generate seed data that matches this domain:

### Book
- `Id`
- `Title`
- `Isbn13`
- `Language`
- `Publisher`
- `Summary`
- `PublicationYear`
- `CreatedAtUtc`
- `UpdatedAtUtc`

### Author
- `Id`
- `Name`

### BookAuthor
- many-to-many link between `Book` and `Author`

### BookCopy
- `Id`
- `BookId`
- `Barcode`
- `InventoryNumber`
- `ShelfLocation`
- `ConditionStatus`
- `CirculationStatus`
- `AcquiredAtUtc`
- `LastInventoryCheckAtUtc`
- `Notes`
- `ConcurrencyToken`

### Borrower
- `Id`
- `CardNumber`
- `FirstName`
- `LastName`
- `Email`
- `Status`
- `CreatedAtUtc`

### Loan
- `Id`
- `CopyId`
- `BorrowerId`
- `CheckedOutAtUtc`
- `DueAtUtc`
- `ReturnedAtUtc`
- `CheckoutCondition`
- `ReturnCondition`
- `CheckoutNotes`
- `ReturnNotes`
- `ConcurrencyToken`

### Reservation
- `Id`
- `BookId`
- `BorrowerId`
- `QueuedAtUtc`
- `Status`
- `AssignedCopyId`
- `ReadyForPickupAtUtc`
- `FulfilledAtUtc`
- `CancelledAtUtc`
- `ConcurrencyToken`

### CopyEvent
- `Id`
- `CopyId`
- `LoanId`
- `ReservationId`
- `BorrowerId`
- `EventType`
- `Description`
- `MetadataJson`
- `OccurredAtUtc`

## Enum values

### BorrowerStatus
- `Active`
- `Suspended`

### CopyConditionStatus
- `Excellent`
- `Good`
- `Fair`
- `Damaged`
- `Lost`

### CirculationStatus
- `Available`
- `OnLoan`
- `Reserved`
- `Repair`
- `Lost`
- `Withdrawn`

### ReservationStatus
- `Active`
- `ReadyForPickup`
- `Fulfilled`
- `Cancelled`

### CopyEventType
- `Created`
- `StatusChanged`
- `CheckedOut`
- `Returned`
- `ReservationPlaced`
- `ReservationAssigned`
- `ReservationCancelled`
- `InventoryChecked`
- `ConditionUpdated`

## Required scale

Generate roughly:

- `1,000` books
- `2,500` physical copies
- `100` borrowers

Do not generate a tiny sample. The dataset should be large enough that:

- search feels real
- pagination matters
- multiple copies per title are normal
- overdue lists and reservation queues are non-trivial

## Data realism requirements

### Catalog quality
- Books should have believable titles, not placeholder garbage like `Book 1`, unless used only in a clearly limited test subset.
- Include a mix of:
  - literary fiction
  - crime / thriller
  - fantasy / sci-fi
  - history / biography
  - children / YA
  - practical nonfiction
- Vary languages, but keep `Swedish` and `English` dominant.
- Use realistic author names.
- Some books should have one author, some two, and a small minority three.
- Summaries should be short and credible, not lorem ipsum.

### Physical copy realism
- Every copy must have a unique `Barcode`.
- Every copy must have a unique `InventoryNumber`.
- Shelf locations should look like real library shelf codes, for example:
  - `A3-4`
  - `B7-2`
  - `NF-12`
  - `YA-5`
- Most copies should be `Good`.
- A meaningful minority should be `Fair`.
- A few should be `Damaged`, `Lost`, `Repair`, or `Withdrawn`.
- Some copies should include operational notes.

### Borrower realism
- Borrowers should have believable Swedish-style names.
- Card numbers should follow a consistent pattern like `LIB-0001`.
- A few borrowers should be `Suspended`.
- Email addresses should look realistic enough for a demo.

## Required circulation scenarios

The seed data must cover these app behaviors:

### Availability mix
- Many books with at least one `Available` copy.
- Many books where all copies are `OnLoan`.
- Some books with one `Reserved` copy and other copies still available.
- Some books with a single copy only.
- Some books with 4-6 copies.

### Loans
- Active loans that are not overdue.
- Overdue loans with different overdue ages:
  - 1-3 days
  - 4-10 days
  - 10+ days
- Historical returned loans.
- Loans with varied `CheckoutCondition`.
- Some returned loans with `ReturnCondition` worse than checkout condition.

### Reservations
- Active queued reservations with no assigned copy yet.
- `ReadyForPickup` reservations with assigned copies.
- At least a few titles with a queue length of 2-4.
- Some cancelled and fulfilled reservations in history.

### Copy events
- Enough `CopyEvent` rows to make the audit trail useful in the UI.
- Include event sequences like:
  - checkout -> return
  - reservation placed -> reservation assigned
  - condition updated
  - status changed to repair or lost

## Business-rule consistency

Respect these rules:

- One active loan per copy.
- One active reservation per borrower per book.
- `Reserved` copies should usually correspond to a `ReadyForPickup` reservation with `AssignedCopyId`.
- `OnLoan` copies should have an active `Loan`.
- `ReturnedAtUtc` must be null only for active loans.
- Overdue state is derived from `DueAtUtc` and current time, not stored directly.
- Suspended borrowers should not be given new active reservations or fresh active loans unless you are intentionally generating a legacy edge case and explicitly marking it as such.

## Distribution targets

Aim for something close to:

- `120` active non-overdue loans
- `35` overdue loans
- `40` historical returned loans
- `15` ready-for-pickup reservations
- `20` active queued reservations
- `5` suspended borrowers

These are not hard limits, but stay close enough that the UI looks balanced.

## Output format

Output valid C# seed-building code for this project.

Requirements for the code:

- Use the real entity names from this project.
- Use deterministic generation with a fixed seed.
- Generate `Guid`s in code, not placeholders like `GUID_HERE`.
- Avoid pseudocode.
- Avoid comments that say "fill this in later".
- Return code that can be pasted into a `LibrarySeeder` implementation with minimal editing.

Preferred output structure:

1. small reusable arrays for first names, last names, title parts, publishers, languages
2. code to build authors
3. code to build borrowers
4. code to build books and copies
5. code to build loans
6. code to build reservations
7. code to build copy events
8. final `AddRange` / save sequence

## Style requirements for generated code

- Keep it readable and compact.
- Favor helper methods when repetition becomes noisy.
- Do not invent infrastructure outside the seed-data concern.
- Do not add unrelated refactors.
- Do not add authentication, payments, notifications, or branch logic.

## Final instruction

Generate the seed data code so that a reviewer using the app immediately sees:

- a rich searchable catalog
- real copy-level inventory states
- meaningful overdue pressure
- visible reservation queues
- believable borrower activity
- useful audit history

Only output the C# code and any tiny helper methods needed for the seeder.

---
