export function PageHeader({
  eyebrow,
  title,
  description,
}: {
  eyebrow: string
  title: string
  description: string
}) {
  return (
    <div className="mb-5 px-1">
      <p className="text-xs font-semibold uppercase tracking-[0.28em] text-stone-500">{eyebrow}</p>
      <h2 className="mt-3 text-4xl leading-tight font-semibold text-stone-950">{title}</h2>
      <p className="mt-3 max-w-3xl text-sm leading-6 text-stone-600">{description}</p>
    </div>
  )
}
