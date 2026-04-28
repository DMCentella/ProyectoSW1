namespace CooperativaMercado.Model
{
    public class ReporteDeuda
    {
        public int IdDeuda { get; set; }
        public string? Puesto { get; set; }
        public string? Socio { get; set; }
        public decimal Monto { get; set; }
        public int Mes { get; set; }
        public int Anio { get; set; }
        public string? Estado { get; set; }

    }
}
