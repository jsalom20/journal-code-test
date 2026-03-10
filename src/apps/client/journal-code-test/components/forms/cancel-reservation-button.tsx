"use client"

import { useRouter } from "next/navigation"
import { useState, useTransition } from "react"

import { Button } from "@/components/ui/button"
import { getApiBaseUrl } from "@/lib/api"

export function CancelReservationButton({ reservationId }: { reservationId: string }) {
  const router = useRouter()
  const [message, setMessage] = useState<string | null>(null)
  const [isPending, startTransition] = useTransition()

  function handleCancel() {
    startTransition(async () => {
      try {
        const response = await fetch(`${getApiBaseUrl()}/api/reservations/${reservationId}`, {
          method: "DELETE",
        })

        if (!response.ok) {
          const error = (await response.json()) as { message?: string }
          throw new Error(error.message ?? "Cancellation failed.")
        }

        setMessage("Cancelled.")
        router.refresh()
      } catch (error) {
        setMessage(error instanceof Error ? error.message : "Cancellation failed.")
      }
    })
  }

  return (
    <div className="flex items-center gap-2">
      <Button type="button" size="sm" variant="ghost" onClick={handleCancel} disabled={isPending}>
        {isPending ? "Cancelling..." : "Cancel"}
      </Button>
      {message ? <span className="text-xs text-stone-600">{message}</span> : null}
    </div>
  )
}
