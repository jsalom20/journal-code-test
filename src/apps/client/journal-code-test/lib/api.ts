import type {
  BookDetail,
  BorrowerDetail,
  BorrowerListItem,
  CirculationStatus,
  CopyConditionStatus,
  CopyListItem,
  DashboardSummary,
  LoanListItem,
  PagedResult,
  ReservationListItem,
  BookListItem,
} from "@/lib/types"

type ApiErrorPayload = {
  message?: string
}

type ApiInit = RequestInit & {
  query?: Record<string, string | number | undefined | null>
}

export function getApiBaseUrl() {
  return (
    process.env.NEXT_PUBLIC_LIBRARY_API_BASE_URL ??
    process.env.LIBRARY_API_BASE_URL ??
    "http://localhost:5031"
  )
}

export async function apiFetch<T>(path: string, init?: ApiInit): Promise<T> {
  const url = new URL(path, getApiBaseUrl())

  for (const [key, value] of Object.entries(init?.query ?? {})) {
    if (value !== undefined && value !== null && value !== "") {
      url.searchParams.set(key, String(value))
    }
  }

  const response = await fetch(url, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
    cache: "no-store",
  })

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`

    try {
      const payload = (await response.json()) as ApiErrorPayload
      if (payload.message) {
        message = payload.message
      }
    } catch {
      // Fall back to default message.
    }

    throw new Error(message)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export function getDashboardSummary() {
  return apiFetch<DashboardSummary>("/api/dashboard/summary")
}

export function getBooks(params: {
  query?: string
  availability?: string
  page?: number
  pageSize?: number
}) {
  return apiFetch<PagedResult<BookListItem>>("/api/books", { query: params })
}

export function getBook(bookId: string) {
  return apiFetch<BookDetail>(`/api/books/${bookId}`)
}

export function getBorrowers(params: {
  query?: string
  status?: string
}) {
  return apiFetch<BorrowerListItem[]>("/api/borrowers", { query: params })
}

export function getBorrower(borrowerId: string) {
  return apiFetch<BorrowerDetail>(`/api/borrowers/${borrowerId}`)
}

export function getLoans(params: {
  status?: string
  borrowerId?: string
}) {
  return apiFetch<LoanListItem[]>("/api/loans", { query: params })
}

export function getReservations(params: {
  bookId?: string
  borrowerId?: string
  status?: string
}) {
  return apiFetch<ReservationListItem[]>("/api/reservations", { query: params })
}

export function getCopies(params: {
  bookId?: string
  status?: CirculationStatus | string
  condition?: CopyConditionStatus | string
}) {
  return apiFetch<CopyListItem[]>("/api/copies", { query: params })
}
