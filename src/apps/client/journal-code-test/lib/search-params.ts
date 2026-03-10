export function readSingleParam(value: string | string[] | undefined, fallback = "") {
  if (Array.isArray(value)) {
    return value[0] ?? fallback
  }

  return value ?? fallback
}

export function readPositiveInt(value: string | string[] | undefined, fallback: number) {
  const parsed = Number(readSingleParam(value))
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback
}
