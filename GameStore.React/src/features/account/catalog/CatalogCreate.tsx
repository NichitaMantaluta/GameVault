import { navigate } from '../../../app/navigation'
import { createGame } from '../../games/api/gamesApi'
import { setCatalogFeedback } from './feedback'
import { emptyGameFormValues, GameForm, type GameFormValues } from './GameForm'

export function CatalogCreate() {
  async function handleSubmit(values: GameFormValues) {
    const created = await createGame(toCreatePayload(values))
    setCatalogFeedback(`Created “${created.name}”.`)
    navigate('/account/catalog')
  }

  return (
    <div>
      <button type="button" className="account-page__back" onClick={() => navigate('/account/catalog')}>
        ← Back to Catalog
      </button>
      <GameForm
        title="New game"
        submitLabel="Create game"
        initialValues={emptyGameFormValues()}
        onSubmit={handleSubmit}
        onCancel={() => navigate('/account/catalog')}
      />
    </div>
  )
}

function toCreatePayload(values: GameFormValues) {
  const price = Number(values.price)
  const genreId = Number(values.genreId)
  const imageUrl = values.imageUrl.trim()

  return {
    name: values.name.trim(),
    description: values.description.trim(),
    price: Number.isFinite(price) ? price : 0,
    genreId: Number.isFinite(genreId) ? genreId : 0,
    imageUrl: imageUrl.length > 0 ? imageUrl : null,
  }
}
