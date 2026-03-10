"use client"

import { useRouter } from "next/navigation"
import { useState, useTransition } from "react"

import { Button } from "@/components/ui/button"
import { getApiBaseUrl } from "@/lib/api"
import type { CirculationStatus, CopyConditionStatus, CopyListItem } from "@/lib/types"

const circulationOptions: CirculationStatus[] = [
  "Available",
  "OnLoan",
  "Reserved",
  "Repair",
  "Lost",
  "Withdrawn",
]
const conditionOptions: CopyConditionStatus[] = ["Excellent", "Good", "Fair", "Damaged", "Lost"]

export function CopyUpdateForm({ copy }: { copy: CopyListItem }) {
  const router = useRouter()
  const [shelfLocation, setShelfLocation] = useState(copy.shelfLocation)
  const [circulationStatus, setCirculationStatus] = useState<CirculationStatus>(copy.circulationStatus)
  const [conditionStatus, setConditionStatus] = useState<CopyConditionStatus>(copy.conditionStatus)
  const [notes, setNotes] = useState(copy.notes ?? "")
  const [message, setMessage] = useState<string | null>(null)
  const [isPending, startTransition] = useTransition()

  function handleSave() {
    startTransition(async () => {
      try {
        const response = await fetch(`${getApiBaseUrl()}/api/copies/${copy.id}`, {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            bookId: copy.bookId,
            barcode: copy.barcode,
            inventoryNumber: copy.inventoryNumber,
            shelfLocation,
            conditionStatus,
            circulationStatus,
            acquiredAtUtc: copy.acquiredAtUtc,
            lastInventoryCheckAtUtc: new Date().toISOString(),
            notes,
          }),
        })

        if (!response.ok) {
          const error = (await response.json()) as { message?: string }
          throw new Error(error.message ?? "Inventory update failed.")
        }

        setMessage("Saved.")
        router.refresh()
      } catch (error) {
        setMessage(error instanceof Error ? error.message : "Inventory update failed.")
      }
    })
  }

  return (
    <div className="grid gap-2 md:grid-cols-[1fr_160px_160px_auto] md:items-center">
      <input
        value={shelfLocation}
        onChange={(event) => setShelfLocation(event.target.value)}
        className="rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
      />
      <select
        value={conditionStatus}
        onChange={(event) => setConditionStatus(event.target.value as CopyConditionStatus)}
        className="rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
      >
        {conditionOptions.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
      <select
        value={circulationStatus}
        onChange={(event) => setCirculationStatus(event.target.value as CirculationStatus)}
        className="rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
      >
        {circulationOptions.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
      <Button type="button" size="sm" onClick={handleSave} disabled={isPending}>
        {isPending ? "Saving..." : "Save"}
      </Button>
      <textarea
        value={notes}
        onChange={(event) => setNotes(event.target.value)}
        className="md:col-span-4 min-h-20 rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
        placeholder="Inventory notes"
      />
      {message ? <p className="md:col-span-4 text-xs text-stone-600">{message}</p> : null}
    </div>
  )
}
