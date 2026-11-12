import { http } from '../../api/http';
import type { Cliente, Produto } from '../../types/api';

export function listarClientes(): Promise<Cliente[]> {
  return http.get<Cliente[]>('/clientes');
}

export function listarProdutos(): Promise<Produto[]> {
  return http.get<Produto[]>('/produtos');
}
