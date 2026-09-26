using Microsoft.EntityFrameworkCore;
using GestorCredito.Api.Models;

namespace GestorCredito.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();
    public DbSet<TipoPersona> TiposPersona => Set<TipoPersona>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Requisito> Requisitos => Set<Requisito>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Prospecto> Prospectos => Set<Prospecto>();
    public DbSet<Simulacion> Simulaciones => Set<Simulacion>();
    public DbSet<Cuota> Cuotas => Set<Cuota>();
    public DbSet<DocumentoAdjunto> DocumentosAdjuntos => Set<DocumentoAdjunto>();
    public DbSet<DocumentoGenerado> DocumentosGenerados => Set<DocumentoGenerado>();
    public DbSet<CuentaBancariaInterna> CuentasBancariasInternas => Set<CuentaBancariaInterna>();
    public DbSet<DeclaracionInversion> DeclaracionesInversion => Set<DeclaracionInversion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TipoDocumento / TipoPersona
        modelBuilder.Entity<TipoDocumento>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
            e.HasMany(x => x.TiposPersona)
                .WithMany(x => x.TiposDocumento)
                .UsingEntity(j => j.ToTable("TipoDocumentoTipoPersona"));
        });

        modelBuilder.Entity<TipoPersona>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        // Producto
        modelBuilder.Entity<Producto>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.MontoMin).HasColumnType("decimal(18,2)");
            e.Property(x => x.MontoMax).HasColumnType("decimal(18,2)");
            e.Property(x => x.TasaMin).HasColumnType("decimal(5,2)");
            e.Property(x => x.TasaMax).HasColumnType("decimal(5,2)");
            e.Property(x => x.CargoOtrosPorCuota).HasColumnType("decimal(18,2)");
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Producto_MontoMin", "[MontoMin] > 0"));
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Producto_MontoMax", "[MontoMax] >= [MontoMin]"));
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Producto_TasaRango", "[TasaMax] >= [TasaMin] AND [TasaMin] >= 0"));
            e.ToTable(t => t.HasCheckConstraint(
                "CK_Producto_PlazoRango", "[PlazoMax] >= [PlazoMin] AND [PlazoMin] >= 1"));
        });

        // Requisito
        modelBuilder.Entity<Requisito>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.HasOne(x => x.Producto)
                .WithMany(x => x.Requisitos)
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Usuario
        modelBuilder.Entity<Usuario>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        });

        // Prospecto
        modelBuilder.Entity<Prospecto>(e =>
        {
            e.Property(x => x.NumeroDocumento).HasMaxLength(20).IsRequired();
            e.Property(x => x.Nombres).HasMaxLength(150);
            e.Property(x => x.Apellidos).HasMaxLength(150);
            e.Property(x => x.Direccion).HasMaxLength(250);
            e.Property(x => x.CuentaExternaBanco).HasMaxLength(150);
            e.Property(x => x.CuentaExternaCCI).HasMaxLength(20);
            e.Property(x => x.ComentarioAprobador).HasMaxLength(1000);

            e.HasOne(x => x.TipoDocumento)
                .WithMany()
                .HasForeignKey(x => x.TipoDocumentoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CuentaBancariaInterna)
                .WithMany()
                .HasForeignKey(x => x.CuentaBancariaInternaId)
                .OnDelete(DeleteBehavior.Restrict);

            // FR-010: un único Prospecto "activo" (Estado distinto de Rechazado/Finalizado)
            // por Número de Documento, aplicado con un índice único filtrado. SQL Server no
            // admite NOT/<> en el predicado de un índice filtrado, así que se listan
            // explícitamente los estados "activos" (todos salvo Rechazado=4 y Finalizado=6).
            e.HasIndex(x => x.NumeroDocumento)
                .IsUnique()
                .HasFilter("[Estado] IN (0, 1, 2, 3, 5)")
                .HasDatabaseName("IX_Prospecto_NumeroDocumento_Activo");
        });

        // CuentaBancariaInterna (002-seguimiento-avanzado-prospectos)
        modelBuilder.Entity<CuentaBancariaInterna>(e =>
        {
            e.Property(x => x.NumeroDocumento).HasMaxLength(20).IsRequired();
            e.Property(x => x.NumeroCuenta).HasMaxLength(30).IsRequired();

            // data-model.md: no puede existir más de una CuentaBancariaInterna con el mismo
            // (TipoDocumentoId, NumeroDocumento, NumeroCuenta).
            e.HasIndex(x => new { x.TipoDocumentoId, x.NumeroDocumento, x.NumeroCuenta })
                .IsUnique()
                .HasDatabaseName("IX_CuentaBancariaInterna_Cliente_NumeroCuenta");
        });

        // DeclaracionInversion (002-seguimiento-avanzado-prospectos)
        modelBuilder.Entity<DeclaracionInversion>(e =>
        {
            e.Property(x => x.MontoInvertir).HasColumnType("decimal(18,2)");
            e.Property(x => x.Detalle).HasMaxLength(500);

            e.HasOne(x => x.Prospecto)
                .WithOne(x => x.DeclaracionInversion)
                .HasForeignKey<DeclaracionInversion>(x => x.ProspectoId)
                .OnDelete(DeleteBehavior.Cascade);

            // A lo sumo una Declaración de Inversión por Prospecto.
            e.HasIndex(x => x.ProspectoId).IsUnique();
        });

        // Simulacion
        modelBuilder.Entity<Simulacion>(e =>
        {
            e.Property(x => x.Monto).HasColumnType("decimal(18,2)");
            e.Property(x => x.Tasa).HasColumnType("decimal(5,2)");

            e.HasOne(x => x.Prospecto)
                .WithMany(x => x.Simulaciones)
                .HasForeignKey(x => x.ProspectoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Solo puede existir una Simulacion aceptada por Prospecto.
            e.HasIndex(x => x.ProspectoId)
                .IsUnique()
                .HasFilter("[Aceptada] = 1")
                .HasDatabaseName("IX_Simulacion_ProspectoId_Aceptada");
        });

        // Cuota
        modelBuilder.Entity<Cuota>(e =>
        {
            e.Property(x => x.SaldoInicial).HasColumnType("decimal(18,2)");
            e.Property(x => x.Amortizacion).HasColumnType("decimal(18,2)");
            e.Property(x => x.Interes).HasColumnType("decimal(18,2)");
            e.Property(x => x.Otros).HasColumnType("decimal(18,2)");
            e.Property(x => x.PagoTotal).HasColumnType("decimal(18,2)");
            e.Property(x => x.SaldoFinal).HasColumnType("decimal(18,2)");

            e.HasOne(x => x.Simulacion)
                .WithMany(x => x.Cuotas)
                .HasForeignKey(x => x.SimulacionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DocumentoAdjunto
        modelBuilder.Entity<DocumentoAdjunto>(e =>
        {
            e.Property(x => x.NombreArchivo).HasMaxLength(255).IsRequired();

            e.HasOne(x => x.Prospecto)
                .WithMany(x => x.DocumentosAdjuntos)
                .HasForeignKey(x => x.ProspectoId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Requisito)
                .WithMany()
                .HasForeignKey(x => x.RequisitoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Un único archivo adjunto por (Prospecto, Requisito); un nuevo cargue reemplaza.
            e.HasIndex(x => new { x.ProspectoId, x.RequisitoId }).IsUnique();
        });

        // DocumentoGenerado
        modelBuilder.Entity<DocumentoGenerado>(e =>
        {
            e.HasOne(x => x.Prospecto)
                .WithMany(x => x.DocumentosGenerados)
                .HasForeignKey(x => x.ProspectoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
