export type BorrowerStatus = "Active" | "Suspended"
export type CopyConditionStatus = "Excellent" | "Good" | "Fair" | "Damaged" | "Lost"
export type CirculationStatus =
  | "Available"
  | "OnLoan"
  | "Reserved"
  | "Repair"
  | "Lost"
  | "Withdrawn"
export type ReservationStatus = "Active" | "ReadyForPickup" | "Fulfilled" | "Cancelled"
export type CopyEventType =
  | "Created"
  | "StatusChanged"
  | "CheckedOut"
  | "Returned"
  | "ReservationPlaced"
  | "ReservationAssigned"
  | "ReservationCancelled"
  | "InventoryChecked"
  | "ConditionUpdated"

export type PagedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export type DashboardSummary = {
  totalBooks: number
  totalCopies: number
  availableCopies: number
  activeLoans: number
  overdueLoans: number
  activeReservations: number
  suspendedBorrowers: number
  outstandingFinesSek: number
}

export type BookListItem = {
  id: string
  title: string
  isbn13: string
  language: string
  publicationYear: number | null
  publisher: string | null
  authors: string[]
  totalCopies: number
  availableCopies: number
  onLoanCopies: number
  reservedCopies: number
}

export type CopySummary = {
  id: string
  barcode: string
  inventoryNumber: string
  shelfLocation: string
  conditionStatus: CopyConditionStatus
  circulationStatus: CirculationStatus
  acquiredAtUtc: string
  lastInventoryCheckAtUtc: string | null
  notes: string | null
}

export type ReservationQueueItem = {
  id: string
  borrowerId: string
  borrowerName: string
  cardNumber: string
  status: ReservationStatus
  queuedAtUtc: string
  assignedCopyId: string | null
  readyForPickupAtUtc: string | null
}

export type CopyEventItem = {
  id: string
  copyId: string
  eventType: CopyEventType
  description: string
  occurredAtUtc: string
  borrowerName: string | null
}

export type BookDetail = {
  id: string
  title: string
  isbn13: string
  language: string
  publicationYear: number | null
  publisher: string | null
  summary: string | null
  authors: string[]
  copies: CopySummary[]
  reservationQueue: ReservationQueueItem[]
  recentEvents: CopyEventItem[]
}

export type CopyListItem = {
  id: string
  bookId: string
  bookTitle: string
  barcode: string
  inventoryNumber: string
  shelfLocation: string
  conditionStatus: CopyConditionStatus
  circulationStatus: CirculationStatus
  acquiredAtUtc: string
  lastInventoryCheckAtUtc: string | null
  notes: string | null
}

export type BorrowerListItem = {
  id: string
  cardNumber: string
  fullName: string
  email: string
  status: BorrowerStatus
  activeLoansCount: number
  activeReservationsCount: number
  outstandingFineSek: number
}

export type BorrowerLoanItem = {
  loanId: string
  copyId: string
  bookId: string
  bookTitle: string
  barcode: string
  checkedOutAtUtc: string
  dueAtUtc: string
  returnedAtUtc: string | null
  isOverdue: boolean
  fineSek: number
}

export type BorrowerReservationItem = {
  reservationId: string
  bookId: string
  bookTitle: string
  status: ReservationStatus
  queuedAtUtc: string
  assignedCopyId: string | null
  readyForPickupAtUtc: string | null
}

export type BorrowerDetail = {
  id: string
  cardNumber: string
  fullName: string
  email: string
  status: BorrowerStatus
  outstandingFineSek: number
  currentLoans: BorrowerLoanItem[]
  loanHistory: BorrowerLoanItem[]
  reservations: BorrowerReservationItem[]
}

export type LoanListItem = {
  loanId: string
  borrowerId: string
  borrowerName: string
  cardNumber: string
  bookId: string
  bookTitle: string
  copyId: string
  barcode: string
  checkedOutAtUtc: string
  dueAtUtc: string
  returnedAtUtc: string | null
  isOverdue: boolean
  fineSek: number
  checkoutCondition: CopyConditionStatus
  returnCondition: CopyConditionStatus | null
}

export type ReservationListItem = {
  reservationId: string
  bookId: string
  bookTitle: string
  borrowerId: string
  borrowerName: string
  cardNumber: string
  status: ReservationStatus
  queuedAtUtc: string
  assignedCopyId: string | null
  readyForPickupAtUtc: string | null
}
