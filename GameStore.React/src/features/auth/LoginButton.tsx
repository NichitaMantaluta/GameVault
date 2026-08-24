import { navigate } from '../../app/navigation'
import { useAuth } from './AuthProvider'
import './LoginButton.css'

export function LoginButton() {
  const { isReady, isAuthenticated, username, login, logout } = useAuth()

  if (isAuthenticated) {
    return (
      <div className="auth-bar">
        {username ? (
          <button
            type="button"
            className="auth-bar__user"
            onClick={() => navigate('/account')}
            aria-label={`Open account for ${username}`}
          >
            {username}
          </button>
        ) : null}
        <button type="button" className="auth-bar__button" onClick={logout}>
          Log out
        </button>
      </div>
    )
  }

  return (
    <button
      type="button"
      className="auth-bar__button"
      onClick={login}
      disabled={!isReady}
    >
      Log in
    </button>
  )
}
