"use client"

import { useRouter } from "next/navigation"
import { useState, useTransition } from "react"

import { Button } from "@/components/ui/button"
import { getApiBaseUrl } from "@/lib/api"
import type { CopyConditionStatus } from "@/lib/types"

const returnOptions: CopyConditionStatus[] = ["Excellent", "Good", "Fair", "Damaged", "Lost"]

export function ReturnLoanButton({ loanId }: { loanId: string }) {
  const router = useRouter()
  const [returnCondition, setReturnCondition] = useState<CopyConditionStatus>("Good")
  const [message, setMessage] = useState<string | null>(null)
  const [isPending, startTransition] = useTransition()

  function handleClick() {
    startTransition(async () => {
      try {
        const response = await fetch(`${getApiBaseUrl()}/api/loans/${loanId}/return`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            returnCondition,
            returnNotes: "Returned from circulation desk UI.",
          }),
        })

        if (!response.ok) {
          const error = (await response.json()) as { message?: string }
          throw new Error(error.message ?? "Return failed.")
        }

        setMessage("Returned.")
        router.refresh()
      } catch (error) {
        setMessage(error instanceof Error ? error.message : "Return failed.")
      }
    })
  }

  return (
    <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
      <select
        value={returnCondition}
        onChange={(event) => setReturnCondition(event.target.value as CopyConditionStatus)}
        className="rounded-2xl border border-stone-300 bg-white px-3 py-2 text-sm"
      >
        {returnOptions.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
      <Button type="button" size="sm" onClick={handleClick} disabled={isPending}>
        {isPending ? "Returning..." : "Return"}
      </Button>
      {message ? <span className="text-xs text-stone-600">{message}</span> : null}
    </div>
  )
}
