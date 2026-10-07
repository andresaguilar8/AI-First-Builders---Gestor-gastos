import { api } from './client'

export type Category = {
  id: number
  name: string
}

export const categoriesApi = {
  list: () => api.get<Category[]>('/categories'),
  create: (name: string) => api.post<Category>('/categories', { name }),
  rename: (id: number, name: string) => api.put<Category>(`/categories/${id}`, { name }),
  remove: (id: number) => api.delete<void>(`/categories/${id}`),
}
