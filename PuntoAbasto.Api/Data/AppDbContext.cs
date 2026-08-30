using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PuntoAbasto.Api.Models;

namespace PuntoAbasto.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ProductoUnidad> ProductoUnidades => Set<ProductoUnidad>();
    public DbSet<Inventario> Inventarios => Set<Inventario>();
    public DbSet<InventarioMovimiento> InventarioMovimientos => Set<InventarioMovimiento>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoItem> PedidoItems => Set<PedidoItem>();
    public DbSet<PedidoEstado> PedidoEstados => Set<PedidoEstado>();
    public DbSet<NotaVenta> NotasVenta => Set<NotaVenta>();
    public DbSet<Auditoria> Auditorias => Set<Auditoria>();
    public DbSet<Config> Configs => Set<Config>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        // ── USUARIOS ─────────────────────────────────────────────
        // Id = auth.users.id (Supabase): lo asigna el trigger de Postgres,
        // nunca EF, así que no tiene default y no es value-generated acá.
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            ConfigurarUpdatedAtManejadoPorTrigger(entity.Property(e => e.UpdatedAt));
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // ── CLIENTES ─────────────────────────────────────────────
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("clientes");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            ConfigurarUpdatedAtManejadoPorTrigger(entity.Property(e => e.UpdatedAt));
            entity.HasIndex(e => e.Telefono).IsUnique();
        });

        // ── CATEGORIAS ───────────────────────────────────────────
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categorias");
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // ── PRODUCTOS ────────────────────────────────────────────
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("productos");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            ConfigurarUpdatedAtManejadoPorTrigger(entity.Property(e => e.UpdatedAt));

            entity.HasOne(e => e.Categoria)
                  .WithMany(c => c.Productos)
                  .HasForeignKey(e => e.CategoriaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PRODUCTO_UNIDADES ────────────────────────────────────
        modelBuilder.Entity<ProductoUnidad>(entity =>
        {
            entity.ToTable("producto_unidades");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();

            entity.HasOne(e => e.Producto)
                  .WithMany(p => p.Unidades)
                  .HasForeignKey(e => e.ProductoId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Una sola unidad "default" por producto (índice parcial, ver schema.sql).
            entity.HasIndex(e => e.ProductoId)
                  .IsUnique()
                  .HasFilter("es_default = true")
                  .HasDatabaseName("ux_producto_unidades_default");
        });

        // ── INVENTARIO ───────────────────────────────────────────
        modelBuilder.Entity<Inventario>(entity =>
        {
            entity.ToTable("inventario");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();

            // alerta_activa y updated_at los recalcula el trigger
            // fn_inventario_check_alerta en cada INSERT/UPDATE de
            // stock_actual o stock_minimo: EF nunca debe escribirlos,
            // solo releerlos después de guardar.
            ConfigurarUpdatedAtManejadoPorTrigger(entity.Property(e => e.UpdatedAt));
            entity.Property(e => e.AlertaActiva)
                  .ValueGeneratedOnAddOrUpdate()
                  .Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            entity.HasIndex(e => e.ProductoUnidadId).IsUnique();

            entity.HasOne(e => e.ProductoUnidad)
                  .WithOne(pu => pu.Inventario)
                  .HasForeignKey<Inventario>(e => e.ProductoUnidadId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── INVENTARIO_MOVIMIENTOS ───────────────────────────────
        modelBuilder.Entity<InventarioMovimiento>(entity =>
        {
            entity.ToTable("inventario_movimientos");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(e => e.Inventario)
                  .WithMany(i => i.Movimientos)
                  .HasForeignKey(e => e.InventarioId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Pedido)
                  .WithMany(p => p.InventarioMovimientos)
                  .HasForeignKey(e => e.PedidoId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Usuario)
                  .WithMany(u => u.InventarioMovimientos)
                  .HasForeignKey(e => e.UsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── PEDIDOS ──────────────────────────────────────────────
        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.ToTable("pedidos");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(e => e.FechaPedido).HasDefaultValueSql("now()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Estado).HasDefaultValue("recibido");
            entity.Property(e => e.Origen).HasDefaultValue("whatsapp");

            // El número correlativo (PED-0001) lo asigna un trigger BEFORE INSERT
            // en Postgres (fn_pedidos_set_numero), nunca el cliente EF: lo marcamos
            // ValueGeneratedOnAdd + BeforeSaveBehavior.Ignore para que EF no lo
            // mande en el INSERT y sí lo relea vía RETURNING después de guardarlo.
            entity.Property(e => e.Numero)
                  .ValueGeneratedOnAdd()
                  .Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            entity.HasIndex(e => e.Numero).IsUnique();

            entity.HasOne(e => e.Cliente)
                  .WithMany(c => c.Pedidos)
                  .HasForeignKey(e => e.ClienteId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Usuario)
                  .WithMany(u => u.Pedidos)
                  .HasForeignKey(e => e.UsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── PEDIDO_ITEMS ─────────────────────────────────────────
        modelBuilder.Entity<PedidoItem>(entity =>
        {
            entity.ToTable("pedido_items");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();

            entity.HasOne(e => e.Pedido)
                  .WithMany(p => p.Items)
                  .HasForeignKey(e => e.PedidoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ProductoUnidad)
                  .WithMany(pu => pu.PedidoItems)
                  .HasForeignKey(e => e.ProductoUnidadId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── PEDIDO_ESTADOS ───────────────────────────────────────
        modelBuilder.Entity<PedidoEstado>(entity =>
        {
            entity.ToTable("pedido_estados");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(e => e.Pedido)
                  .WithMany(p => p.HistorialEstados)
                  .HasForeignKey(e => e.PedidoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Usuario)
                  .WithMany(u => u.PedidoEstados)
                  .HasForeignKey(e => e.UsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── NOTAS_VENTA ──────────────────────────────────────────
        modelBuilder.Entity<NotaVenta>(entity =>
        {
            entity.ToTable("notas_venta");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(e => e.FechaEmision).HasDefaultValueSql("now()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Estado).HasDefaultValue("emitida");

            // Mismo motivo que en PEDIDOS.numero: lo asigna un trigger BEFORE INSERT.
            entity.Property(e => e.Numero)
                  .ValueGeneratedOnAdd()
                  .Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

            entity.HasIndex(e => e.Numero).IsUnique();
            entity.HasIndex(e => e.PedidoId).IsUnique();

            entity.HasOne(e => e.Pedido)
                  .WithOne(p => p.NotaVenta)
                  .HasForeignKey<NotaVenta>(e => e.PedidoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Usuario)
                  .WithMany(u => u.NotasVenta)
                  .HasForeignKey(e => e.UsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── AUDITORIA ────────────────────────────────────────────
        modelBuilder.Entity<Auditoria>(entity =>
        {
            entity.ToTable("auditoria");
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()").ValueGeneratedOnAdd();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.DatosAnteriores).HasColumnType("jsonb");
            entity.Property(e => e.DatosNuevos).HasColumnType("jsonb");

            entity.HasOne(e => e.Usuario)
                  .WithMany(u => u.Auditorias)
                  .HasForeignKey(e => e.UsuarioId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── CONFIG ───────────────────────────────────────────────
        modelBuilder.Entity<Config>(entity =>
        {
            entity.ToTable("config");
            ConfigurarUpdatedAtManejadoPorTrigger(entity.Property(e => e.UpdatedAt));
            entity.HasIndex(e => e.Clave).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// updated_at en usuarios/clientes/productos/config lo pisa un trigger
    /// BEFORE UPDATE (fn_set_updated_at) en cada UPDATE, sin importar qué
    /// mande el cliente. Si no marcamos la propiedad así, EF sigue creyendo
    /// que el valor que tenía en memoria (desactualizado) es el correcto
    /// después de SaveChanges, en vez de releer el now() real que puso Postgres.
    /// </summary>
    private static void ConfigurarUpdatedAtManejadoPorTrigger(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<DateTimeOffset> property)
    {
        property
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate()
            .Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
    }
}
