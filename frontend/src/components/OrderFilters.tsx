import type { Cliente } from '../types/api';

export interface FilterValues {
  clienteId: string;
  dataInicio: string;
  dataFim: string;
}

interface OrderFiltersProps {
  clients: Cliente[];
  values: FilterValues;
  onChange: (values: FilterValues) => void;
  onApply: () => void;
  onClear: () => void;
  disabled?: boolean;
}

export default function OrderFilters({ clients, values, onChange, onApply, onClear, disabled }: OrderFiltersProps) {
  function update(field: keyof FilterValues, value: string) {
    onChange({ ...values, [field]: value });
  }

  return (
    <section className="filters-panel" aria-label="Filtros de pedidos">
      <div className="filter-field filter-client">
        <label htmlFor="filter-client">Cliente</label>
        <select id="filter-client" value={values.clienteId} onChange={(event) => update('clienteId', event.target.value)} disabled={disabled}>
          <option value="">Todos os clientes</option>
          {clients.map((client) => <option key={client.id} value={client.id}>{client.nome}</option>)}
        </select>
      </div>
      <div className="filter-field">
        <label htmlFor="filter-start">Data inicial</label>
        <input id="filter-start" type="date" value={values.dataInicio} max={values.dataFim || undefined} onChange={(event) => update('dataInicio', event.target.value)} disabled={disabled} />
      </div>
      <div className="filter-field">
        <label htmlFor="filter-end">Data final</label>
        <input id="filter-end" type="date" value={values.dataFim} min={values.dataInicio || undefined} onChange={(event) => update('dataFim', event.target.value)} disabled={disabled} />
      </div>
      <div className="filter-actions">
        <button className="button button-secondary" onClick={onClear} disabled={disabled}>Limpar</button>
        <button className="button button-primary" onClick={onApply} disabled={disabled}>Filtrar pedidos</button>
      </div>
    </section>
  );
}
