using CooperativaMercado.Model;
using CooperativaMercado.Repository.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CooperativaMercado.Repository.Dao
{
    public class IngresoDao : IIngreso
    {
        private readonly string connectionString;

        public IngresoDao()
        {
            connectionString = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build()
                .GetConnectionString("dataBase")!;
        }

        public int RegistrarIngreso(IngresoDiario ingreso)
        {
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_RegistrarIngreso", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdPuesto", ingreso.IdPuesto);
                cmd.Parameters.AddWithValue("@Fecha", ingreso.Fecha);
                cmd.Parameters.AddWithValue("@Monto", ingreso.Monto);
                if (ingreso.IdUsuario == 0)
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@IdUsuario", ingreso.IdUsuario);
                }

                return cmd.ExecuteNonQuery();
            }
        }

        public List<IngresoDiario> Listar()
        {
            List<IngresoDiario> lista = new List<IngresoDiario>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ListarIngresos", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(new IngresoDiario()
                    {
                        IdIngreso = dr.GetInt32(0),
                        IdPuesto = dr.GetInt32(1),
                        Fecha = dr.GetDateTime(3),
                        Monto = dr.GetDecimal(4),
                        IdUsuario = dr.IsDBNull(6) ? (int?)null : dr.GetInt32(6)
                    });
                }
            }
            return lista;
        }

        public List<IngresoDiario> ObtenerPorPuesto(int idPuesto)
        {
            List<IngresoDiario> lista = new List<IngresoDiario>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ListarIngresos", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    if (dr.GetInt32(1) == idPuesto)
                    {
                        lista.Add(new IngresoDiario()
                        {
                            IdIngreso = dr.GetInt32(0),
                            IdPuesto = dr.GetInt32(1),
                            Fecha = dr.GetDateTime(2),
                            Monto = dr.GetDecimal(3),
                            FechaRegistro = dr.GetDateTime(4),
                            IdUsuario = dr.GetInt32(5)
                        });
                    }
                }
            }
            return lista;
        }

        public decimal TotalIngresosPorRango(DateTime inicio, DateTime fin)
        {
            decimal total = 0;
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ReporteIngresosPorRango", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FechaInicio", inicio.Date);
                cmd.Parameters.AddWithValue("@FechaFin", fin.Date);
                var result = cmd.ExecuteScalar();
                if (result != null)
                    total = Convert.ToDecimal(result);
            }
            return total;
        }

        public List<ReporteIngreso> ReporteIngresosPorPuesto(DateTime inicio, DateTime fin)
        {
            List<ReporteIngreso> lista = new List<ReporteIngreso>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ReporteIngresosPorPuesto", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FechaInicio", inicio.Date);
                cmd.Parameters.AddWithValue("@FechaFin", fin.Date);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(new ReporteIngreso()
                    {
                        Puesto = dr.GetString(0),
                        TotalIngresos = dr.GetDecimal(1)
                    });
                }
            }
            return lista;
        }

        public List<IngresoDiario> ObtenerPorSocio(int idSocio, DateTime? inicio = null, DateTime? fin = null)
        {
            List<IngresoDiario> lista = new List<IngresoDiario>();
            using (SqlConnection cn = new SqlConnection(connectionString))
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("sp_ObtenerIngresosPorSocio", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@IdSocio", idSocio);
                cmd.Parameters.AddWithValue("@FechaInicio", inicio ?? DateTime.Today);
                cmd.Parameters.AddWithValue("@FechaFin", fin ?? DateTime.Today);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    lista.Add(new IngresoDiario()
                    {
                        NumeroPuesto = dr.GetString(0),
                        IdIngreso = dr.GetInt32(1),
                        IdPuesto = dr.GetInt32(2),
                        Fecha = dr.GetDateTime(3),
                        Monto = dr.GetDecimal(4),
                        FechaRegistro = dr.GetDateTime(5)
                    });
                }
            }
            return lista;
        }
    }
}
