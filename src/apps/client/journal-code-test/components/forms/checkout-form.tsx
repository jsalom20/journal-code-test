"use client"

import { useRouter } from "next/navigation"
import { useState, useTransition } from "react"

import { Button } from "@/components/ui/button"
import { getApiBaseUrl } from "@/lib/api"
import type { BorrowerListItem, CopyConditionStatus } from "@/lib/types"

const conditionOptions: CopyConditionStatus[] = ["Excellent", "Good", "Fair", "Damaged"]

export function CheckoutForm({
  copyId,
  borrowers,
}: {
  copyId: string
  borrowers: BorrowerListItem[]
}) {
  const router = useRouter()
  const [borrowerId, setBorrowerId] = useState(borrowers[0]?.id ?? "")
  const [checkoutCondition, setCheckoutCondition] = useState<CopyConditionStatus>("Good")
  const [checkoutNotes, setCheckoutNotes] = useState("")
  const [message, setMessage] = useState<string | null>(null)
  const [isPending, startTransition] = useTransition()

  async function handleSubmit(formData: FormData) {
    const payload = {
      copyId,
      borrowerId: String(formData.get("borrowerId") ?? ""),
      checkoutCondition: String(formData.get("checkoutCondition") ?? "Good"),
      checkoutNotes: String(formData.get("checkoutNotes") ?? ""),
    }

    startTransition(async () => {
      try {
        const response = await fetch(`${getApiBaseUrl()}/api/loans/checkout`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload),
        })

        if (!response.ok) {
          const error = (await response.json()) as { message?: string }
          throw new Error(error.message ?? "Checkout failed.")
        }

        setMessage("Copy checked out.")
        router.refresh()
      } catch (error) {
        setMessage(error instanceof Error ? error.message : "Checkout failed.")
      }
    })
  }

  if (borrowers.length === 0) {
    return <p className="text-sm text-stone-500">No active borrowers available for checkout.</p>
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
      <div>
        <label className="text-xs font-semibold uppercase tracking-[0.18em] text-stone-500">
          Condition at checkout
        </label>
        <select
          name="checkoutCondition"
          value={checkoutCondition}
          onChange={(event) => setCheckoutCondition(event.target.value as CopyConditionStatus)}
          className="mt-2 w-full rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
        >
          {conditionOptions.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
      </div>
      <div>
        <label className="text-xs font-semibold uppercase tracking-[0.18em] text-stone-500">
          Notes
        </label>
        <textarea
          name="checkoutNotes"
          value={checkoutNotes}
          onChange={(event) => setCheckoutNotes(event.target.value)}
          className="mt-2 min-h-20 w-full rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
          placeholder="Desk notes, visible damage, patron request..."
        />
      </div>
      <div className="flex items-center justify-between gap-3">
        <Button type="submit" disabled={isPending}>
          {isPending ? "Checking out..." : "Check out copy"}
        </Button>
        {message ? <p className="text-xs text-stone-600">{message}</p> : null}
      </div>
    </form>
  )
}
