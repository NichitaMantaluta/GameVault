import type { ChangeEvent } from 'react'
import './SearchInput.css'

type SearchInputProps = {
  value: string
  onChange: (value: string) => void
}

export function SearchInput({ value, onChange }: SearchInputProps) {
  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    onChange(event.target.value)
  }

  return (
    <label className="search-input">
      <span className="search-input__label">Search games</span>
      <input
        className="search-input__field"
        type="search"
        name="search"
        placeholder="Search by game name"
        value={value}
        onChange={handleChange}
        autoComplete="off"
      />
    </label>
  )
}
