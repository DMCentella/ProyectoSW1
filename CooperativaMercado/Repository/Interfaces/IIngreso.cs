using CooperativaMercado.Model;

namespace CooperativaMercado.Repository.Interfaces
{
    public interface IIngreso
    {
        int RegistrarIngreso(IngresoDiario ingreso);

        List<IngresoDiario> Listar();
        List<IngresoDiario> ObtenerPorPuesto(int idPuesto);


    }
}

