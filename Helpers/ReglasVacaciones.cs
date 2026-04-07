public static class ReglasVacaciones
{
    public static bool PuedeSolicitar(DateTime fechaInicio)
    {
        var hoy = DateTime.Today;

        // Si es el mismo mes y año
        if (fechaInicio.Year == hoy.Year &&
            fechaInicio.Month == hoy.Month)
        {
            return hoy.Day <= 10;
        }

        // Mes siguiente o posterior → OK
        return fechaInicio >= new DateTime(hoy.Year, hoy.Month, 1).AddMonths(1);
    }
}