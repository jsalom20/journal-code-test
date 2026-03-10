"use client"

import { useActionState } from "react"

import { initialMutationState, updateCopy } from "@/app/(library)/actions"
import { Button } from "@/components/ui/button"
import type { CirculationStatus, CopyConditionStatus, CopyListItem } from "@/lib/types"

const circulationOptions: CirculationStatus[] = [
  "Available",
  "Repair",
  "Lost",
  "Withdrawn",
]
const conditionOptions: CopyConditionStatus[] = ["Excellent", "Good", "Fair", "Damaged", "Lost"]

export function CopyUpdateForm({ copy }: { copy: CopyListItem }) {
  const [state, formAction, isPending] = useActionState(updateCopy, initialMutationState)
  const isWorkflowManagedStatus =
    copy.circulationStatus === "OnLoan" || copy.circulationStatus === "Reserved"
  const allowedCirculationOptions = isWorkflowManagedStatus
    ? [copy.circulationStatus]
    : circulationOptions

  return (
    <form action={formAction} className="grid gap-2 md:grid-cols-[1fr_160px_160px_auto] md:items-center">
      <input type="hidden" name="copyId" value={copy.id} />
      <input type="hidden" name="bookId" value={copy.bookId} />
      <input type="hidden" name="barcode" value={copy.barcode} />
      <input type="hidden" name="inventoryNumber" value={copy.inventoryNumber} />
      <input type="hidden" name="acquiredAtUtc" value={copy.acquiredAtUtc} />
      <input
        name="shelfLocation"
        defaultValue={copy.shelfLocation}
        className="rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
      />
      <select
        name="conditionStatus"
        defaultValue={copy.conditionStatus}
        className="rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
      >
        {conditionOptions.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
      <select
        name="circulationStatus"
        defaultValue={copy.circulationStatus}
        className="rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
        disabled={isWorkflowManagedStatus}
      >
        {allowedCirculationOptions.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
      <Button type="submit" size="sm" disabled={isPending}>
        {isPending ? "Saving..." : "Save"}
      </Button>
      <textarea
        name="notes"
        defaultValue={copy.notes ?? ""}
        className="md:col-span-4 min-h-20 rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
        placeholder="Inventory notes"
      />
      {isWorkflowManagedStatus ? (
        <p className="md:col-span-4 text-xs text-stone-500">
          {copy.circulationStatus} copies must move through checkout, return, and reservation workflows.
        </p>
      ) : null}
      {state.message ? (
        <p
          aria-live="polite"
          className={[
            "md:col-span-4 text-xs",
            state.status === "error" ? "text-rose-700" : "text-stone-600",
          ].join(" ")}
        >
          {state.message}
        </p>
      ) : null}
    </form>
  )
}
