import { http } from '../../api/http';
import type { CriarPedidoInput, CriarPedidoResult, PedidoFilters, PedidoPage } from '../../types/api';

export function listarPedidos(filters: PedidoFilters): Promise<PedidoPage> {
  const params = new URLSearchParams();
  if (filters.clienteId) params.set('clienteId', String(filters.clienteId));
  if (filters.dataInicio) params.set('dataInicio', filters.dataInicio);
  if (filters.dataFim) params.set('dataFim', filters.dataFim);
  params.set('pagina', String(filters.pagina));
  params.set('tamanhoPagina', String(filters.tamanhoPagina));

  return http.get<PedidoPage>(`/pedidos?${params.toString()}`);
}

export function criarPedido(input: CriarPedidoInput): Promise<CriarPedidoResult> {
  return http.post<CriarPedidoInput, CriarPedidoResult>('/pedidos', input);
}
