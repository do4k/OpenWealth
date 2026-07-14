import { useState } from 'react'

/**
 * Shared state for editing one row of a list in place: which item is being
 * edited and a draft copy of its fields. Each page renders its own editable
 * row markup (field types differ too much to generalise) but shares this
 * start/update/cancel bookkeeping so edit-in-place is consistent everywhere.
 */
export function useInlineEdit<T extends { id: string }>() {
  const [editingId, setEditingId] = useState<string | null>(null)
  const [draft, setDraft] = useState<T | null>(null)

  function startEdit(item: T) {
    setEditingId(item.id)
    setDraft({ ...item })
  }

  function cancelEdit() {
    setEditingId(null)
    setDraft(null)
  }

  function updateDraft(patch: Partial<T>) {
    setDraft((d) => (d ? { ...d, ...patch } : d))
  }

  return { editingId, draft, isEditing: (id: string) => editingId === id, startEdit, cancelEdit, updateDraft }
}
