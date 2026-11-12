import type { PedidoListItem } from '../types/api';
import { formatCurrency, formatDate } from '../utils/format';

interface OrdersTableProps {
  orders: PedidoListItem[];
  loading: boolean;
  error: string | null;
  onRetry: () => void;
}

export default function OrdersTable({ orders, loading, error, onRetry }: OrdersTableProps) {
  if (loading) {
    return (
      <div className="table-state" role="status">
        <span className="spinner" />
        <strong>Carregando pedidos...</strong>
        <span>Estamos consultando os dados no servidor.</span>
      </div>
    );
  }

  if (error) {
    return (
      <div className="table-state" role="alert">
        <div className="state-icon state-icon-error">!</div>
        <strong>Não foi possível carregar os pedidos</strong>
        <span>{error}</span>
        <button className="button button-secondary button-small" onClick={onRetry}>Tentar novamente</button>
      </div>
    );
  }

  if (orders.length === 0) {
    return (
      <div className="table-state">
        <div className="state-icon">⌕</div>
        <strong>Nenhum pedido encontrado</strong>
        <span>Altere os filtros ou cadastre o primeiro pedido.</span>
      </div>
    );
  }

  return (
    <div className="table-wrap">
      <table className="orders-table">
        <thead>
          <tr>
            <th>Pedido</th>
            <th>Cliente</th>
            <th>Data do pedido</th>
            <th>Itens</th>
            <th className="align-right">Valor total</th>
          </tr>
        </thead>
        <tbody>
          {orders.map((order) => (
            <tr key={order.id}>
              <td><span className="order-number">#{String(order.id).padStart(4, '0')}</span></td>
              <td><span className="customer-name">{order.clienteNome}</span></td>
              <td className="muted-cell">{formatDate(order.dataPedido)}</td>
              <td><span className="items-pill">{order.quantidadeItens} {order.quantidadeItens === 1 ? 'item' : 'itens'}</span></td>
              <td className="align-right amount-cell">{formatCurrency(order.valorTotal)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
