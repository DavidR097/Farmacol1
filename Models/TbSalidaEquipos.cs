namespace Farmacol.Models
{
    public class TbSalidaEquipos
    {
        public string? Id { get; set; } 
        public DateOnly? FechaRegistro { get; set; }
        public string? Solicitante { get; set; }
        // Usuario corporativo (nombre de usuario) del solicitante
        public string? SolicitanteUsuario { get; set; }
        // Correo corporativo del solicitante
        public string? SolicitanteCorreo { get; set; }
        public string? Area { get; set; }
        public string? Elemento { get; set; }
        public int? Cantidad { get; set; }
        public string? Descripcion { get; set; }
        public string? CódigoSerie { get; set; }
        public string? MotivoSalida { get; set; }
        public string? Observacion { get; set; }
        public string? Destino { get; set; }
        public string? DebeRegresar { get; set; }
        public DateOnly? FechaSalida { get; set; }
        public DateOnly? FechaRegreso { get; set; }
        public string? Estado { get; set; }
        public string? ObservacionEstado { get; set; }
        public DateOnly? Fecha { get; set; }
        public string? EstadoConsulta { get; set; }
        public string? EtapaAprobacion { get; set; }
        public string? AprobacionGerencia { get; set; }
        public string? AprobacionCH { get; set; }
        // Timestamps for approvals
        public DateTime? FechaAprobacionGerencia { get; set; }
        public DateTime? FechaAprobacionCH { get; set; }
    }
}