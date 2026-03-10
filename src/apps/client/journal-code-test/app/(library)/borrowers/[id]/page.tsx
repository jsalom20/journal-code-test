import { notFound } from "next/navigation"

import { CancelReservationButton } from "@/components/forms/cancel-reservation-button"
import { PageHeader } from "@/components/page-header"
import { SectionCard } from "@/components/section-card"
import { StatusBadge } from "@/components/status-badge"
import { formatCurrency, formatDate } from "@/lib/format"
import { getBorrower } from "@/lib/api"

export default async function BorrowerDetailPage({
  params,
}: {
  params: Promise<{ id: string }>
}) {
  const { id } = await params

  try {
    const borrower = await getBorrower(id)

    return (
      <div className="space-y-6">
        <PageHeader
          eyebrow="Borrower detail"
          title={borrower.fullName}
          description={`${borrower.cardNumber} · ${borrower.email}`}
        />

        <div className="grid gap-4 md:grid-cols-3">
          <SectionCard className="md:col-span-2">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">Current loans</p>
            <div className="mt-4 space-y-3">
              {borrower.currentLoans.map((loan) => (
                <div key={loan.loanId} className="rounded-[1.5rem] border border-stone-200 bg-stone-50/70 p-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="font-semibold text-stone-950">{loan.bookTitle}</p>
                      <p className="mt-1 text-sm text-stone-600">Copy {loan.barcode}</p>
                    </div>
                    {loan.isOverdue ? <StatusBadge value="Overdue" /> : <StatusBadge value="OnLoan" />}
                  </div>
                  <div className="mt-3 flex flex-wrap gap-4 text-sm text-stone-700">
                    <span>Due {formatDate(loan.dueAtUtc)}</span>
                    <span>{formatCurrency(loan.fineSek)}</span>
                  </div>
                </div>
              ))}
              {borrower.currentLoans.length === 0 ? (
                <p className="text-sm text-stone-600">No active loans.</p>
              ) : null}
            </div>
          </SectionCard>

          <SectionCard>
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">Profile</p>
            <div className="mt-4 space-y-3 text-sm text-stone-700">
              <div className="flex items-center justify-between gap-3">
                <span>Status</span>
                <StatusBadge value={borrower.status} />
              </div>
              <div className="flex items-center justify-between gap-3">
                <span>Outstanding fines</span>
                <span className="font-semibold text-stone-950">{formatCurrency(borrower.outstandingFineSek)}</span>
              </div>
              <div className="flex items-center justify-between gap-3">
                <span>Reservations</span>
                <span className="font-semibold text-stone-950">{borrower.reservations.length}</span>
              </div>
            </div>
          </SectionCard>
        </div>

        <div className="grid gap-6 xl:grid-cols-[1fr_1fr]">
          <SectionCard>
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">Reservations</p>
            <div className="mt-4 space-y-3">
              {borrower.reservations.map((reservation) => (
                <div key={reservation.reservationId} className="rounded-[1.5rem] border border-stone-200 bg-stone-50/70 p-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="font-semibold text-stone-950">{reservation.bookTitle}</p>
                      <p className="mt-1 text-sm text-stone-600">Queued {formatDate(reservation.queuedAtUtc)}</p>
                    </div>
                    <StatusBadge value={reservation.status} />
                  </div>
                  {(reservation.status === "Active" || reservation.status === "ReadyForPickup") ? (
                    <div className="mt-3">
                      <CancelReservationButton reservationId={reservation.reservationId} />
                    </div>
                  ) : null}
                </div>
              ))}
              {borrower.reservations.length === 0 ? (
                <p className="text-sm text-stone-600">No reservations.</p>
              ) : null}
            </div>
          </SectionCard>

          <SectionCard>
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">Loan history</p>
            <div className="mt-4 space-y-3">
              {borrower.loanHistory.map((loan) => (
                <div key={loan.loanId} className="rounded-[1.5rem] border border-stone-200 bg-stone-50/70 p-4">
                  <p className="font-semibold text-stone-950">{loan.bookTitle}</p>
                  <p className="mt-1 text-sm text-stone-600">
                    Checked out {formatDate(loan.checkedOutAtUtc)} · Returned {formatDate(loan.returnedAtUtc)}
                  </p>
                </div>
              ))}
              {borrower.loanHistory.length === 0 ? (
                <p className="text-sm text-stone-600">No historical loans yet.</p>
              ) : null}
            </div>
          </SectionCard>
        </div>
      </div>
    )
  } catch {
    notFound()
  }
}
