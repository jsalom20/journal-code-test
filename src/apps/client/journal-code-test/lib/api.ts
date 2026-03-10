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
  title?: string
  detail?: string
}

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number
  ) {
    super(message)
    this.name = "ApiError"
  }
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

export async function apiRequest<T>(path: string, init?: ApiInit): Promise<T> {
  const url = new URL(path, getApiBaseUrl())

  for (const [key, value] of Object.entries(init?.query ?? {})) {
    if (value !== undefined && value !== null && value !== "") {
      url.searchParams.set(key, String(value))
    }
  }

  let response: Response

  try {
    response = await fetch(url, {
      ...init,
      headers: {
        "Content-Type": "application/json",
        ...(init?.headers ?? {}),
      },
      cache: "no-store",
    })
  } catch {
    throw new Error(
      `Library API request failed for ${url.origin}. Ensure the backend is running and the API base URL is correct.`
    )
  }

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`

    try {
      const payload = (await response.json()) as ApiErrorPayload
      if (payload.message || payload.detail || payload.title) {
        message = payload.message ?? payload.detail ?? payload.title ?? message
      }
    } catch {
      // Fall back to default message.
    }

    throw new ApiError(message, response.status)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export function apiFetch<T>(path: string, init?: ApiInit): Promise<T> {
  return apiRequest<T>(path, { cache: "no-store", ...init })
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
