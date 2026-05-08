namespace Farmacol.Models
{
    public class TbSolicitudTerceros
    {
        public int? Id { get; set; }
        public DateOnly? FechaRegistro { get; set; }
        public string? Solicitante { get; set; }
        public string? Cargo { get; set; }
        public string? Area { get; set; }
        public string? TipoSolicitud { get; set; }            
            public string? NombresTerceros { get; set; }
            public string? TipoDocumento { get; set; }
            public string? DocumentoTerceros { get; set; }
            public string? Empresa { get; set; }
            public string? ContactoEmpresa { get; set; }
            public DateTime? FechaIngreso { get; set; }
            public string? MotivoVisita { get; set; }
            public string? AreaDirigida { get; set; }
            public string? EquiposIngreso { get; set; }
            public string? IngresoVehiculo { get; set; }
            public string? PlacaVehiculo { get; set; }
            
        
            public string? DocumentacionSST { get; set; }
            

            public string? PlanillaDePago { get; set; }       
            public string? Identificacion { get; set; }         
            public string? CursosEspeciales { get; set; }     
            
        
            public string? RequiereCursosEspeciales { get; set; } 
            
            public string? RequiereEPP { get; set; }
            public string? ElementoEPP { get; set; }
            
        public string? Estado { get; set; }
        public string? AprobadoPor { get; set; }
        public DateTime? FechaAprobacionSST { get; set; }
        public DateTime? FechaRechazo { get; set; }
        public string? RechazadoPor { get; set; }
        public DateTime? FechaDevolucion { get; set; }
        public string? DevueltoPor { get; set; }
        public string? ObservacionEstado { get; set; }
        public string? ConductorNombre { get; set; }
        public string? ConductorLicenciaUrl { get; set; }
        public string? ConductorCedulaUrl { get; set; }
        public string? ConductorPlanillaSeguridadSocialUrl { get; set; }
        public string? ConductorFormacionVialUrl { get; set; }

        public string? VehiculoLicenciaTransitoUrl { get; set; }
        public string? VehiculoSOATUrl { get; set; }
        public string? VehiculoRTMUrl { get; set; }
        public string? VehiculoPolizasUrl { get; set; }
        public string? VehiculoPlanMantenimientoUrl { get; set; }
        public string? VehiculoInspeccionesUrl { get; set; }

        public string? EmpresaHabilitacionMTUrl { get; set; }
        public string? EmpresaReportePESVUrl { get; set; }
    }
}