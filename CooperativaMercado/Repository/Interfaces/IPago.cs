using CooperativaMercado.Model;

namespace CooperativaMercado.Repository.Interfaces
{
    public interface IPago
    {
        int RegistrarPago(int idDeuda, decimal monto, string metodoPago);
        List<Pago> Listar();
        List<Pago> ObtenerPorPuesto(int idPuesto);
        Pago ObtenerDatosBoleta(int idPago);
    }
}
