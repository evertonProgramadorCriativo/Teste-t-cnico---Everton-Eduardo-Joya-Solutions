import { useState } from 'react';
import type { TopCliente } from '../types/api';
import { obterTopClientes } from '../features/relatorios/relatoriosApi';
import { formatCurrency, localDateString } from '../utils/format';

export default function TopClientsPanel() {
  const now = new Date();
  const [startDate, setStartDate] = useState(`${now.getFullYear()}-01-01`);
  const [endDate, setEndDate] = useState(localDateString(now));
  const [clients, setClients] = useState<TopCliente[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function loadRanking() {
    if (!startDate || !endDate || startDate > endDate) {
      setError('Informe um período válido para consultar o ranking.');
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const result = await obterTopClientes(startDate, endDate);
      setClients(result.slice(0, 5));
      setLoaded(true);
    } catch (error) {
      setError(error instanceof Error ? error.message : 'Não foi possível consultar o ranking.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <section className="ranking-card">
      <div className="ranking-header">
        <div>
          <span className="eyebrow">DESEMPENHO</span>
          <h2>Top 5 clientes</h2>
          <p>Clientes com maior valor total de pedidos no período.</p>
        </div>
        <div className="ranking-symbol">↗</div>
      </div>
      <div className="ranking-filters">
        <div className="filter-field"><label htmlFor="ranking-start">Data inicial</label><input id="ranking-start" type="date" value={startDate} max={endDate} onChange={(event) => setStartDate(event.target.value)} /></div>
        <div className="filter-field"><label htmlFor="ranking-end">Data final</label><input id="ranking-end" type="date" value={endDate} min={startDate} onChange={(event) => setEndDate(event.target.value)} /></div>
        <button className="button button-secondary" onClick={loadRanking} disabled={loading}>{loading ? 'Consultando...' : 'Consultar ranking'}</button>
      </div>
      {error && <div className="alert alert-error" role="alert">{error}</div>}
      {!loaded ? (
        <div className="ranking-placeholder">Escolha o período e consulte os clientes com maior volume de compras.</div>
      ) : clients.length === 0 ? (
        <div className="ranking-placeholder">Não existem pedidos nesse período.</div>
      ) : (
        <div className="ranking-list">
          {clients.map((client, index) => {
            const maxValue = Math.max(...clients.map((item) => item.totalComprado), 1);
            return (
              <div className="ranking-row" key={client.clienteId}>
                <span className={`ranking-position ${index === 0 ? 'ranking-first' : ''}`}>{String(index + 1).padStart(2, '0')}</span>
                <div className="ranking-client-info"><strong>{client.clienteNome}</strong><div className="ranking-bar"><span style={{ width: `${(client.totalComprado / maxValue) * 100}%` }} /></div></div>
                <div className="ranking-amount"><strong>{formatCurrency(client.totalComprado)}</strong><small>{client.quantidadePedidos} pedidos</small></div>
              </div>
            );
          })}
        </div>
      )}
    </section>
  );
}
