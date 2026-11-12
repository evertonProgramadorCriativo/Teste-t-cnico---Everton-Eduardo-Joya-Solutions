export interface Cliente {
  id: number;
  nome: string;
}

export interface Produto {
  id: number;
  nome: string;
  precoUnitario: number;
  estoqueDisponivel: number;
}

export interface PedidoListItem {
  id: number;
  clienteId: number;
  clienteNome: string;
  dataPedido: string;
  quantidadeItens: number;
  valorTotal: number;
}

export interface PedidoPage {
  items: PedidoListItem[];
  pagina: number;
  tamanhoPagina: number;
  totalRegistros: number;
  totalPaginas: number;
}

export interface PedidoItemInput {
  produtoId: number;
  quantidade: number;
}

export interface CriarPedidoInput {
  clienteId: number;
  itens: PedidoItemInput[];
}

export interface CriarPedidoResult {
  id: number;
  dataPedido: string;
  valorTotal: number;
}

export interface TopCliente {
  clienteId: number;
  clienteNome: string;
  totalComprado: number;
  quantidadePedidos: number;
}

export interface PedidoFilters {
  clienteId?: number;
  dataInicio?: string;
  dataFim?: string;
  pagina: number;
  tamanhoPagina: number;
}

export interface ApiErrorBody {
  title?: string;
  detail?: string;
  message?: string;
  errors?: Record<string, string[]>;
}
