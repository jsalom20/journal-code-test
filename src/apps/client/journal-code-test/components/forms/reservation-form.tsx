"use client"

import { useRouter } from "next/navigation"
import { useState, useTransition } from "react"

import { Button } from "@/components/ui/button"
import { getApiBaseUrl } from "@/lib/api"
import type { BorrowerListItem } from "@/lib/types"

export function ReservationForm({
  bookId,
  borrowers,
}: {
  bookId: string
  borrowers: BorrowerListItem[]
}) {
  const router = useRouter()
  const [borrowerId, setBorrowerId] = useState(borrowers[0]?.id ?? "")
  const [message, setMessage] = useState<string | null>(null)
  const [isPending, startTransition] = useTransition()

  async function handleSubmit(formData: FormData) {
    startTransition(async () => {
      try {
        const response = await fetch(`${getApiBaseUrl()}/api/reservations`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            bookId,
            borrowerId: String(formData.get("borrowerId") ?? ""),
          }),
        })

        if (!response.ok) {
          const error = (await response.json()) as { message?: string }
          throw new Error(error.message ?? "Reservation failed.")
        }

        setMessage("Reservation placed.")
        router.refresh()
      } catch (error) {
        setMessage(error instanceof Error ? error.message : "Reservation failed.")
      }
    })
  }

  if (borrowers.length === 0) {
    return <p className="text-sm text-stone-500">No active borrowers available for reservations.</p>
  }

  return (
    <form action={handleSubmit} className="space-y-3 rounded-[1.5rem] border border-stone-200 bg-stone-50/80 p-4">
      <div>
        <label className="text-xs font-semibold uppercase tracking-[0.18em] text-stone-500">
          Borrower
        </label>
        <select
          name="borrowerId"
          value={borrowerId}
          onChange={(event) => setBorrowerId(event.target.value)}
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
        {message ? <p className="text-xs text-stone-600">{message}</p> : null}
      </div>
    </form>
  )
}
