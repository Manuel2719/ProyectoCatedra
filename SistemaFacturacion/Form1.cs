using System;
using System.Data;
// Esta librería permite comunicar el programa con SQL Server
using System.Data.SqlClient;
using System.Text;
using System.Windows.Forms;

namespace SistemaFacturacion
{
    public partial class Form1 : Form
    {
        // Variable para ir sumando el dinero de la venta actual
        decimal totalFactura = 0;

        string conexionString = @"Server=localhost\SQLEXPRESS; Database=TiendaFacturacionDB; Trusted_Connection=True;";

        public Form1()
        {
            InitializeComponent();
        }

        // Esto se ejecuta automáticamente al abrir la aplicación
        private void Form1_Load(object sender, EventArgs e)
        {
            ConfigurarTabla();

            dgvDetalle.AllowUserToAddRows = false;

            txtProducto.ReadOnly = true;
            txtPrecio.ReadOnly = true;

            CargarProductos();
        }

        // Prepara las columnas de la tabla para que no inicie vacía
        private void ConfigurarTabla()
        {
            dgvDetalle.Columns.Add("Producto", "Producto");
            dgvDetalle.Columns.Add("Precio", "Precio Unitario");
            dgvDetalle.Columns.Add("Cantidad", "Cantidad");
            dgvDetalle.Columns.Add("Subtotal", "Subtotal");
        }

        private void ProbarConexion()
        {
            try
            {
                using (SqlConnection conexion = new SqlConnection(conexionString))
                {
                    conexion.Open();

                    string consulta = "SELECT COUNT(*) FROM Productos";

                    using (SqlCommand comando = new SqlCommand(consulta, conexion))
                    {
                        int cantidad = Convert.ToInt32(comando.ExecuteScalar());

                        MessageBox.Show(
                            $"Conexión exitosa.\nProductos registrados: {cantidad}"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error de conexión: " + ex.Message);
            }
        }

        private void CargarProductos()
        {
            try
            {
                using (SqlConnection conexion = new SqlConnection(conexionString))
                {
                    conexion.Open();

                    string consulta = "SELECT ID_Producto, Nombre, Precio FROM Productos";

                    SqlDataAdapter adaptador = new SqlDataAdapter(consulta, conexion);

                    DataTable tabla = new DataTable();

                    adaptador.Fill(tabla);

                    cboProductos.DataSource = tabla;
                    cboProductos.DisplayMember = "Nombre";
                    cboProductos.ValueMember = "ID_Producto";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los productos: " + ex.Message);
            }
        }

        // Se ejecuta al presionar "Agregar Producto"
        private void btnAgregar_Click(object sender, EventArgs e)
        {
            if (cboProductos.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un producto.");
                return;
            }

            if (!decimal.TryParse(txtPrecio.Text, out decimal precio))
            {
                MessageBox.Show("El precio del producto no es válido.");
                return;
            }

            if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida mayor que cero.");
                return;
            }

            string nombre = txtProducto.Text;

            decimal subtotal = precio * cantidad;

            dgvDetalle.Rows.Add(nombre, precio, cantidad, subtotal);

            totalFactura += subtotal;

            lblTotal.Text = "Total a Pagar: $" + totalFactura.ToString("N2");

            txtCantidad.Clear();
        }

        // Se ejecuta al presionar "Generar Factura"
        private void btnFacturar_Click(object sender, EventArgs e)
        {
            if (dgvDetalle.Rows.Count == 0)
            {
                MessageBox.Show("No hay productos cargados en la factura.");
                return;
            }

            // Conectar a la base de datos SQL Server y guardar la venta
            using (SqlConnection conexion = new SqlConnection(conexionString))
            {
                try
                {
                    conexion.Open();
                    string query = "INSERT INTO Ventas (Fecha, Total) VALUES (GETDATE(), @Total)";

                    using (SqlCommand comando = new SqlCommand(query, conexion))
                    {
                        comando.Parameters.AddWithValue("@Total", totalFactura);
                        comando.ExecuteNonQuery(); // Ejecuta el guardado en SQL
                    }

                    MessageBox.Show("¡Venta registrada con éxito!");

                    // Limpiar todo para atender al siguiente cliente
                    MostrarFactura();

                    dgvDetalle.Rows.Clear();

                    totalFactura = 0;

                    lblTotal.Text = "Total a Pagar: $0.00";
                }

                catch (Exception ex)
                {
                    // Si falla SQL avisará aquí
                    MessageBox.Show("Nota: El cálculo local funcionó, pero falló la base de datos: " + ex.Message);
                }
            }
        }

        private void cboProductos_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboProductos.SelectedItem is DataRowView producto)
            {
                txtProducto.Text = producto["Nombre"].ToString();

                decimal precio = Convert.ToDecimal(producto["Precio"]);

                txtPrecio.Text = precio.ToString("0.00");
            }
        }

        private void MostrarFactura()
        {
            StringBuilder factura = new StringBuilder();

            factura.AppendLine("TIENDA - COMPROBANTE DE VENTA");
            factura.AppendLine("------------------------------");

            factura.AppendLine("Fecha: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"));

            factura.AppendLine("------------------------------");

            foreach (DataGridViewRow fila in dgvDetalle.Rows)
            {
                if (fila.IsNewRow)
                    continue;

                string nombre = fila.Cells["Producto"].Value.ToString() ?? "";

                decimal precio = Convert.ToDecimal(fila.Cells["Precio"].Value);

                int cantidad = Convert.ToInt32(fila.Cells["Cantidad"].Value);

                decimal subtotal = Convert.ToDecimal(fila.Cells["Subtotal"].Value);

                factura.AppendLine("Producto: " + nombre);
                factura.AppendLine("Precio: $" + precio.ToString("N2"));
                factura.AppendLine("Cantidad: " + cantidad);
                factura.AppendLine("Subtotal: $" + subtotal.ToString("N2"));

                factura.AppendLine("------------------------------");
            }

            factura.AppendLine("TOTAL: $" + totalFactura.ToString("N2"));

            factura.AppendLine("------------------------------");
            factura.AppendLine("Gracias por su compra.");

            MessageBox.Show(factura.ToString(), "Comprobante de venta");
        }
    }
}
