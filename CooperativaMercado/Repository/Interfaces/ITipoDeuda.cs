using CooperativaMercado.Model;

namespace CooperativaMercado.Repository.Interfaces
{
    public interface ITipoDeuda
    {

        List<TipoDeuda> Listar();
        TipoDeuda ObtenerPorId(int id);

        int Registrar(TipoDeuda tipo);
        int Actualizar(TipoDeuda tipo);

        int Desactivar(int id);
    }
}
