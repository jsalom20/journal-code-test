# EF Core Syntax Note

## Modern syntax that did not work

While implementing the backend, I used modern C# pattern matching inside Entity Framework queries, for example:

```csharp
reservation.Status is ReservationStatus.Active or ReservationStatus.ReadyForPickup
```

This reads well in normal C# code, but it failed inside EF Core LINQ queries with:

`CS8122: An expression tree may not contain an 'is' pattern-matching operator.`

## Why it failed

The issue was not that the syntax is invalid C#. The issue was that EF Core receives many LINQ queries as expression trees so it can translate them into SQL.

Pattern matching syntax like:

```csharp
entity.Status is A or B
```

does not translate cleanly when it appears inside an expression tree that EF Core needs to inspect and convert into SQL.

## What I changed instead

I replaced those expressions with explicit boolean comparisons:

```csharp
entity.Status == ReservationStatus.Active ||
entity.Status == ReservationStatus.ReadyForPickup
```

## Why I chose the simpler syntax

- It is fully compatible with EF Core query translation.
- It is obvious to other developers and reviewers what SQL shape is intended.
- It avoids surprises where valid C# syntax works in in-memory code but fails in database-backed LINQ.
- In a code test, predictable persistence behavior is more important than using the newest syntax everywhere.

## Takeaway

Modern C# syntax is fine in regular application logic, but for EF Core queries I will prefer the most translation-safe form unless I know the provider supports the newer expression shape.
