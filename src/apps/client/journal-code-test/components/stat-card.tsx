import { formatCurrency } from "@/lib/format"

export function StatCard({
  label,
  value,
  tone = "default",
  currency = false,
}: {
  label: string
  value: number
  tone?: "default" | "warning"
  currency?: boolean
}) {
  return (
    <div
      className={[
        "rounded-[1.75rem] border p-5",
        tone === "warning"
          ? "border-amber-300/60 bg-amber-50/90"
          : "border-stone-200/80 bg-stone-50/80",
      ].join(" ")}
    >
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-stone-500">{label}</p>
      <p className="mt-4 text-3xl leading-none font-semibold text-stone-950">
        {currency ? formatCurrency(value) : value.toLocaleString("sv-SE")}
      </p>
    </div>
  )
}
