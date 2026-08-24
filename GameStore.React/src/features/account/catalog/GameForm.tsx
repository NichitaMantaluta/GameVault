import { useState, type FormEvent } from 'react'
import { ApiValidationError } from '../../games/api/gamesApi'
import { genreOptionsForSelect } from './genres'

export type GameFormValues = {
  name: string
  description: string
  price: string
  genreId: string
  imageUrl: string
}

type GameFormProps = {
  title: string
  submitLabel: string
  initialValues: GameFormValues
  currentGenre?: { id: number; name: string } | null
  onSubmit: (values: GameFormValues) => Promise<void>
  onCancel: () => void
}

export function GameForm({
  title,
  submitLabel,
  initialValues,
  currentGenre = null,
  onSubmit,
  onCancel,
}: GameFormProps) {
  const [values, setValues] = useState(initialValues)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const genres = genreOptionsForSelect(currentGenre)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setFormError(null)
    setFieldErrors({})

    try {
      await onSubmit(values)
    } catch (cause) {
      if (cause instanceof ApiValidationError) {
        setFieldErrors(normalizeFieldErrors(cause.fieldErrors))
        setFormError(cause.message)
      } else {
        setFormError(cause instanceof Error ? cause.message : 'Something went wrong.')
      }
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <div className="catalog-admin">
      <h2 className="account-page__section-title">{title}</h2>
      <form className="catalog-form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <label className="catalog-form__field">
          <span>Name</span>
          <input
            value={values.name}
            onChange={(event) => setValues((current) => ({ ...current, name: event.target.value }))}
            maxLength={200}
            required
            autoComplete="off"
          />
          <FieldError errors={fieldErrors} names={['name', 'Name']} />
        </label>

        <label className="catalog-form__field">
          <span>Description</span>
          <textarea
            value={values.description}
            onChange={(event) =>
              setValues((current) => ({ ...current, description: event.target.value }))
            }
            rows={5}
            maxLength={4000}
            required
          />
          <FieldError errors={fieldErrors} names={['description', 'Description']} />
        </label>

        <div className="catalog-form__row">
          <label className="catalog-form__field">
            <span>Price (USD)</span>
            <input
              type="number"
              inputMode="decimal"
              min="0"
              step="0.01"
              value={values.price}
              onChange={(event) =>
                setValues((current) => ({ ...current, price: event.target.value }))
              }
              required
            />
            <FieldError errors={fieldErrors} names={['price', 'Price']} />
          </label>

          <label className="catalog-form__field">
            <span>Genre</span>
            <select
              value={values.genreId}
              onChange={(event) =>
                setValues((current) => ({ ...current, genreId: event.target.value }))
              }
              required
            >
              <option value="" disabled>
                Select a genre
              </option>
              {genres.map((genre) => (
                <option key={genre.id} value={genre.id}>
                  {genre.name}
                </option>
              ))}
            </select>
            <FieldError errors={fieldErrors} names={['genreId', 'GenreId']} />
          </label>
        </div>

        <label className="catalog-form__field">
          <span>Image URL (optional)</span>
          <input
            type="url"
            value={values.imageUrl}
            onChange={(event) =>
              setValues((current) => ({ ...current, imageUrl: event.target.value }))
            }
            maxLength={2048}
            placeholder="https://…"
          />
          <FieldError errors={fieldErrors} names={['imageUrl', 'ImageUrl']} />
        </label>

        {formError ? (
          <p className="catalog-form__error" role="alert">
            {formError}
          </p>
        ) : null}

        <div className="catalog-form__actions">
          <button
            type="submit"
            className="account-page__button account-page__button--primary"
            disabled={isSaving}
          >
            {isSaving ? 'Saving…' : submitLabel}
          </button>
          <button type="button" className="account-page__button" onClick={onCancel} disabled={isSaving}>
            Cancel
          </button>
        </div>
      </form>
    </div>
  )
}

function FieldError({
  errors,
  names,
}: {
  errors: Record<string, string[]>
  names: string[]
}) {
  const messages = names.flatMap((name) => errors[name] ?? [])
  if (messages.length === 0) {
    return null
  }

  return <span className="catalog-form__field-error">{messages[0]}</span>
}

function normalizeFieldErrors(errors: Record<string, string[]>): Record<string, string[]> {
  const normalized: Record<string, string[]> = {}
  for (const [key, value] of Object.entries(errors)) {
    normalized[key] = value
    normalized[key.toLowerCase()] = value
  }
  return normalized
}

export function emptyGameFormValues(): GameFormValues {
  return {
    name: '',
    description: '',
    price: '',
    genreId: '',
    imageUrl: '',
  }
}
