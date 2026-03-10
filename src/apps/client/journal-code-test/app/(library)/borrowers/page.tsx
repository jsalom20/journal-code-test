import Link from "next/link"

import { PageHeader } from "@/components/page-header"
import { SectionCard } from "@/components/section-card"
import { StatusBadge } from "@/components/status-badge"
import { formatCurrency } from "@/lib/format"
import { getBorrowers } from "@/lib/api"
import { readSingleParam } from "@/lib/search-params"

export default async function BorrowersPage({
  searchParams,
}: {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>
}) {
  const params = await searchParams
  const query = readSingleParam(params.query)
  const status = readSingleParam(params.status)
  const borrowers = await getBorrowers({ query, status })

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Borrowers"
        title="Patron management"
        description="Seeded borrowers stand in for authentication in this code test, but the desk still needs loan pressure, fines, and reservation load visible by patron."
      />

      <SectionCard>
        <form className="grid gap-3 md:grid-cols-[1fr_220px_auto]">
          <input
            name="query"
            defaultValue={query}
            placeholder="Name, email, or card number"
            className="rounded-2xl border border-stone-300 bg-white px-4 py-3 text-sm"
          />
          <select
            name="status"
            defaultValue={status}
            className="rounded-2xl border border-stone-300 bg-white px-4 py-3 text-sm"
          >
            <option value="">All statuses</option>
            <option value="Active">Active</option>
            <option value="Suspended">Suspended</option>
          </select>
          <button className="rounded-2xl bg-stone-900 px-4 py-3 text-sm font-semibold text-white">
            Search
          </button>
        </form>
      </SectionCard>

      <SectionCard className="overflow-hidden p-0">
        <div className="grid grid-cols-[1.3fr_1fr_140px_120px_120px] gap-4 border-b border-stone-200 px-5 py-3 text-xs font-semibold uppercase tracking-[0.18em] text-stone-500">
          <span>Borrower</span>
          <span>Contact</span>
          <span>Status</span>
          <span>Loans</span>
          <span>Fines</span>
        </div>
        {borrowers.map((borrower) => (
          <Link
            key={borrower.id}
            href={`/borrowers/${borrower.id}`}
            className="grid grid-cols-[1.3fr_1fr_140px_120px_120px] gap-4 border-b border-stone-100 px-5 py-4 text-sm transition hover:bg-stone-50/70"
          >
            <div>
              <p className="font-semibold text-stone-950">{borrower.fullName}</p>
              <p className="mt-1 text-stone-600">{borrower.cardNumber}</p>
            </div>
            <p className="text-stone-600">{borrower.email}</p>
            <div>
              <StatusBadge value={borrower.status} />
            </div>
            <p className="font-medium text-stone-900">{borrower.activeLoansCount}</p>
            <p className="font-medium text-stone-900">{formatCurrency(borrower.outstandingFineSek)}</p>
          </Link>
        ))}
      </SectionCard>
    </div>
  )
}
