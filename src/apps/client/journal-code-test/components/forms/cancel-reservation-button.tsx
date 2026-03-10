"use client"

import { useActionState } from "react"

import { cancelReservation, initialMutationState } from "@/app/(library)/actions"
import { Button } from "@/components/ui/button"

export function CancelReservationButton({ reservationId }: { reservationId: string }) {
  const [state, formAction, isPending] = useActionState(cancelReservation, initialMutationState)

  return (
    <form action={formAction} className="flex items-center gap-2">
      <input type="hidden" name="reservationId" value={reservationId} />
      <Button type="submit" size="sm" variant="ghost" disabled={isPending}>
        {isPending ? "Cancelling..." : "Cancel"}
      </Button>
      {state.message ? (
        <span
          aria-live="polite"
          className={state.status === "error" ? "text-xs text-rose-700" : "text-xs text-stone-600"}
        >
          {state.message}
        </span>
      ) : null}
    </form>
  )
}
