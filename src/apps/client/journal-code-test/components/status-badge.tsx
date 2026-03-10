import { cn } from "@/lib/utils"

const toneMap: Record<string, string> = {
  Available: "bg-emerald-50 text-emerald-700 ring-emerald-200",
  Active: "bg-sky-50 text-sky-700 ring-sky-200",
  Good: "bg-sky-50 text-sky-700 ring-sky-200",
  OnLoan: "bg-amber-50 text-amber-700 ring-amber-200",
  Overdue: "bg-rose-50 text-rose-700 ring-rose-200",
  ReadyForPickup: "bg-fuchsia-50 text-fuchsia-700 ring-fuchsia-200",
  Reserved: "bg-fuchsia-50 text-fuchsia-700 ring-fuchsia-200",
  Suspended: "bg-rose-50 text-rose-700 ring-rose-200",
  Damaged: "bg-orange-50 text-orange-700 ring-orange-200",
  Repair: "bg-orange-50 text-orange-700 ring-orange-200",
  Lost: "bg-rose-50 text-rose-700 ring-rose-200",
  Withdrawn: "bg-stone-100 text-stone-700 ring-stone-300",
  Fulfilled: "bg-emerald-50 text-emerald-700 ring-emerald-200",
  Cancelled: "bg-stone-100 text-stone-700 ring-stone-300",
  Fair: "bg-yellow-50 text-yellow-700 ring-yellow-200",
  Excellent: "bg-emerald-50 text-emerald-700 ring-emerald-200",
  Created: "bg-stone-100 text-stone-700 ring-stone-300",
  StatusChanged: "bg-stone-100 text-stone-700 ring-stone-300",
  CheckedOut: "bg-amber-50 text-amber-700 ring-amber-200",
  Returned: "bg-emerald-50 text-emerald-700 ring-emerald-200",
  ReservationPlaced: "bg-sky-50 text-sky-700 ring-sky-200",
  ReservationAssigned: "bg-fuchsia-50 text-fuchsia-700 ring-fuchsia-200",
  ReservationCancelled: "bg-stone-100 text-stone-700 ring-stone-300",
  InventoryChecked: "bg-indigo-50 text-indigo-700 ring-indigo-200",
  ConditionUpdated: "bg-orange-50 text-orange-700 ring-orange-200",
}

export function StatusBadge({
  value,
  className,
}: {
  value: string
  className?: string
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset",
        toneMap[value] ?? "bg-stone-100 text-stone-700 ring-stone-300",
        className
      )}
    >
      {value}
    </span>
  )
}
