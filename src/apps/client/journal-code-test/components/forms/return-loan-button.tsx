"use client"

import { useActionState } from "react"

import { initialMutationState, returnLoan } from "@/app/(library)/actions"
import { Button } from "@/components/ui/button"
import type { CopyConditionStatus } from "@/lib/types"

const returnOptions: CopyConditionStatus[] = ["Excellent", "Good", "Fair", "Damaged", "Lost"]

export function ReturnLoanButton({ loanId }: { loanId: string }) {
  const [state, formAction, isPending] = useActionState(returnLoan, initialMutationState)

  return (
    <form action={formAction} className="flex flex-col gap-2 sm:flex-row sm:items-center">
      <input type="hidden" name="loanId" value={loanId} />
      <select
        name="returnCondition"
        defaultValue="Good"
        className="rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
      >
        {returnOptions.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
      <Button type="submit" size="sm" disabled={isPending}>
        {isPending ? "Returning..." : "Return"}
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
