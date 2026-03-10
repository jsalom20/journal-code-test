"use client"

type ErrorPageProps = {
  error: Error & { digest?: string }
  reset: () => void
}

export default function LibraryErrorPage({ error, reset }: ErrorPageProps) {
  return (
    <div className="rounded-[2rem] border border-rose-200 bg-white/90 p-8 shadow-[0_24px_80px_-40px_rgba(70,42,14,0.35)]">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-rose-600">
        Something went wrong
      </p>
      <h2 className="mt-3 text-3xl font-semibold text-stone-950">The library data could not be loaded.</h2>
      <p className="mt-3 max-w-2xl text-sm leading-6 text-stone-600">
        {error.message || "Retry the request. If the problem persists, inspect the upstream API."}
      </p>
      <button
        type="button"
        onClick={reset}
        className="mt-6 rounded-2xl bg-stone-900 px-4 py-3 text-sm font-semibold text-white"
      >
        Try again
      </button>
    </div>
  )
}
