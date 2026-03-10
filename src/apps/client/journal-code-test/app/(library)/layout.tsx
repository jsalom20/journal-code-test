import { LibraryShell } from "@/components/library-shell"

export default function AppLayout({ children }: { children: React.ReactNode }) {
  return <LibraryShell>{children}</LibraryShell>
}
