import { http } from '../../api/http';
import type { TopCliente } from '../../types/api';

export function obterTopClientes(dataInicio: string, dataFim: string): Promise<TopCliente[]> {
  const params = new URLSearchParams({ dataInicio, dataFim });
  return http.get<TopCliente[]>(`/relatorios/top-clientes?${params.toString()}`);
}
