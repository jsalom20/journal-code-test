import { notFound } from "next/navigation"

import { BookCirculationPanel } from "@/components/forms/book-circulation-panel"
import { PageHeader } from "@/components/page-header"
import { SectionCard } from "@/components/section-card"
import { StatusBadge } from "@/components/status-badge"
import { formatDate } from "@/lib/format"
import { ApiError, getBook, getBorrowers } from "@/lib/api"

export default async function BookDetailPage({
  params,
}: {
  params: Promise<{ id: string }>
}) {
  const { id } = await params
  let book: Awaited<ReturnType<typeof getBook>>
  let borrowers: Awaited<ReturnType<typeof getBorrowers>>

  try {
    ;[book, borrowers] = await Promise.all([
      getBook(id),
      getBorrowers({ status: "Active" }),
    ])
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound()
    }

    throw error
  }

  const checkoutCopies = book.copies
    .filter((copy) => copy.circulationStatus === "Available" || copy.circulationStatus === "Reserved")
    .map((copy) => ({
      id: copy.id,
      inventoryNumber: copy.inventoryNumber,
      barcode: copy.barcode,
    }))

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Book detail"
        title={book.title}
        description={`${book.authors.join(", ")} · ${book.language} · ${book.publisher ?? "No publisher"} · ${book.isbn13}`}
      />

      <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <SectionCard>
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">Copies</p>
          <div className="mt-5 space-y-4">
            {book.copies.map((copy) => (
              <div key={copy.id} className="rounded-[1.75rem] border border-stone-200 bg-stone-50/70 p-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <p className="font-semibold text-stone-950">
                      {copy.inventoryNumber} · {copy.barcode}
                    </p>
                    <p className="mt-1 text-sm text-stone-600">
                      {copy.shelfLocation} · acquired {formatDate(copy.acquiredAtUtc)}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <StatusBadge value={copy.circulationStatus} />
                    <StatusBadge value={copy.conditionStatus} />
                  </div>
                </div>

                {copy.notes ? <p className="mt-3 text-sm text-stone-700">{copy.notes}</p> : null}
              </div>
            ))}
          </div>
        </SectionCard>

        <div className="space-y-6">
          <SectionCard>
            <BookCirculationPanel
              bookId={book.id}
              borrowers={borrowers}
              checkoutCopies={checkoutCopies}
            />
          </SectionCard>

          <SectionCard>
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">Reservation queue</p>
            <div className="mt-4 space-y-3">
              {book.reservationQueue.map((reservation) => (
                <div key={reservation.id} className="rounded-[1.5rem] border border-stone-200 bg-stone-50/70 p-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="font-semibold text-stone-950">{reservation.borrowerName}</p>
                      <p className="mt-1 text-sm text-stone-600">{reservation.cardNumber}</p>
                    </div>
                    <StatusBadge value={reservation.status} />
                  </div>
                  <p className="mt-3 text-sm text-stone-700">Queued {formatDate(reservation.queuedAtUtc)}</p>
                </div>
              ))}
              {book.reservationQueue.length === 0 ? (
                <p className="text-sm text-stone-600">No active reservations on this title.</p>
              ) : null}
            </div>
          </SectionCard>

          <SectionCard>
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">Audit trail</p>
            <div className="mt-4 space-y-3">
              {book.recentEvents.map((event) => (
                <div key={event.id} className="rounded-[1.5rem] border border-stone-200 bg-stone-50/70 p-4">
                  <div className="flex items-center justify-between gap-3">
                    <StatusBadge value={event.eventType} />
                    <p className="text-xs text-stone-500">{formatDate(event.occurredAtUtc)}</p>
                  </div>
                  <p className="mt-3 text-sm text-stone-800">{event.description}</p>
                  {event.borrowerName ? (
                    <p className="mt-2 text-xs uppercase tracking-[0.18em] text-stone-500">{event.borrowerName}</p>
                  ) : null}
                </div>
              ))}
            </div>
          </SectionCard>
        </div>
      </div>
    </div>
  )
}
