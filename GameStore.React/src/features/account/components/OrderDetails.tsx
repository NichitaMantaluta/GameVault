import { useEffect, useState } from 'react'
import { navigate } from '../../../app/navigation'
import { getOrder, OrderNotFoundError } from '../../orders/api/ordersApi'
import type { GetOrderResponse } from '../../orders/types/orders'
import { formatMoney, formatOrderDate, formatOrderStatus } from '../format'

type OrderDetailsProps = {
  orderId: string
}

export function OrderDetails({ orderId }: OrderDetailsProps) {
  const [order, setOrder] = useState<GetOrderResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    async function loadOrder() {
      setIsLoading(true)
      setError(null)
      setNotFound(false)
      setOrder(null)

      try {
        const response = await getOrder(orderId, controller.signal)
        setOrder(response)
      } catch (cause) {
        if (isAbortError(cause)) {
          return
        }

        if (cause instanceof OrderNotFoundError) {
          setNotFound(true)
          return
        }

        setError(cause instanceof Error ? cause.message : 'Failed to load order.')
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false)
        }
      }
    }

    void loadOrder()

    return () => controller.abort()
  }, [orderId, reloadKey])

  return (
    <div>
      <button
        type="button"
        className="account-page__back"
        onClick={() => navigate('/account/orders')}
      >
        ← Back to order history
      </button>

      <h2 className="account-page__section-title">Order details</h2>

      {isLoading ? <p className="account-page__status">Loading order…</p> : null}

      {notFound ? (
        <div className="account-page__message" role="alert">
          <p>This order was not found.</p>
          <button
            type="button"
            className="account-page__button account-page__button--primary"
            onClick={() => navigate('/account/orders')}
          >
            View order history
          </button>
        </div>
      ) : null}

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

      {!isLoading && !error && !notFound && order ? (
        <>
          <header className="account-page__detail-header">
            <p className="account-page__order-id">{order.id}</p>
            <p className="account-page__detail-meta">
              <span>{formatOrderDate(order.createdAt)}</span>
              <span>{formatOrderStatus(order.status)}</span>
              <span>{formatMoney(order.totalAmount, order.currency)}</span>
            </p>
          </header>

          {order.items.length === 0 ? (
            <p className="account-page__status">This order has no items.</p>
          ) : (
            <ul className="account-page__items">
              {order.items.map((item) => (
                <li key={item.gameId} className="account-page__item">
                  <p className="account-page__item-name">{item.gameName}</p>
                  <p className="account-page__item-price">
                    {formatMoney(item.price, order.currency)}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </>
      ) : null}
    </div>
  )
}

function isAbortError(cause: unknown): boolean {
  return cause instanceof Error && cause.name === 'AbortError'
}
