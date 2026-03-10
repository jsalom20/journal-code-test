"use client"

import type { BorrowerListItem } from "@/lib/types"

import { CheckoutForm } from "@/components/forms/checkout-form"
import { ReservationForm } from "@/components/forms/reservation-form"

type CheckoutCopyOption = {
  id: string
  inventoryNumber: string
  barcode: string
}

export function BookCirculationPanel({
  bookId,
  borrowers,
  checkoutCopies,
}: {
  bookId: string
  borrowers: BorrowerListItem[]
  checkoutCopies: CheckoutCopyOption[]
}) {
  return (
    <div className="space-y-6">
      <div>
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">
          Checkout
        </p>
        <h3 className="mt-2 text-2xl font-semibold text-stone-950">Move a copy to a borrower</h3>
        <div className="mt-4">
          <CheckoutForm borrowers={borrowers} copies={checkoutCopies} />
        </div>
      </div>

      <div>
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">
          Reservation
        </p>
        <h3 className="mt-2 text-2xl font-semibold text-stone-950">Add a borrower to the queue</h3>
        <div className="mt-4">
          <ReservationForm bookId={bookId} borrowers={borrowers} />
        </div>
      </div>
    </div>
  )
}
