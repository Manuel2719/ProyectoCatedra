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
        decimal subtotalFactura = 0;
        decimal impuestoFactura = 0;
        decimal descuentoFactura = 0;

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

        private void CalcularTotales()
        {
            // El precio de los productos ya incluye IVA.
            decimal totalConIVA = 0;

            foreach (DataGridViewRow fila in dgvDetalle.Rows)
            {
                if (fila.IsNewRow)
                    continue;

                totalConIVA += Convert.ToDecimal(
                    fila.Cells["Subtotal"].Value);
            }

            // Calculamos la base imponible y el IVA incluido.
            subtotalFactura = Math.Round(
                totalConIVA / 1.13m, 2,
                MidpointRounding.AwayFromZero);

            impuestoFactura = totalConIVA - subtotalFactura;

            // Por ahora no aplicamos descuentos.
            descuentoFactura = 0;

            totalFactura = totalConIVA - descuentoFactura;

            lblTotal.Text =
                "Total a pagar: $" + totalFactura.ToString("N2");
        }

        // Prepara las columnas de la tabla para que no inicie vacía

        private void ConfigurarTabla()
        {
            dgvDetalle.Columns.Clear();

            dgvDetalle.Columns.Add("ID_Producto", "ID");
            dgvDetalle.Columns.Add("Producto", "Producto");
            dgvDetalle.Columns.Add("Precio", "Precio Unitario");
            dgvDetalle.Columns.Add("Cantidad", "Cantidad");
            dgvDetalle.Columns.Add("Subtotal", "Subtotal");

            // El usuario no necesita ver el identificador.
            dgvDetalle.Columns["ID_Producto"].Visible = false;

            dgvDetalle.AllowUserToAddRows = false;
            dgvDetalle.ReadOnly = true;
            dgvDetalle.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
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

                    string consulta = @"
                    SELECT ID_Producto, Nombre, Precio, Stock
                    FROM Productos
                    WHERE Activo = 1";

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
            if (!(cboProductos.SelectedItem is DataRowView producto))
            {
                MessageBox.Show("Seleccione un producto.");
                return;
            }

            int idProducto = Convert.ToInt32(producto["ID_Producto"]);
            string nombre = producto["Nombre"].ToString();
            decimal precio = Convert.ToDecimal(producto["Precio"]);

            if (!int.TryParse(txtCantidad.Text, out int cantidad)
                || cantidad <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida mayor que cero.");
                return;
            }

            decimal subtotal = precio * cantidad;

            dgvDetalle.Rows.Add(
                idProducto,
                nombre,
                precio,
                cantidad,
                subtotal
            );

            int stockDisponible = Convert.ToInt32(producto["Stock"]);

            // Sumamos las unidades de este producto
            // que ya están agregadas al carrito.
            int cantidadEnCarrito = 0;

            foreach (DataGridViewRow fila in dgvDetalle.Rows)
            {
                if (fila.IsNewRow)
                    continue;

                int idEnCarrito = Convert.ToInt32(
                    fila.Cells["ID_Producto"].Value);

                if (idEnCarrito == idProducto)
                {
                    cantidadEnCarrito += Convert.ToInt32(
                        fila.Cells["Cantidad"].Value);
                }
            }

            if (cantidad > stockDisponible - cantidadEnCarrito)
            {
                MessageBox.Show(
                    "No hay suficientes existencias.\n" +
                    "Stock disponible: " + stockDisponible + "\n" +
                    "Unidades en el carrito: " + cantidadEnCarrito,
                    "Stock insuficiente",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            CalcularTotales();
            txtCantidad.Clear();
            txtCantidad.Focus();
        }


        // Se ejecuta al presionar "Generar Factura"

        private void btnFacturar_Click(object sender, EventArgs e)
        {
            if (dgvDetalle.Rows.Count == 0)
            {
                MessageBox.Show("No hay productos en la factura.");
                return;
            }

            using (SqlConnection conexion = new SqlConnection(conexionString))
            {
                try
                {
                    conexion.Open();

                    using (SqlTransaction transaccion = conexion.BeginTransaction())
                    {
                        try
                        {
                            // 1. Registrar la venta.
                            string sqlVenta = @"
                        INSERT INTO Ventas
                            (Fecha, Subtotal, Impuesto, Descuento, Total)
                        OUTPUT INSERTED.ID_Venta
                        VALUES
                            (GETDATE(), @Subtotal, @Impuesto, @Descuento, @Total)";

                            int idVenta;

                            using (SqlCommand comando = new SqlCommand(
                                sqlVenta, conexion, transaccion))
                            {
                                comando.Parameters.AddWithValue(
                                    "@Subtotal", subtotalFactura);

                                comando.Parameters.AddWithValue(
                                    "@Impuesto", impuestoFactura);

                                comando.Parameters.AddWithValue(
                                    "@Descuento", descuentoFactura);

                                comando.Parameters.AddWithValue(
                                    "@Total", totalFactura);


                                idVenta = Convert.ToInt32(comando.ExecuteScalar());

                            }

                            // 2. Registrar cada producto vendido.
                            string sqlDetalle = @"
                        INSERT INTO DetalleVentas
                            (ID_Venta, ID_Producto, Cantidad,
                             PrecioUnitario, Subtotal)
                        VALUES
                            (@ID_Venta, @ID_Producto, @Cantidad,
                             @PrecioUnitario, @Subtotal)";

                            foreach (DataGridViewRow fila in dgvDetalle.Rows)
                            {
                                if (fila.IsNewRow)
                                    continue;

                                int idProducto = Convert.ToInt32(
                                    fila.Cells["ID_Producto"].Value);

                                int cantidad = Convert.ToInt32(
                                    fila.Cells["Cantidad"].Value);

                                decimal precio = Convert.ToDecimal(
                                    fila.Cells["Precio"].Value);

                                decimal subtotal = Convert.ToDecimal(
                                    fila.Cells["Subtotal"].Value);

                                using (SqlCommand comandoDetalle =
                                    new SqlCommand(sqlDetalle, conexion, transaccion))
                                {
                                    comandoDetalle.Parameters.AddWithValue(
                                        "@ID_Venta", idVenta);

                                    comandoDetalle.Parameters.AddWithValue(
                                        "@ID_Producto", idProducto);

                                    comandoDetalle.Parameters.AddWithValue(
                                        "@Cantidad", cantidad);

                                    comandoDetalle.Parameters.AddWithValue(
                                        "@PrecioUnitario", precio);

                                    comandoDetalle.Parameters.AddWithValue(
                                        "@Subtotal", subtotal);

                                    comandoDetalle.ExecuteNonQuery();
                                }


                                // Descontamos las unidades vendidas.
                                // La condición evita que el stock quede negativo.
                                string sqlStock = @"
                                UPDATE Productos
                                SET Stock = Stock - @Cantidad
                                WHERE ID_Producto = @ID_Producto
                                  AND Activo = 1
                                  AND Stock >= @Cantidad";

                                using (SqlCommand comandoStock =
                                    new SqlCommand(sqlStock, conexion, transaccion))
                                {
                                    comandoStock.Parameters.AddWithValue(
                                        "@Cantidad", cantidad);

                                    comandoStock.Parameters.AddWithValue(
                                        "@ID_Producto", idProducto);

                                    int filasActualizadas =
                                        comandoStock.ExecuteNonQuery();

                                    if (filasActualizadas != 1)
                                    {
                                        throw new Exception(
                                            "Stock insuficiente para el producto: " +
                                            fila.Cells["Producto"].Value);
                                    }
                                }

                            }

                            // 3. Confirmar todos los cambios.
                            transaccion.Commit();
                        }
                        catch
                        {
                            // Si algo falla, cancelar toda la venta.
                            transaccion.Rollback();
                            throw;
                        }
                    }

                    MessageBox.Show("¡Venta registrada correctamente!");

                    MostrarFactura();

                    dgvDetalle.Rows.Clear();
                    CalcularTotales();
                    CargarProductos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Error al registrar la venta: " + ex.Message);
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


            factura.AppendLine(
                "Subtotal sin IVA: $" + subtotalFactura.ToString("N2"));

            factura.AppendLine(
                "IVA incluido (13%): $" + impuestoFactura.ToString("N2"));

            factura.AppendLine(
                "Descuento: $" + descuentoFactura.ToString("N2"));

            factura.AppendLine("------------------------------");

            factura.AppendLine(
                "TOTAL: $" + totalFactura.ToString("N2"));


            factura.AppendLine("------------------------------");
            factura.AppendLine("Gracias por su compra.");

            MessageBox.Show(factura.ToString(), "Comprobante de venta");
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (dgvDetalle.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Seleccione un producto para eliminar.");
                return;
            }

            // Obtenemos la fila seleccionada.
            DataGridViewRow filaSeleccionada =
                dgvDetalle.SelectedRows[0];

            // Confirmamos que el usuario quiera eliminarla.
            DialogResult respuesta = MessageBox.Show(
                "¿Desea quitar este producto de la factura?",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (respuesta == DialogResult.Yes)
            {
                // Quitamos el producto del carrito.
                dgvDetalle.Rows.Remove(filaSeleccionada);

                // Recalculamos subtotal, IVA y total.
                CalcularTotales();
            }
        }
    }
}
