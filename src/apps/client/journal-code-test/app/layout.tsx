import type { Metadata } from "next"

import "./globals.css"
import { ThemeProvider } from "@/components/theme-provider"
import { cn } from "@/lib/utils"

export const metadata: Metadata = {
  title: "Frenda Library",
  description: "Library circulation and inventory app built for the Frenda code test.",
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html
      lang="en"
      suppressHydrationWarning
      className={cn("antialiased", "font-serif")}
    >
      <body className="min-h-svh text-stone-950">
        <ThemeProvider>{children}</ThemeProvider>
      </body>
    </html>
  )
}
