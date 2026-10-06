import { useEffect, useState } from 'react'
import { api } from './api/client'

type Health = { status: string; database: string; today: string }

function App() {
  const [health, setHealth] = useState<Health | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    api
      .get<Health>('/health')
      .then(setHealth)
      .catch(() => setFailed(true))
  }, [])

  return (
    <main>
      <h1>Gestor de gastos</h1>
      {failed && <p role="alert">No se pudo conectar con el servidor.</p>}
      {health && (
        <p>
          Servidor: {health.status} · Base de datos: {health.database} · Hoy: {health.today}
        </p>
      )}
    </main>
  )
}

export default App
