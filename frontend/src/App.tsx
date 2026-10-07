import { useState } from 'react'
import { DueTodayAlert } from './alerts/DueTodayAlert'
import { ExpensesPage } from './expenses/ExpensesPage'

function App() {
  const [alertKey, setAlertKey] = useState(0)

  return (
    <main>
      <header className="app-header">
        <h1>Gestor de gastos</h1>
      </header>
      <DueTodayAlert refreshKey={alertKey} />
      <ExpensesPage onExpensesChanged={() => setAlertKey((key) => key + 1)} />
    </main>
  )
}

export default App
