import { useAuth } from './AuthProvider'
import './LoginButton.css'

export function LoginButton() {
  const { isReady, isAuthenticated, username, login, logout } = useAuth()

  if (isAuthenticated) {
    return (
      <div className="auth-bar">
        {username ? <p className="auth-bar__user">{username}</p> : null}
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
