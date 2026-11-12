import { useMemo, useState, type FormEvent } from 'react';
import type { Cliente, CriarPedidoInput, Produto } from '../types/api';
import { formatCurrency } from '../utils/format';

interface DraftItem {
  rowId: number;
  produtoId: string;
  quantidade: number;
}

interface CreateOrderModalProps {
  clients: Cliente[];
  products: Produto[];
  onClose: () => void;
  onSubmit: (input: CriarPedidoInput) => Promise<void>;
}

export default function CreateOrderModal({ clients, products, onClose, onSubmit }: CreateOrderModalProps) {
  const [clienteId, setClienteId] = useState('');
  const [items, setItems] = useState<DraftItem[]>([{ rowId: 1, produtoId: '', quantidade: 1 }]);
  const [nextRowId, setNextRowId] = useState(2);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const selectedProducts = useMemo(
    () => items.map((item) => products.find((product) => product.id === Number(item.produtoId))).filter((product): product is Produto => Boolean(product)),
    [items, products],
  );

  const total = items.reduce((sum, item) => {
    const product = products.find((candidate) => candidate.id === Number(item.produtoId));
    return sum + (product ? product.precoUnitario * item.quantidade : 0);
  }, 0);

  function updateItem(rowId: number, field: 'produtoId' | 'quantidade', value: string | number) {
    setItems((current) => current.map((item) => item.rowId === rowId ? { ...item, [field]: value } : item));
    setError(null);
  }

  function addItem() {
    setItems((current) => [...current, { rowId: nextRowId, produtoId: '', quantidade: 1 }]);
    setNextRowId((current) => current + 1);
  }

  function removeItem(rowId: number) {
    setItems((current) => current.filter((item) => item.rowId !== rowId));
    setError(null);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    if (!clienteId) {
      setError('Selecione um cliente para continuar.');
      return;
    }
    if (items.length === 0 || items.some((item) => !item.produtoId)) {
      setError('Adicione pelo menos um produto e preencha todos os itens.');
      return;
    }
    if (items.some((item) => !Number.isInteger(item.quantidade) || item.quantidade <= 0)) {
      setError('A quantidade de cada produto deve ser um número inteiro maior que zero.');
      return;
    }
    const productIds = items.map((item) => Number(item.produtoId));
    if (new Set(productIds).size !== productIds.length) {
      setError('Um produto foi selecionado mais de uma vez. Ajuste a quantidade em uma única linha.');
      return;
    }
    const stockIssue = items.find((item) => {
      const product = products.find((candidate) => candidate.id === Number(item.produtoId));
      return !product || item.quantidade > product.estoqueDisponivel;
    });
    if (stockIssue) {
      const product = products.find((candidate) => candidate.id === Number(stockIssue.produtoId));
      setError(product ? `Estoque insuficiente para ${product.nome}. Disponível: ${product.estoqueDisponivel}.` : 'Um dos produtos selecionados não está disponível.');
      return;
    }

    const payload: CriarPedidoInput = {
      clienteId: Number(clienteId),
      itens: items.map((item) => ({ produtoId: Number(item.produtoId), quantidade: item.quantidade })),
    };

    setSaving(true);
    try {
      await onSubmit(payload);
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : 'Não foi possível salvar o pedido.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="modal-backdrop" role="presentation" onMouseDown={(event) => event.target === event.currentTarget && !saving && onClose()}>
      <section className="modal-card" role="dialog" aria-modal="true" aria-labelledby="create-order-title">
        <header className="modal-header">
          <div>
            <span className="eyebrow">NOVO REGISTRO</span>
            <h2 id="create-order-title">Criar pedido</h2>
            <p>Selecione o cliente e adicione os produtos do pedido.</p>
          </div>
          <button className="icon-button" aria-label="Fechar" onClick={onClose} disabled={saving}>×</button>
        </header>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            <div className="form-section">
              <div className="section-heading"><span className="step-number">1</span><div><strong>Dados do cliente</strong><small>Quem está realizando o pedido?</small></div></div>
              <div className="filter-field">
                <label htmlFor="order-client">Cliente <span className="required-mark">*</span></label>
                <select id="order-client" value={clienteId} onChange={(event) => setClienteId(event.target.value)} required>
                  <option value="">Selecione um cliente</option>
                  {clients.map((client) => <option key={client.id} value={client.id}>{client.nome}</option>)}
                </select>
              </div>
            </div>

            <div className="form-section">
              <div className="section-heading"><span className="step-number">2</span><div><strong>Produtos do pedido</strong><small>Inclua um ou mais produtos e suas quantidades.</small></div></div>
              <div className="order-items-list">
                {items.map((item, index) => {
                  const product = products.find((candidate) => candidate.id === Number(item.produtoId));
                  const alreadySelected = items.filter((candidate) => candidate.rowId !== item.rowId).map((candidate) => Number(candidate.produtoId));
                  return (
                    <div className="order-item-card" key={item.rowId}>
                      <div className="order-item-heading"><strong>Produto {index + 1}</strong>{items.length > 1 && <button type="button" className="text-button danger-text" onClick={() => removeItem(item.rowId)}>Remover</button>}</div>
                      <div className="item-fields">
                        <div className="filter-field product-field">
                          <label htmlFor={`product-${item.rowId}`}>Produto</label>
                          <select id={`product-${item.rowId}`} value={item.produtoId} onChange={(event) => updateItem(item.rowId, 'produtoId', event.target.value)} required>
                            <option value="">Selecione um produto</option>
                            {products.map((option) => (
                              <option key={option.id} value={option.id} disabled={alreadySelected.includes(option.id) || option.estoqueDisponivel <= 0}>
                                {option.nome}{option.estoqueDisponivel <= 0 ? ' — sem estoque' : ''}
                              </option>
                            ))}
                          </select>
                        </div>
                        <div className="filter-field quantity-field">
                          <label htmlFor={`quantity-${item.rowId}`}>Quantidade</label>
                          <input id={`quantity-${item.rowId}`} type="number" min="1" step="1" max={product?.estoqueDisponivel || undefined} value={item.quantidade} onChange={(event) => updateItem(item.rowId, 'quantidade', Number(event.target.value))} required />
                        </div>
                      </div>
                      <div className="product-meta">
                        <span>{product ? `Preço unitário: ${formatCurrency(product.precoUnitario)}` : 'Selecione um produto para ver o preço'}</span>
                        <span>{product ? `Estoque: ${product.estoqueDisponivel}` : ''}</span>
                        {product && <strong>{formatCurrency(product.precoUnitario * item.quantidade)}</strong>}
                      </div>
                    </div>
                  );
                })}
              </div>
              <button type="button" className="button button-add-item" onClick={addItem} disabled={products.length === 0 || items.length >= products.length}>＋ Adicionar outro produto</button>
              {products.length === 0 && <p className="field-hint">Nenhum produto disponível para seleção.</p>}
            </div>

            <div className="order-total-box">
              <div><span>Resumo do pedido</span><small>{selectedProducts.length} {selectedProducts.length === 1 ? 'produto selecionado' : 'produtos selecionados'}</small></div>
              <div className="order-total-value">{formatCurrency(total)}</div>
            </div>
            <p className="field-hint">O valor exibido é uma estimativa. O backend deve recalcular os preços e validar o estoque ao salvar.</p>
            {error && <div className="alert alert-error" role="alert">{error}</div>}
          </div>

          <footer className="modal-footer">
            <button type="button" className="button button-secondary" onClick={onClose} disabled={saving}>Cancelar</button>
            <button type="submit" className="button button-primary" disabled={saving || clients.length === 0 || products.length === 0}>{saving ? <><span className="button-spinner" /> Salvando...</> : 'Salvar pedido'}</button>
          </footer>
        </form>
      </section>
    </div>
  );
}
