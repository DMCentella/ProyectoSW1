using CooperativaMercado.Model;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CooperativaMercado.Repository.Dao
{
    public class HistorialPuestoDao
    {
        private readonly string connectionString;

        public HistorialPuestoDao()
        {
            connectionString = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build()
                .GetConnectionString("dataBase")!;
        }

        public List<HistorialPuesto> ObtenerPorPuesto(int idPuesto)
        {
            List<HistorialPuesto> lista = new List<HistorialPuesto>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ObtenerHistorialPuesto", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdPuesto", idPuesto);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(new HistorialPuesto()
                    {
                        IdHistorial = dr.GetInt32(0),
                        IdPuesto = dr.GetInt32(1),
                        IdSocio = dr.GetInt32(2),
                        NombreSocio = dr.GetString(3),
                        FechaInicio = dr.GetDateTime(4),
                        FechaFin = dr.IsDBNull(5) ? null : dr.GetDateTime(5),
                        MotivoRetiro = dr.IsDBNull(6) ? null : dr.GetString(6),
                        Activo = dr.GetBoolean(7)
                    });
                }
            }
            return lista;
        }
    }
}
