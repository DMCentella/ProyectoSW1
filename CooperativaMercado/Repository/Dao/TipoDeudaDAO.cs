using CooperativaMercado.Model;
using CooperativaMercado.Repository.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CooperativaMercado.Repository.Dao
{
    public class TipoDeudaDao : ITipoDeuda
    {
        private readonly string connectionString;

        public TipoDeudaDao()
        {
            connectionString = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build()
                .GetConnectionString("dataBase")!;
        }

        public List<TipoDeuda> Listar()
        {
            List<TipoDeuda> lista = new List<TipoDeuda>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ListarTipoDeuda", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(new TipoDeuda()
                    {
                        IdTipoDeuda = dr.GetInt32(0),
                        Nombre = dr.GetString(1),
                        MontoBase = dr.IsDBNull(2) ? null : dr.GetDecimal(2),
                        Activo = dr.GetBoolean(3)
                    });
                }
            }

            return lista;
        }

        public TipoDeuda ObtenerPorId(int id)
        {
            TipoDeuda tipo = null;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ObtenerTipoDeuda", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdTipoDeuda", id);
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    tipo = new TipoDeuda()
                    {
                        IdTipoDeuda = dr.GetInt32(0),
                        Nombre = dr.GetString(1),
                        MontoBase = dr.IsDBNull(2) ? null : dr.GetDecimal(2),
                        Activo = dr.GetBoolean(3)
                    };
                }
            }

            return tipo;
        }

        public int Registrar(TipoDeuda tipo)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_RegistrarTipoDeuda", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Nombre", tipo.Nombre);
                cmd.Parameters.AddWithValue("@MontoBase", (object?)tipo.MontoBase ?? DBNull.Value);
                return cmd.ExecuteNonQuery();
            }
        }

        public int Actualizar(TipoDeuda tipo)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ActualizarTipoDeuda", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdTipoDeuda", tipo.IdTipoDeuda);
                cmd.Parameters.AddWithValue("@Nombre", tipo.Nombre);
                cmd.Parameters.AddWithValue("@MontoBase", (object?)tipo.MontoBase ?? DBNull.Value);
                return cmd.ExecuteNonQuery();
            }
        }

        public int Desactivar(int id)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_DesactivarTipoDeuda", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdTipoDeuda", id);
                return cmd.ExecuteNonQuery();
            }
        }

    }
}
