namespace CooperativaMercado.Model
{
    public class Pago
    {
        public int IdPago { get; set; }
        public int IdDeuda { get; set; }
        public string? NombreSocio { get; set; }
        public string? ConceptoDeuda { get; set; }
        public string? NumeroPuesto { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string NumeroRecibo { get; set; }
        public string? MetodoPago { get; set; }
    }
}
