import { cn } from "@/lib/utils"

export function SectionCard({
  className,
  children,
}: {
  className?: string
  children: React.ReactNode
}) {
  return (
    <section
      className={cn(
        "rounded-[2rem] border border-stone-300/60 bg-white/80 p-5 shadow-[0_24px_80px_-40px_rgba(70,42,14,0.35)] backdrop-blur sm:p-6",
        className
      )}
    >
      {children}
    </section>
  )
}
