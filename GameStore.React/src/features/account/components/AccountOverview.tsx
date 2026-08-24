import { useAuth } from '../../auth/AuthProvider'

export function AccountOverview() {
  const { profile } = useAuth()
  const fields = buildFields(profile)

  return (
    <div>
      <h2 className="account-page__section-title">Account Overview</h2>
      {fields.length === 0 ? (
        <p className="account-page__status">No account details are available from your sign-in.</p>
      ) : (
        <dl className="account-page__fields">
          {fields.map((field) => (
            <div key={field.label}>
              <dt>{field.label}</dt>
              <dd>{field.value}</dd>
            </div>
          ))}
        </dl>
      )}
    </div>
  )
}

function buildFields(profile: {
  username: string | null
  name: string | null
  givenName: string | null
  familyName: string | null
  email: string | null
  subject: string | null
}): Array<{ label: string; value: string }> {
  const fields: Array<{ label: string; value: string }> = []

  if (profile.username) {
    fields.push({ label: 'Username', value: profile.username })
  }

  if (profile.name) {
    fields.push({ label: 'Name', value: profile.name })
  } else {
    const parts = [profile.givenName, profile.familyName].filter(Boolean)
    if (parts.length > 0) {
      fields.push({ label: 'Name', value: parts.join(' ') })
    }
  }

  if (profile.email) {
    fields.push({ label: 'Email', value: profile.email })
  }

  if (profile.subject) {
    fields.push({ label: 'Account ID', value: profile.subject })
  }

  return fields
}
