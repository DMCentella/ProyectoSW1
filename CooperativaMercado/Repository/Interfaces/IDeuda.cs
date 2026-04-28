using CooperativaMercado.Model;

namespace CooperativaMercado.Repository.Interfaces
{
    public interface IDeuda
    {
        List<Deuda> Listar();
        Deuda ObtenerPorId(int id);

        List<Deuda> ObtenerPorPuesto(int idPuesto);
        List<Deuda> ObtenerPendientes();

        int Registrar(Deuda deuda);

        int GenerarAlquilerMensual(int mes, int anio);
    }
}
