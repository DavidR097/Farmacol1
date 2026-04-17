using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Farmacol.Models;

public partial class Farmacol1Context : IdentityDbContext<IdentityUser, IdentityRole, string>
{
    public Farmacol1Context() { }

    public Farmacol1Context(DbContextOptions<Farmacol1Context> options)
        : base(options) { }

    public virtual DbSet<Tbinventario> Tbinventarios { get; set; }
    public virtual DbSet<Tbnotificacione> Tbnotificaciones { get; set; }
    public virtual DbSet<Tbpersonal> Tbpersonals { get; set; }
    public virtual DbSet<Tbresponsiva> Tbresponsivas { get; set; }
    public virtual DbSet<TbsoliRechazadum> TbsoliRechazada { get; set; }
    public virtual DbSet<Tbsolicitude> Tbsolicitudes { get; set; }
    public virtual DbSet<TbsubtiposPermiso> TbsubtiposPermisos { get; set; }
    public virtual DbSet<TbtiposSolicitud> TbtiposSolicituds { get; set; }
    public virtual DbSet<Tbvacacione> Tbvacaciones { get; set; }
    public virtual DbSet<Tbarea> Tbareas { get; set; }
    public virtual DbSet<TbpersonalRetirado> TbpersonalRetirados { get; set; }
    public virtual DbSet<TbDelegacion> TbDelegaciones { get; set; }
    public virtual DbSet<TbExpediente> TbExpedientes { get; set; }
    public virtual DbSet<TbPlantilla> TbPlantillas { get; set; }
    public virtual DbSet<TbAuditTrail> TbAuditTrails { get; set; }
    public virtual DbSet<TbSala> TbSalas { get; set; }
    public virtual DbSet<TbReservaSala> TbReservasSalas { get; set; }
    public virtual DbSet<TbAnuncio> TbAnuncios { get; set; }
    public virtual DbSet<TbRequisicione> TbRequisiciones { get; set; }
    public virtual DbSet<TbCarpeta> TbCarpetas { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    => optionsBuilder.UseSqlServer("Server=DARIANO\\SQLEXPRESS03;database=Farmacol1;integrated security=true;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tbinventario>(entity =>
        {
            entity.HasKey(e => e.IdEquipo).HasName("PK__TBInvent__AB5A5EA976A52A69");
            entity.ToTable("TBInventario");
            entity.Property(e => e.IdEquipo).HasColumnName("ID_Equipo");
            entity.Property(e => e.CC).HasColumnName("Cédula");
            entity.Property(e => e.Anexo).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.Dispositivo).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.Imei).HasMaxLength(100).IsUnicode(false).HasColumnName("IMEI");
            entity.Property(e => e.Marca).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Modelo).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Observación).HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.Planta).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Serie).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Ubicación).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.Ubicación2).HasMaxLength(200).IsUnicode(false);
        });

        modelBuilder.Entity<Tbnotificacione>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion).HasName("PK__TBNotifi");
            entity.ToTable("TBNotificaciones");
            entity.Property(e => e.IdNotificacion).HasColumnName("ID_Notificacion");
            entity.Property(e => e.UsuarioDestino).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Mensaje).HasMaxLength(300).IsUnicode(false);
            entity.Property(e => e.IdSolicitud).HasColumnName("ID_Solicitud");
        });

        modelBuilder.Entity<Tbpersonal>(entity =>
        {
            entity.ToTable("TBPersonal");
            entity.HasKey(e => e.CC);

            entity.Property(e => e.CC).ValueGeneratedNever();
            entity.Property(e => e.ExpedicionCiudad).HasMaxLength(100);
            entity.Property(e => e.CiudadTrabajo).HasMaxLength(100);
            entity.Property(e => e.NombreColaborador).HasMaxLength(150);
            entity.Property(e => e.Cargo).HasMaxLength(100);
            entity.Property(e => e.CodCeco);
            entity.Property(e => e.NombreCentroCostos).HasMaxLength(150);
            entity.Property(e => e.Area).HasMaxLength(100);
            entity.Property(e => e.Gerencia).HasMaxLength(150);
            entity.Property(e => e.MesNacimiento).HasMaxLength(20);
            entity.Property(e => e.Generacion).HasMaxLength(50);
            entity.Property(e => e.Genero).HasMaxLength(20);
            entity.Property(e => e.CiudadNacimiento).HasMaxLength(100);
            entity.Property(e => e.EstadoCivil).HasMaxLength(30);
            entity.Property(e => e.CorreoPersonal).HasMaxLength(150);
            entity.Property(e => e.Contacto).HasMaxLength(100);
            entity.Property(e => e.Contacto).HasMaxLength(100);
            entity.Property(e => e.DireccionResidencia).HasMaxLength(200);
            entity.Property(e => e.Barrio).HasMaxLength(100);
            entity.Property(e => e.Rh).HasMaxLength(10);
            entity.Property(e => e.ContactoEmergencia).HasMaxLength(150);
            entity.Property(e => e.Parentesco).HasMaxLength(50);
            entity.Property(e => e.TelefonoEmergencia).HasMaxLength(30);
            entity.Property(e => e.TipoContrato).HasMaxLength(50);
            entity.Property(e => e.Eps).HasMaxLength(100);
            entity.Property(e => e.FondoPensiones).HasMaxLength(100);
            entity.Property(e => e.FondoCesantias).HasMaxLength(100);
            entity.Property(e => e.CajaCompensacion).HasMaxLength(100);
            entity.Property(e => e.Arl).HasMaxLength(100);
            entity.Property(e => e.TipoCuenta).HasMaxLength(30);
            entity.Property(e => e.NumeroCuenta).HasMaxLength(30);
            entity.Property(e => e.Banco).HasMaxLength(100);
            entity.Property(e => e.TallaCamisa).HasMaxLength(10);
            entity.Property(e => e.Grupo).HasMaxLength(50);
            entity.Property(e => e.Concepto).HasMaxLength(200);
            entity.Property(e => e.CorreoCorporativo).HasMaxLength(150);
            entity.Property(e => e.UsuarioCorporativo).HasMaxLength(100);
        });

        modelBuilder.Entity<Tbresponsiva>(entity =>
        {
            entity.HasKey(e => e.CC).HasName("PK__TBRespon__F12AB28853CB5A26");
            entity.ToTable("TBResponsivas");
            entity.Property(e => e.CC)
                  .ValueGeneratedNever()
                  .HasColumnName("Cédula");
            entity.Property(e => e.Equipo).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Estado).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Marca).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Observación).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Serie).HasMaxLength(100).IsUnicode(false);
        });

        modelBuilder.Entity<TbsoliRechazadum>(entity =>
        {
            entity.HasKey(e => e.IdSolicitud).HasName("PK__TBSoliRe__ED71123A21CDC8AB");
            entity.ToTable("TBSoliRechazada");
            entity.Property(e => e.IdSolicitud).ValueGeneratedNever().HasColumnName("ID_Solicitud");
            entity.Property(e => e.CC).HasColumnName("Cédula");
            entity.Property(e => e.Anexos).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.FechaSolicitud).HasColumnName("Fecha_Solicitud");
            entity.Property(e => e.Motivo).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Observaciones).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.TipoSolicitud).HasMaxLength(100).IsUnicode(false).HasColumnName("Tipo_Solicitud");
        });

        modelBuilder.Entity<Tbsolicitude>(entity =>
        {
            entity.HasKey(e => e.IdSolicitud).HasName("PK__TBSolici__ED71123A33D0234D");
            entity.ToTable("TBSolicitudes");
            entity.Property(e => e.IdSolicitud).HasColumnName("ID_Solicitud");
            entity.Property(e => e.Anexos).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.AprobCh).HasMaxLength(10).IsUnicode(false).HasColumnName("Aprob_CH");
            entity.Property(e => e.AprobJinmediato).HasMaxLength(10).IsUnicode(false).HasColumnName("Aprob_JInmediato");
            entity.Property(e => e.Cargo).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.CargoJinmediato).HasMaxLength(100).IsUnicode(false).HasColumnName("Cargo_JInmediato");
            entity.Property(e => e.Estado).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.EtapaAprobacion).HasMaxLength(50).IsUnicode(false).HasColumnName("EtapaAprobacion");
            entity.Property(e => e.FechaFin).HasColumnName("Fecha_Fin");
            entity.Property(e => e.FechaInicio).HasColumnName("Fecha_Inicio");
            entity.Property(e => e.FechaSolicitud).HasColumnName("Fecha_Solicitud");
            entity.Property(e => e.HoraFin).HasColumnName("Hora_Fin");
            entity.Property(e => e.HoraInicio).HasColumnName("Hora_Inicio");
            entity.Property(e => e.JefeInmediato).HasMaxLength(100).IsUnicode(false).HasColumnName("Jefe_Inmediato");
            entity.Property(e => e.Motivo).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.ObservacionJefe).HasMaxLength(300).IsUnicode(false).HasColumnName("ObservacionJefe");
            entity.Property(e => e.ObservacionRRHH).HasMaxLength(300).IsUnicode(false).HasColumnName("ObservacionRRHH");
            entity.Property(e => e.Observaciones).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.SubtipoPermiso).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.TipoSolicitud).HasMaxLength(200).IsUnicode(false).HasColumnName("Tipo_Solicitud");
            entity.Property(e => e.TotalDias).HasColumnName("Total_Dias");
            entity.Property(e => e.TotalHoras).HasColumnName("Total_Horas");
            entity.Property(e => e.Paso1Aprobador).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Paso1Estado).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Paso1Obs).HasMaxLength(300).IsUnicode(false);
            entity.Property(e => e.Paso2Aprobador).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Paso2Estado).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Paso2Obs).HasMaxLength(300).IsUnicode(false);
            entity.Property(e => e.Paso3Aprobador).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Paso3Estado).HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.Paso3Obs).HasMaxLength(300).IsUnicode(false);
            entity.Property(e => e.TipoFlujo).HasMaxLength(50);
            entity.Property(e => e.DocumentoSolicitado).HasMaxLength(100);

        });

        modelBuilder.Entity<TbsubtiposPermiso>(entity =>
        {
            entity.HasKey(e => e.IdSubtipo).HasName("PK__TBSubtip__730213EB2F928969");
            entity.ToTable("TBSubtiposPermiso");
            entity.Property(e => e.IdSubtipo).HasColumnName("ID_Subtipo");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false);
        });

        modelBuilder.Entity<TbtiposSolicitud>(entity =>
        {
            entity.HasKey(e => e.IdTipo).HasName("PK__TBTiposS__D34E66199410A618");
            entity.ToTable("TBTiposSolicitud");
            entity.Property(e => e.IdTipo).HasColumnName("ID_Tipo");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false);
        });

        modelBuilder.Entity<Tbvacacione>(entity =>
        {
            entity.HasKey(e => e.IdVacación).HasName("PK__TBVacaci__C39CCDF16ADBF4C9");
            entity.ToTable("TBVacaciones");
            entity.Property(e => e.IdVacación).HasColumnName("ID_Vacación");
            entity.Property(e => e.Anexos).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Cargo).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.CC).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.FechaFin).HasColumnName("Fecha_Fin");
            entity.Property(e => e.FechaInicio).HasColumnName("Fecha_Inicio");
            entity.Property(e => e.FechaSolicitud).HasColumnName("Fecha_Solicitud");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Observaciones).HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.TotalDías).HasColumnName("Total_Días");
        });

        modelBuilder.Entity<Tbarea>(entity =>
        {
            entity.HasKey(e => e.IdArea).HasName("PK__TBAreas");
            entity.ToTable("TBAreas");
            entity.Property(e => e.IdArea).HasColumnName("ID_Area");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false);
        });

        modelBuilder.Entity<TbpersonalRetirado>(entity =>
        {
            entity.ToTable("TBPersonalRetirado");
            entity.Property(e => e.CC).ValueGeneratedNever();
        });

        modelBuilder.Entity<TbDelegacion>(entity =>
        {
            entity.ToTable("TBDelegaciones");
        });

        modelBuilder.Entity<TbExpediente>(entity =>
        {
            entity.ToTable("TBExpedientes", schema: "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CC).IsRequired();
            entity.Property(e => e.NombreArchivo).HasMaxLength(300).IsRequired();
            entity.Property(e => e.TipoDocumento).HasMaxLength(100);
            entity.Property(e => e.RutaArchivo).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Modulo).HasMaxLength(20);
            entity.Property(e => e.SubidoPor).HasMaxLength(200);
            entity.Property(e => e.Visible).HasDefaultValue(true);
            entity.Property(e => e.FechaSubida).HasDefaultValueSql("GETDATE()");
        });


        modelBuilder.Entity<TbPlantilla>(entity =>
        {
            entity.ToTable("TBPlantillas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(200);
            entity.Property(e => e.TipoDocumento).HasMaxLength(100);
            entity.Property(e => e.Modulo).HasMaxLength(20);
            entity.Property(e => e.RutaArchivo).HasMaxLength(500);
            entity.Property(e => e.SubidaPor).HasMaxLength(200);
            entity.Property(e => e.FechaSubida).HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<TbAuditTrail>(entity =>
        {
            entity.ToTable("TBAuditTrail");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Fecha).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Usuario).HasMaxLength(200);
            entity.Property(e => e.Modulo).HasMaxLength(100);
            entity.Property(e => e.Accion).HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.EntidadId).HasMaxLength(100);
            entity.Property(e => e.Ip).HasMaxLength(50);
        });

        modelBuilder.Entity<TbSala>(entity =>
        {
            entity.ToTable("TBSalas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Activa).HasDefaultValue(true);
        });

        modelBuilder.Entity<TbReservaSala>(entity =>
        {
            entity.ToTable("TBReservasSalas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NombreSolicitante).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Cargo).HasMaxLength(200);
            entity.Property(e => e.Area).HasMaxLength(200);
            entity.Property(e => e.Motivo).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(50).HasDefaultValue("Pendiente");
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.Property(e => e.AtendidaPor).HasMaxLength(200);
            entity.Property(e => e.FechaSolicitud).HasDefaultValueSql("GETDATE()");
            entity.HasOne(e => e.Sala).WithMany()
                  .HasForeignKey(e => e.SalaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
