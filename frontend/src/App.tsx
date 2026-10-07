import { useState } from 'react'
import { DueTodayAlert } from './alerts/DueTodayAlert'
import { CategoriesPage } from './categories/CategoriesPage'
import { ExpensesPage } from './expenses/ExpensesPage'

type Section = 'expenses' | 'categories'

function App() {
  const [alertKey, setAlertKey] = useState(0)
  const [section, setSection] = useState<Section>('expenses')

  const tab = (value: Section, label: string) => (
    <button
      type="button"
      className={`tab${section === value ? ' tab--active' : ''}`}
      aria-current={section === value ? 'page' : undefined}
      onClick={() => setSection(value)}
    >
      {label}
    </button>
  )

  return (
    <main>
      <header className="app-header">
        <h1>Gestor de gastos</h1>
        <nav className="tabs" aria-label="Secciones">
          {tab('expenses', 'Gastos')}
          {tab('categories', 'Categorías')}
        </nav>
      </header>
      <DueTodayAlert refreshKey={alertKey} />
      {/* Los gastos quedan montados para no perder el mes elegido al cambiar de sección. */}
      <div hidden={section !== 'expenses'}>
        <ExpensesPage onExpensesChanged={() => setAlertKey((key) => key + 1)} />
      </div>
      {section === 'categories' && <CategoriesPage />}
    </main>
  )
}

export default App
