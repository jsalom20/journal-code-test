import { CopyUpdateForm } from "@/components/forms/copy-update-form"
import { PageHeader } from "@/components/page-header"
import { SectionCard } from "@/components/section-card"
import { StatusBadge } from "@/components/status-badge"
import { formatDate } from "@/lib/format"
import { getCopies } from "@/lib/api"
import { readSingleParam } from "@/lib/search-params"

export default async function InventoryPage({
  searchParams,
}: {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>
}) {
  const params = await searchParams
  const status = readSingleParam(params.status)
  const condition = readSingleParam(params.condition)
  const copies = await getCopies({ status, condition })

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Inventory"
        title="Copy-level management"
        description="Physical libraries scale on copy-level traceability, not just title metadata. Location, condition, and circulation state need to be editable without losing history."
      />

      <SectionCard>
        <form className="grid gap-3 md:grid-cols-[220px_220px_auto]">
          <select name="status" defaultValue={status} className="rounded-2xl border border-stone-300 bg-white px-4 py-3 text-sm">
            <option value="">All statuses</option>
            <option value="Available">Available</option>
            <option value="OnLoan">On loan</option>
            <option value="Reserved">Reserved</option>
            <option value="Repair">Repair</option>
            <option value="Lost">Lost</option>
            <option value="Withdrawn">Withdrawn</option>
          </select>
          <select
            name="condition"
            defaultValue={condition}
            className="rounded-2xl border border-stone-300 bg-white px-4 py-3 text-sm"
          >
            <option value="">All conditions</option>
            <option value="Excellent">Excellent</option>
            <option value="Good">Good</option>
            <option value="Fair">Fair</option>
            <option value="Damaged">Damaged</option>
            <option value="Lost">Lost</option>
          </select>
          <button className="rounded-2xl bg-stone-900 px-4 py-3 text-sm font-semibold text-white">
            Filter
          </button>
        </form>
      </SectionCard>

      <div className="space-y-4">
        {copies.slice(0, 60).map((copy) => (
          <SectionCard key={copy.id}>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.2em] text-stone-500">{copy.bookTitle}</p>
                <h3 className="mt-2 text-xl font-semibold text-stone-950">
                  {copy.inventoryNumber} · {copy.barcode}
                </h3>
                <p className="mt-2 text-sm text-stone-600">
                  Last inventory check {formatDate(copy.lastInventoryCheckAtUtc)}
                </p>
              </div>
              <div className="flex gap-2">
                <StatusBadge value={copy.circulationStatus} />
                <StatusBadge value={copy.conditionStatus} />
              </div>
            </div>
            <div className="mt-4">
              <CopyUpdateForm copy={copy} />
            </div>
          </SectionCard>
        ))}
      </div>
    </div>
  )
}
