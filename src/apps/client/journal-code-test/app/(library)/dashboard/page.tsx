import Link from "next/link"

import { PageHeader } from "@/components/page-header"
import { SectionCard } from "@/components/section-card"
import { StatCard } from "@/components/stat-card"
import { StatusBadge } from "@/components/status-badge"
import { formatCurrency, formatDate } from "@/lib/format"
import { getDashboardSummary, getLoans, getReservations } from "@/lib/api"

export default async function DashboardPage() {
  const [summary, overdueLoans, readyReservations] = await Promise.all([
    getDashboardSummary(),
    getLoans({ status: "overdue" }),
    getReservations({ status: "ReadyForPickup" }),
  ])

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Overview"
        title="Library operations at a glance"
        description="Keep the current loan pressure, reservation queue, and fine exposure visible before you move into detailed circulation work."
      />

      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Total books" value={summary.totalBooks} />
        <StatCard label="Available copies" value={summary.availableCopies} />
        <StatCard label="Overdue loans" value={summary.overdueLoans} tone="warning" />
        <StatCard label="Outstanding fines" value={summary.outstandingFinesSek} currency tone="warning" />
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <SectionCard>
          <div className="flex items-center justify-between gap-4">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">Overdue desk</p>
              <h3 className="mt-2 text-2xl font-semibold text-stone-950">Loans that need action</h3>
            </div>
            <Link href="/loans?status=overdue" className="text-sm font-medium text-stone-700 underline-offset-4 hover:underline">
              Open loan board
            </Link>
          </div>

          <div className="mt-5 space-y-3">
            {overdueLoans.slice(0, 6).map((loan) => (
              <div key={loan.loanId} className="rounded-[1.5rem] border border-amber-200 bg-amber-50/70 p-4">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <p className="font-semibold text-stone-950">{loan.bookTitle}</p>
                    <p className="mt-1 text-sm text-stone-600">
                      {loan.borrowerName} · {loan.cardNumber} · Copy {loan.barcode}
                    </p>
                  </div>
                  <StatusBadge value="Overdue" />
                </div>
                <div className="mt-3 flex flex-wrap gap-4 text-sm text-stone-700">
                  <span>Due {formatDate(loan.dueAtUtc)}</span>
                  <span>{formatCurrency(loan.fineSek)}</span>
                </div>
              </div>
            ))}

            {overdueLoans.length === 0 ? (
              <p className="rounded-[1.5rem] border border-stone-200 bg-stone-50/70 p-4 text-sm text-stone-600">
                No overdue loans right now.
              </p>
            ) : null}
          </div>
        </SectionCard>

        <SectionCard>
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">Pickup queue</p>
          <h3 className="mt-2 text-2xl font-semibold text-stone-950">Reservations ready now</h3>
          <div className="mt-5 space-y-3">
            {readyReservations.slice(0, 6).map((reservation) => (
              <div key={reservation.reservationId} className="rounded-[1.5rem] border border-fuchsia-200 bg-fuchsia-50/70 p-4">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="font-semibold text-stone-950">{reservation.bookTitle}</p>
                    <p className="mt-1 text-sm text-stone-600">
                      {reservation.borrowerName} · {reservation.cardNumber}
                    </p>
                  </div>
                  <StatusBadge value={reservation.status} />
                </div>
                <p className="mt-3 text-sm text-stone-700">
                  Assigned copy: {reservation.assignedCopyId?.slice(0, 8) ?? "Pending"}
                </p>
              </div>
            ))}

            {readyReservations.length === 0 ? (
              <p className="rounded-[1.5rem] border border-stone-200 bg-stone-50/70 p-4 text-sm text-stone-600">
                No ready reservations at the moment.
              </p>
            ) : null}
          </div>
        </SectionCard>
      </div>
    </div>
  )
}
