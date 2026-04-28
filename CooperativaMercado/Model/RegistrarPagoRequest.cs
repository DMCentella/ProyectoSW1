namespace CooperativaMercado.Model
{
    public class RegistrarPagoRequest
    {
        public int IdDeuda { get; set; }
        public decimal Monto { get; set; }
        public string MetodoPago { get; set; }
    }
}
