import { useCallback, useEffect, useMemo, useState } from 'react';
import Brand from '../components/Brand';
import CreateOrderModal from '../components/CreateOrderModal';
import OrderFilters, { type FilterValues } from '../components/OrderFilters';
import OrdersTable from '../components/OrdersTable';
import Pagination from '../components/Pagination';
import TopClientsPanel from '../components/TopClientsPanel';
import { criarPedido, listarPedidos } from '../features/pedidos/pedidosApi';
import { listarClientes, listarProdutos } from '../features/pedidos/lookupsApi';
import type { Cliente, CriarPedidoInput, PedidoPage, Produto } from '../types/api';
import { formatCurrency } from '../utils/format';

const PAGE_SIZE = 10;
const EMPTY_PAGE: PedidoPage = { items: [], pagina: 1, tamanhoPagina: PAGE_SIZE, totalRegistros: 0, totalPaginas: 0 };
const EMPTY_FILTERS: FilterValues = { clienteId: '', dataInicio: '', dataFim: '' };

export default function OrdersPage() {
  const [pageData, setPageData] = useState<PedidoPage>(EMPTY_PAGE);
  const [clients, setClients] = useState<Cliente[]>([]);
  const [products, setProducts] = useState<Produto[]>([]);
  const [filters, setFilters] = useState<FilterValues>(EMPTY_FILTERS);
  const [appliedFilters, setAppliedFilters] = useState<FilterValues>(EMPTY_FILTERS);
  const [page, setPage] = useState(1);
  const [loadingOrders, setLoadingOrders] = useState(true);
  const [ordersError, setOrdersError] = useState<string | null>(null);
  const [lookupError, setLookupError] = useState<string | null>(null);
  const [globalMessage, setGlobalMessage] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);

  const loadOrders = useCallback(async () => {
    setLoadingOrders(true);
    setOrdersError(null);
    try {
      const result = await listarPedidos({
        clienteId: appliedFilters.clienteId ? Number(appliedFilters.clienteId) : undefined,
        dataInicio: appliedFilters.dataInicio || undefined,
        dataFim: appliedFilters.dataFim || undefined,
        pagina: page,
        tamanhoPagina: PAGE_SIZE,
      });
      setPageData(result);
    } catch (error) {
      setOrdersError(error instanceof Error ? error.message : 'Erro inesperado ao buscar pedidos.');
    } finally {
      setLoadingOrders(false);
    }
  }, [appliedFilters, page, refreshKey]);

  useEffect(() => {
    void loadOrders();
  }, [loadOrders]);

  useEffect(() => {
    let active = true;
    async function loadLookups() {
      const [clientsResult, productsResult] = await Promise.allSettled([listarClientes(), listarProdutos()]);
      if (!active) return;
      const errors: string[] = [];
      if (clientsResult.status === 'fulfilled') setClients(clientsResult.value);
      else errors.push(`clientes: ${clientsResult.reason instanceof Error ? clientsResult.reason.message : 'falha na consulta'}`);
      if (productsResult.status === 'fulfilled') setProducts(productsResult.value);
      else errors.push(`produtos: ${productsResult.reason instanceof Error ? productsResult.reason.message : 'falha na consulta'}`);
      if (errors.length > 0) setLookupError(`Não foi possível carregar ${errors.join(' e ')}. Confira os endpoints GET /clientes e GET /produtos.`);
    }
    void loadLookups();
    return () => { active = false; };
  }, []);

  const totalValueOnPage = useMemo(() => pageData.items.reduce((sum, order) => sum + order.valorTotal, 0), [pageData.items]);
  const totalUnitsOnPage = useMemo(() => pageData.items.reduce((sum, order) => sum + order.quantidadeItens, 0), [pageData.items]);

  function applyFilters() {
    if (filters.dataInicio && filters.dataFim && filters.dataInicio > filters.dataFim) {
      setGlobalMessage('A data inicial não pode ser posterior à data final.');
      return;
    }
    setGlobalMessage(null);
    setPage(1);
    setAppliedFilters({ ...filters });
  }

  function clearFilters() {
    setFilters(EMPTY_FILTERS);
    setAppliedFilters(EMPTY_FILTERS);
    setPage(1);
    setGlobalMessage(null);
  }

  async function handleCreateOrder(input: CriarPedidoInput) {
    await criarPedido(input);
    setShowCreateModal(false);
    setGlobalMessage('Pedido cadastrado com sucesso.');
    setPage(1);
    setAppliedFilters(EMPTY_FILTERS);
    setFilters(EMPTY_FILTERS);
    setRefreshKey((current) => current + 1);
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <Brand />
        <div className="sidebar-divider" />
        <span className="sidebar-label">MENU PRINCIPAL</span>
        <div className="sidebar-link sidebar-link-active"><span className="sidebar-link-icon">▦</span>Pedidos</div>
        <div className="sidebar-note"><span className="status-dot" /> Ambiente de desenvolvimento</div>
        <div className="sidebar-footer"><span className="sidebar-footer-mark">J</span><div><strong>Joya Solutions</strong><small>Mini gestão de pedidos</small></div></div>
      </aside>

      <main className="main-content">
        <header className="topbar"><div className="breadcrumb">Workspace <span>/</span> <strong>Pedidos</strong></div><div className="topbar-right"><span className="topbar-status"><span className="status-dot" /> Sistema de pedidos</span><div className="avatar">ES</div></div></header>

        <div className="page-container">
          <section className="page-heading">
            <div><span className="eyebrow">OPERAÇÃO COMERCIAL</span><h1>Pedidos</h1><p>Acompanhe os pedidos dos seus clientes e gerencie novos registros.</p></div>
            <button className="button button-primary button-create" onClick={() => { setGlobalMessage(null); setShowCreateModal(true); }}><span className="plus-icon">＋</span> Novo pedido</button>
          </section>

          {globalMessage && <div className="alert alert-success" role="status"><span>✓</span>{globalMessage}<button className="alert-close" onClick={() => setGlobalMessage(null)} aria-label="Fechar mensagem">×</button></div>}
          {lookupError && <div className="alert alert-warning" role="alert">{lookupError}</div>}

          <section className="summary-grid" aria-label="Resumo da página atual">
            <article className="summary-card"><div className="summary-card-top"><span>Pedidos nesta página</span><span className="summary-icon icon-blue">▤</span></div><strong>{pageData.items.length}</strong><small>de {pageData.totalRegistros} pedidos encontrados</small></article>
            <article className="summary-card"><div className="summary-card-top"><span>Valor nesta página</span><span className="summary-icon icon-green">R$</span></div><strong>{formatCurrency(totalValueOnPage)}</strong><small>Soma dos pedidos exibidos</small></article>
            <article className="summary-card"><div className="summary-card-top"><span>Itens nesta página</span><span className="summary-icon icon-violet">▦</span></div><strong>{totalUnitsOnPage}</strong><small>Produtos somados nos pedidos exibidos</small></article>
          </section>

          <section className="content-card">
            <div className="content-card-heading"><div><h2>Todos os pedidos</h2><p>Consulte e filtre os registros cadastrados.</p></div><span className="record-count">{pageData.totalRegistros} registros</span></div>
            <OrderFilters clients={clients} values={filters} onChange={setFilters} onApply={applyFilters} onClear={clearFilters} disabled={loadingOrders} />
            <OrdersTable orders={pageData.items} loading={loadingOrders} error={ordersError} onRetry={() => setRefreshKey((current) => current + 1)} />
            {!loadingOrders && !ordersError && <Pagination page={pageData.pagina || page} totalPages={pageData.totalPaginas} totalItems={pageData.totalRegistros} pageSize={pageData.tamanhoPagina || PAGE_SIZE} onPageChange={setPage} />}
          </section>

          <TopClientsPanel />
          <footer className="page-footer">Joya Solutions <span>•</span> Mini Gestão de Pedidos <span>•</span> Desenvolvido com React + TypeScript</footer>
        </div>
      </main>

      {showCreateModal && <CreateOrderModal clients={clients} products={products} onClose={() => setShowCreateModal(false)} onSubmit={handleCreateOrder} />}
    </div>
  );
}
