import Link from "next/link"

import { PageHeader } from "@/components/page-header"
import { SectionCard } from "@/components/section-card"
import { StatusBadge } from "@/components/status-badge"
import { getBooks } from "@/lib/api"
import { readPositiveInt, readSingleParam } from "@/lib/search-params"

export default async function BooksPage({
  searchParams,
}: {
  searchParams: Promise<{ [key: string]: string | string[] | undefined }>
}) {
  const params = await searchParams
  const query = readSingleParam(params.query)
  const availability = readSingleParam(params.availability)
  const page = readPositiveInt(params.page, 1)

  const books = await getBooks({ query, availability, page, pageSize: 16 })

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Catalog"
        title="Search the collection"
        description="Use the title-level catalog to see availability, then drill into the physical copies that can actually move."
      />

      <SectionCard>
        <form className="grid gap-3 md:grid-cols-[1fr_220px_auto]">
          <input
            name="query"
            defaultValue={query}
            placeholder="Title, author, or ISBN"
            className="rounded-2xl border border-stone-300 bg-white px-4 py-3 text-sm"
          />
          <select
            name="availability"
            defaultValue={availability}
            className="rounded-2xl border border-stone-300 bg-white px-4 py-3 text-sm"
          >
            <option value="">All availability</option>
            <option value="available">Has available copy</option>
            <option value="Reserved">Reserved</option>
            <option value="OnLoan">On loan</option>
          </select>
          <button className="rounded-2xl bg-stone-900 px-4 py-3 text-sm font-semibold text-white">
            Search
          </button>
        </form>
      </SectionCard>

      <div className="grid gap-4 xl:grid-cols-2">
        {books.items.map((book) => (
          <Link
            key={book.id}
            href={`/books/${book.id}`}
            className="rounded-[2rem] border border-stone-300/60 bg-white/80 p-5 shadow-[0_24px_80px_-40px_rgba(70,42,14,0.35)] transition hover:-translate-y-0.5 hover:border-stone-400"
          >
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.2em] text-stone-500">{book.isbn13}</p>
                <h3 className="mt-2 text-2xl font-semibold text-stone-950">{book.title}</h3>
                <p className="mt-2 text-sm text-stone-600">{book.authors.join(", ")}</p>
              </div>
              <StatusBadge value={book.availableCopies > 0 ? "Available" : book.reservedCopies > 0 ? "Reserved" : "OnLoan"} />
            </div>
            <div className="mt-5 grid gap-2 sm:grid-cols-4">
              <div className="rounded-2xl bg-stone-100/90 px-3 py-3 text-sm">
                <p className="text-xs uppercase tracking-[0.18em] text-stone-500">Copies</p>
                <p className="mt-2 font-semibold text-stone-950">{book.totalCopies}</p>
              </div>
              <div className="rounded-2xl bg-emerald-50/90 px-3 py-3 text-sm">
                <p className="text-xs uppercase tracking-[0.18em] text-emerald-700">Available</p>
                <p className="mt-2 font-semibold text-stone-950">{book.availableCopies}</p>
              </div>
              <div className="rounded-2xl bg-amber-50/90 px-3 py-3 text-sm">
                <p className="text-xs uppercase tracking-[0.18em] text-amber-700">On loan</p>
                <p className="mt-2 font-semibold text-stone-950">{book.onLoanCopies}</p>
              </div>
              <div className="rounded-2xl bg-fuchsia-50/90 px-3 py-3 text-sm">
                <p className="text-xs uppercase tracking-[0.18em] text-fuchsia-700">Reserved</p>
                <p className="mt-2 font-semibold text-stone-950">{book.reservedCopies}</p>
              </div>
            </div>
          </Link>
        ))}
      </div>

      <SectionCard className="flex items-center justify-between">
        <p className="text-sm text-stone-600">
          Page {books.page} · {books.totalCount} matches
        </p>
        <div className="flex gap-3">
          {books.page > 1 ? (
            <Link
              href={`/books?query=${encodeURIComponent(query)}&availability=${encodeURIComponent(availability)}&page=${books.page - 1}`}
              className="rounded-2xl border border-stone-300 px-4 py-2 text-sm"
            >
              Previous
            </Link>
          ) : null}
          {books.page * books.pageSize < books.totalCount ? (
            <Link
              href={`/books?query=${encodeURIComponent(query)}&availability=${encodeURIComponent(availability)}&page=${books.page + 1}`}
              className="rounded-2xl border border-stone-300 px-4 py-2 text-sm"
            >
              Next
            </Link>
          ) : null}
        </div>
      </SectionCard>
    </div>
  )
}
