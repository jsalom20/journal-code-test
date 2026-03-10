import Link from "next/link"
import { BookCopy, BookMarked, Clock3, LayoutDashboard, Users } from "lucide-react"

const navigation = [
  { href: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { href: "/books", label: "Books", icon: BookMarked },
  { href: "/borrowers", label: "Borrowers", icon: Users },
  { href: "/loans", label: "Loans", icon: Clock3 },
  { href: "/inventory", label: "Inventory", icon: BookCopy },
]

export function LibraryShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-svh bg-[radial-gradient(circle_at_top_left,_rgba(165,120,58,0.2),_transparent_35%),linear-gradient(180deg,_rgba(252,250,245,1),_rgba(246,238,228,1))]">
      <div className="mx-auto flex min-h-svh w-full max-w-7xl flex-col px-4 py-4 lg:flex-row lg:gap-6 lg:px-6">
        <aside className="mb-4 rounded-[2rem] border border-stone-300/60 bg-white/75 p-5 shadow-[0_24px_80px_-40px_rgba(70,42,14,0.45)] backdrop-blur lg:mb-0 lg:w-72">
          <div className="mb-8">
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-stone-500">
              Frenda Library
            </p>
            <h1 className="mt-3 text-3xl leading-none font-semibold text-stone-900">
              Circulation desk
            </h1>
            <p className="mt-3 text-sm leading-6 text-stone-600">
              Search the catalog, move physical copies, and keep the queue visible.
            </p>
          </div>

          <nav className="space-y-2">
            {navigation.map((item) => {
              const Icon = item.icon
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className="flex items-center justify-between rounded-2xl border border-stone-200/70 bg-stone-50/80 px-4 py-3 text-sm font-medium text-stone-700 transition hover:border-stone-300 hover:bg-white hover:text-stone-950"
                >
                  <span>{item.label}</span>
                  <Icon className="size-4" />
                </Link>
              )
            })}
          </nav>

          <div className="mt-8 rounded-[1.75rem] bg-stone-900 px-4 py-5 text-stone-50">
            <p className="text-xs uppercase tracking-[0.24em] text-stone-300">Policy</p>
            <p className="mt-3 text-sm leading-6 text-stone-100/85">
              Loans run 21 days. Overdue fine is 5 SEK per day, capped at 200 SEK.
            </p>
          </div>
        </aside>

        <main className="flex-1">{children}</main>
      </div>
    </div>
  )
}
