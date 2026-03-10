"use server"

import { refresh } from "next/cache"

import { apiRequest, ApiError } from "@/lib/api"

export type MutationState = {
  status: "idle" | "success" | "error"
  message: string
}

export const initialMutationState: MutationState = {
  status: "idle",
  message: "",
}

function success(message: string): MutationState {
  return { status: "success", message }
}

function failure(message: string): MutationState {
  return { status: "error", message }
}

function readRequiredString(formData: FormData, key: string) {
  const value = String(formData.get(key) ?? "").trim()
  return value
}

export async function checkoutCopy(
  _previousState: MutationState,
  formData: FormData
): Promise<MutationState> {
  const copyId = readRequiredString(formData, "copyId")
  const borrowerId = readRequiredString(formData, "borrowerId")
  const checkoutCondition = readRequiredString(formData, "checkoutCondition")
  const checkoutNotes = String(formData.get("checkoutNotes") ?? "")

  if (!copyId || !borrowerId) {
    return failure("Select both a copy and a borrower.")
  }

  try {
    await apiRequest<void>("/api/loans/checkout", {
      method: "POST",
      body: JSON.stringify({
        copyId,
        borrowerId,
        checkoutCondition,
        checkoutNotes,
      }),
    })

    refresh()
    return success("Copy checked out.")
  } catch (error) {
    if (error instanceof ApiError) {
      return failure(error.message)
    }

    throw error
  }
}

export async function placeReservation(
  _previousState: MutationState,
  formData: FormData
): Promise<MutationState> {
  const bookId = readRequiredString(formData, "bookId")
  const borrowerId = readRequiredString(formData, "borrowerId")

  if (!bookId || !borrowerId) {
    return failure("Select a borrower before placing the reservation.")
  }

  try {
    await apiRequest<void>("/api/reservations", {
      method: "POST",
      body: JSON.stringify({
        bookId,
        borrowerId,
      }),
    })

    refresh()
    return success("Reservation placed.")
  } catch (error) {
    if (error instanceof ApiError) {
      return failure(error.message)
    }

    throw error
  }
}

export async function returnLoan(
  _previousState: MutationState,
  formData: FormData
): Promise<MutationState> {
  const loanId = readRequiredString(formData, "loanId")
  const returnCondition = readRequiredString(formData, "returnCondition")

  if (!loanId) {
    return failure("Missing loan identifier.")
  }

  try {
    await apiRequest<void>(`/api/loans/${loanId}/return`, {
      method: "POST",
      body: JSON.stringify({
        returnCondition,
        returnNotes: "Returned from circulation desk UI.",
      }),
    })

    refresh()
    return success("Returned.")
  } catch (error) {
    if (error instanceof ApiError) {
      return failure(error.message)
    }

    throw error
  }
}

export async function cancelReservation(
  _previousState: MutationState,
  formData: FormData
): Promise<MutationState> {
  const reservationId = readRequiredString(formData, "reservationId")

  if (!reservationId) {
    return failure("Missing reservation identifier.")
  }

  try {
    await apiRequest<void>(`/api/reservations/${reservationId}`, {
      method: "DELETE",
    })

    refresh()
    return success("Cancelled.")
  } catch (error) {
    if (error instanceof ApiError) {
      return failure(error.message)
    }

    throw error
  }
}

export async function updateCopy(
  _previousState: MutationState,
  formData: FormData
): Promise<MutationState> {
  const copyId = readRequiredString(formData, "copyId")
  const bookId = readRequiredString(formData, "bookId")
  const barcode = readRequiredString(formData, "barcode")
  const inventoryNumber = readRequiredString(formData, "inventoryNumber")
  const shelfLocation = readRequiredString(formData, "shelfLocation")
  const conditionStatus = readRequiredString(formData, "conditionStatus")
  const circulationStatus = readRequiredString(formData, "circulationStatus")
  const acquiredAtUtc = readRequiredString(formData, "acquiredAtUtc")
  const notes = String(formData.get("notes") ?? "")

  if (!copyId || !bookId || !barcode || !inventoryNumber || !shelfLocation || !acquiredAtUtc) {
    return failure("Missing required copy details.")
  }

  try {
    await apiRequest<void>(`/api/copies/${copyId}`, {
      method: "PATCH",
      body: JSON.stringify({
        bookId,
        barcode,
        inventoryNumber,
        shelfLocation,
        conditionStatus,
        circulationStatus,
        acquiredAtUtc,
        lastInventoryCheckAtUtc: new Date().toISOString(),
        notes,
      }),
    })

    refresh()
    return success("Saved.")
  } catch (error) {
    if (error instanceof ApiError) {
      return failure(error.message)
    }

    throw error
  }
}
