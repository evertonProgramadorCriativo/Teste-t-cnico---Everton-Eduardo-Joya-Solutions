using Joya.Pedidos.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Joya.Pedidos.Api.Data;

public sealed class PedidosDbContext(DbContextOptions<PedidosDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(cliente => cliente.Id);
            entity.Property(cliente => cliente.Nome).HasMaxLength(150).IsRequired();
            entity.Property(cliente => cliente.Email).HasMaxLength(200).IsRequired();
            entity.HasIndex(cliente => cliente.Email).IsUnique();
        });

        modelBuilder.Entity<Produto>(entity =>
        {
            entity.ToTable("Produtos");
            entity.HasKey(produto => produto.Id);
            entity.Property(produto => produto.Nome).HasMaxLength(150).IsRequired();
            entity.Property(produto => produto.PrecoUnitario).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.ToTable("Pedidos");
            entity.HasKey(pedido => pedido.Id);
            entity.Property(pedido => pedido.DataPedido).HasColumnType("datetime2");
            entity.Property(pedido => pedido.ValorTotal).HasColumnType("decimal(18,2)");
            entity.HasOne(pedido => pedido.Cliente)
                .WithMany(cliente => cliente.Pedidos)
                .HasForeignKey(pedido => pedido.ClienteId);
            entity.HasIndex(pedido => pedido.DataPedido)
                .HasDatabaseName("IX_Pedidos_DataPedido")
                .IncludeProperties(pedido => new { pedido.ClienteId, pedido.ValorTotal });
            entity.HasIndex(pedido => new { pedido.ClienteId, pedido.DataPedido })
                .HasDatabaseName("IX_Pedidos_ClienteId_DataPedido");
        });

        modelBuilder.Entity<ItemPedido>(entity =>
        {
            entity.ToTable("ItensPedido");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.PrecoUnitario).HasColumnType("decimal(18,2)");
            entity.HasOne(item => item.Pedido)
                .WithMany(pedido => pedido.ItensPedido)
                .HasForeignKey(item => item.PedidoId);
            entity.HasOne(item => item.Produto)
                .WithMany(produto => produto.ItensPedido)
                .HasForeignKey(item => item.ProdutoId);
            entity.HasIndex(item => item.PedidoId).HasDatabaseName("IX_ItensPedido_PedidoId");
            entity.HasIndex(item => item.ProdutoId).HasDatabaseName("IX_ItensPedido_ProdutoId");
        });
    }
}
