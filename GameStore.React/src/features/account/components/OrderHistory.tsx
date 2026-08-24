import { useEffect, useState } from 'react'
import { navigate } from '../../../app/navigation'
import { getOrders } from '../../orders/api/ordersApi'
import type { GetOrdersItem } from '../../orders/types/orders'
import { formatMoney, formatOrderDate, formatOrderStatus } from '../format'

export function OrderHistory() {
  const [orders, setOrders] = useState<GetOrdersItem[] | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    async function loadOrders() {
      setIsLoading(true)
      setError(null)

      try {
        const response = await getOrders(controller.signal)
        setOrders(response.items)
      } catch (cause) {
        if (isAbortError(cause)) {
          return
        }

        setOrders(null)
        setError(cause instanceof Error ? cause.message : 'Failed to load orders.')
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false)
        }
      }
    }

    void loadOrders()

    return () => controller.abort()
  }, [reloadKey])

  return (
    <div>
      <h2 className="account-page__section-title">Order History</h2>

      {isLoading && !orders ? <p className="account-page__status">Loading orders…</p> : null}

      {error ? (
        <div className="account-page__message" role="alert">
          <p>{error}</p>
          <button
            type="button"
            className="account-page__button account-page__button--primary"
            onClick={() => setReloadKey((current) => current + 1)}
          >
            Try again
          </button>
        </div>
      ) : null}

      {!error && orders && orders.length === 0 ? (
        <div className="account-page__empty">
          <p className="account-page__status">You have not placed any orders yet.</p>
          <button
            type="button"
            className="account-page__button account-page__button--primary"
            onClick={() => navigate('/')}
          >
            Browse games
          </button>
        </div>
      ) : null}

      {!error && orders && orders.length > 0 ? (
        <ul className="account-page__orders">
          {orders.map((order) => (
            <li key={order.id}>
              <button
                type="button"
                className="account-page__order"
                onClick={() => navigate(`/account/orders/${order.id}`)}
              >
                <div>
                  <p className="account-page__order-id">{order.id}</p>
                  <p className="account-page__order-meta">
                    <span>{formatOrderDate(order.createdAt)}</span>
                    <span>{formatOrderStatus(order.status)}</span>
                  </p>
                </div>
                <p className="account-page__order-total">
                  {formatMoney(order.totalAmount, order.currency)}
                </p>
              </button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  )
}

function isAbortError(cause: unknown): boolean {
  return cause instanceof Error && cause.name === 'AbortError'
}
