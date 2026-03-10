"use client"

import { useActionState } from "react"

import { checkoutCopy, initialMutationState } from "@/app/(library)/actions"
import { Button } from "@/components/ui/button"
import type { BorrowerListItem, CopyConditionStatus } from "@/lib/types"

const conditionOptions: CopyConditionStatus[] = ["Excellent", "Good", "Fair", "Damaged"]

type CheckoutCopyOption = {
  id: string
  inventoryNumber: string
  barcode: string
}

export function CheckoutForm({
  borrowers,
  copies,
}: {
  borrowers: BorrowerListItem[]
  copies: CheckoutCopyOption[]
}) {
  const [state, formAction, isPending] = useActionState(checkoutCopy, initialMutationState)

  if (borrowers.length === 0) {
    return <p className="text-sm text-stone-500">No active borrowers available for checkout.</p>
  }

  if (copies.length === 0) {
    return <p className="text-sm text-stone-500">No copies are currently available for checkout.</p>
  }

  return (
    <form action={formAction} className="space-y-3 rounded-[1.5rem] border border-stone-200 bg-stone-50/80 p-4">
      <div>
        <label className="text-xs font-semibold uppercase tracking-[0.18em] text-stone-500">
          Copy
        </label>
        <select
          name="copyId"
          defaultValue={copies[0]?.id ?? ""}
          className="mt-2 w-full rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
        >
          {copies.map((copy) => (
            <option key={copy.id} value={copy.id}>
              {copy.inventoryNumber} · {copy.barcode}
            </option>
          ))}
        </select>
      </div>
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
      <div>
        <label className="text-xs font-semibold uppercase tracking-[0.18em] text-stone-500">
          Condition at checkout
        </label>
        <select
          name="checkoutCondition"
          defaultValue="Good"
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
          className="mt-2 min-h-20 w-full rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
          placeholder="Desk notes, visible damage, patron request..."
        />
      </div>
      <div className="flex items-center justify-between gap-3">
        <Button type="submit" disabled={isPending}>
          {isPending ? "Checking out..." : "Check out copy"}
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
