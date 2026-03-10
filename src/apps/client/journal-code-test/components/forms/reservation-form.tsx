"use client"

import { useActionState } from "react"

import { initialMutationState, placeReservation } from "@/app/(library)/actions"
import { Button } from "@/components/ui/button"
import type { BorrowerListItem } from "@/lib/types"

export function ReservationForm({
  bookId,
  borrowers,
}: {
  bookId: string
  borrowers: BorrowerListItem[]
}) {
  const [state, formAction, isPending] = useActionState(placeReservation, initialMutationState)

  if (borrowers.length === 0) {
    return <p className="text-sm text-stone-500">No active borrowers available for reservations.</p>
  }

  return (
    <form action={formAction} className="space-y-3 rounded-[1.5rem] border border-stone-200 bg-stone-50/80 p-4">
      <input type="hidden" name="bookId" value={bookId} />
      <div>
        <label className="text-xs font-semibold uppercase tracking-[0.18em] text-stone-500">
          Borrower
        </label>
        <select
          name="borrowerId"
          defaultValue={borrowers[0]?.id ?? ""}
          className="mt-2 w-full rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
        >
          {borrowers.map((borrower) => (
            <option key={borrower.id} value={borrower.id}>
              {borrower.fullName} · {borrower.cardNumber}
            </option>
          ))}
        </select>
      </div>
      <div className="flex items-center justify-between gap-3">
        <Button type="submit" variant="secondary" disabled={isPending}>
          {isPending ? "Placing..." : "Place reservation"}
        </Button>
        {state.message ? (
          <p
            aria-live="polite"
            className={state.status === "error" ? "text-xs text-rose-700" : "text-xs text-stone-600"}
          >
            {state.message}
          </p>
        ) : null}
      </div>
    </form>
  )
}
