import Link from "next/link"

export default function LibraryNotFoundPage() {
  return (
    <div className="rounded-[2rem] border border-stone-300/60 bg-white/90 p-8 shadow-[0_24px_80px_-40px_rgba(70,42,14,0.35)]">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">Not found</p>
      <h2 className="mt-3 text-3xl font-semibold text-stone-950">That record does not exist.</h2>
      <p className="mt-3 max-w-2xl text-sm leading-6 text-stone-600">
        The book or borrower could not be found. Return to an index page and pick another record.
      </p>
      <div className="mt-6 flex gap-3">
        <Link
          href="/books"
          className="rounded-2xl bg-stone-900 px-4 py-3 text-sm font-semibold text-white"
        >
          Browse books
        </Link>
        <Link
          href="/borrowers"
          className="rounded-2xl border border-stone-300 px-4 py-3 text-sm font-semibold text-stone-700"
        >
          Browse borrowers
        </Link>
      </div>
    </div>
  )
}
