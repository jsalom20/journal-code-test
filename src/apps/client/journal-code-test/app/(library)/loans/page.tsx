import Link from "next/link"

import { ReturnLoanButton } from "@/components/forms/return-loan-button"
import { PageHeader } from "@/components/page-header"
import { SectionCard } from "@/components/section-card"
import { StatusBadge } from "@/components/status-badge"
import { formatCurrency, formatDate } from "@/lib/format"
import { getLoans } from "@/lib/api"
import { readSingleParam } from "@/lib/search-params"

export default async function LoansPage({
  searchParams,
}: {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>
}) {
  const params = await searchParams
  const status = readSingleParam(params.status, "active")
  const loans = await getLoans({ status })

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Circulation"
        title="Loan board"
        description="This is the operational surface for returns, overdue triage, and copy state changes that happen as books come back to the desk."
      />

      <SectionCard className="flex flex-wrap gap-3">
        {[
          { label: "Active", value: "active" },
          { label: "Overdue", value: "overdue" },
          { label: "History", value: "history" },
        ].map((filter) => (
          <Link
            key={filter.value}
            href={`/loans?status=${filter.value}`}
            className={[
              "rounded-2xl border px-4 py-2 text-sm font-medium",
              status === filter.value
                ? "border-stone-900 bg-stone-900 text-white"
                : "border-stone-300 bg-white text-stone-700",
            ].join(" ")}
          >
            {filter.label}
          </Link>
        ))}
      </SectionCard>

      <div className="space-y-4">
        {loans.map((loan) => (
          <SectionCard key={loan.loanId}>
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.2em] text-stone-500">{loan.cardNumber}</p>
                <h3 className="mt-2 text-2xl font-semibold text-stone-950">{loan.bookTitle}</h3>
                <p className="mt-2 text-sm text-stone-600">
                  {loan.borrowerName} · Copy {loan.barcode}
                </p>
              </div>
              <div className="flex gap-2">
                {loan.isOverdue ? <StatusBadge value="Overdue" /> : null}
                <StatusBadge value={loan.returnedAtUtc ? "Returned" : "OnLoan"} />
              </div>
            </div>
            <div className="mt-4 flex flex-wrap gap-4 text-sm text-stone-700">
              <span>Checked out {formatDate(loan.checkedOutAtUtc)}</span>
              <span>Due {formatDate(loan.dueAtUtc)}</span>
              <span>{formatCurrency(loan.fineSek)}</span>
            </div>
            {!loan.returnedAtUtc ? (
              <div className="mt-4">
                <ReturnLoanButton loanId={loan.loanId} />
              </div>
            ) : null}
          </SectionCard>
        ))}
      </div>
    </div>
  )
}
