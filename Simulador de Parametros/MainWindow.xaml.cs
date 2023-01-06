using Microsoft.Win32;
using ScottPlot;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Simulador_de_Parametros
{
    /*
     * REQUISITOS ARCHIVO CSV: DEMILITADOR ';'
     * NOMBRES DE COLUMNAS
     * - "CodComercio" = Codigo de comercio
     * - "MCC" = Codigo de Rubro (no se recomienda analizar por rubro ya que puede tomar mucho tiempo.
     * - "Tarjeta" = Numero de Tarjeta o Numero de Tarjeta Enciptada
     * - "MontoFac" = Monto de Transaccion
     * - "FechaTrx" = Fecha de Transaccion
     * - "HoraTrx" = Hora de Transaccion
     * - "indicador_fraude" = Indicador de Fraude, F corresponde a Fraude
     * - "Pais" = Pais de origen de Transaccion *----Sin tilde----*
     * - "codrpta" = Codigo de respuesta "F": fraude
     */
    public partial class MainWindow : Window
    {
        Crosshair Crosshair;
        Crosshair Crosshair2;
        Crosshair Crosshair3;
        Crosshair Crosshair4;
        string filename;
        //Variables X
        int totalRows = 0;
        double minMonto = 0;
        double maxMonto = 100000;
        int montoStep = 100;
        int cantDatos = 0;


        //Variables
        float valorUSD = 978;
        DataTable dt = new DataTable();


        //Variales de Conteo
        double montoTotal = 0;
        double maxMontoMuestra = 0;
        double maxTrxDiaria = 0;
        double montoRecomendado = 0;
        double trxRecomendado = 0;
        double bestDiferenciaMonto = 0;
        double bestDiferencia = 100;
        double lastDiferencia = 0;
        double montoTotalRechazado = 0;
        double montoRechazadoFraude = 0;
        double montoRechazadoNoFraude = 0;
        double denegadoTrxFraude = 0;
        double denegadoTrxNoFraude = 0;
        double denegadoTrxTotal = 0;
        double cantDatosCopy = 0;

        double montoRow = 0;
        double montoRowDiario = 0;
        double cantTrxdia = 0;
        double montoRow1 = 0;
        int prcAnalisis = 0;


        //Variables de Promedio
        double lastMontoPromedio = 100;
        List<String> mcc_lista = new List<String>();
        List<String> comercio_lista = new List<String>();
        //Variales para Graficos
        List<double> list_Monto = new List<double>();
        List<double> list_MontoDenegadoTotal = new List<double>();
        List<double> list_MontoDenegadoVenta = new List<double>();
        List<double> list_MontoDenegadoFraude = new List<double>();
        List<double> list_prcDenegado = new List<double>();
        List<double> list_prcDenegadoF = new List<double>();
        List<double> list_prcDenegadoV = new List<double>();
        List<double> list_prcDenegadoFTotal = new List<double>();
        List<double> list_prcDenegadoVTotal = new List<double>();

        List<double> list_ClientesTotal = new List<double>();
        List<double> list_ClientesAfectados = new List<double>();
        List<double> list_TrxTotal = new List<double>();
        List<double> list_TrxTotalDenegadas = new List<double>();

        //variales para conteo
        List<string> clientesTotal = new List<string>();
        List<string> clienteAfectado = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            Application.Current.MainWindow.WindowState = WindowState.Maximized;
            Crosshair = sim_plot.Plot.AddCrosshair(0, 0);
            Crosshair2 = sim_plot2.Plot.AddCrosshair(0, 0);
            Crosshair3 = sim_plot3.Plot.AddCrosshair(0, 0);
            Crosshair4 = sim_plot4.Plot.AddCrosshair(0, 0);
            sim_plot.Refresh();
            sim_plot2.Refresh();
            sim_plot3.Refresh();
            sim_plot4.Refresh();

            GenerarQuery(false, "4511", "20221101", "20221231", 954);
        }

        private async void CargarArchivo(string filepath)
        {
            label_FileSelected.Content = "Analizando Archivo... Espere";
            btn_selectFile.IsEnabled = false;
            btn_selectFile.Visibility = Visibility.Collapsed;
            progress_file.Visibility = Visibility.Visible;

            var progreso = new Progress<int>(value =>
            {
                label_FileSelected.Content = "Analizando datos: " + value + "%";
                progress_file.Value = value;
            }
            );

            await Task.Run(() =>
            {
                LeerCSV(filepath);
                OrdenarDT();
                ConteoGeneral();
                AnalizarCSV(progreso);
            });
            //progress_file.Visibility = Visibility.Collapsed;
            //btn_selectFile.Visibility = Visibility.Visible;

            label_FileSelected.Content = "Archivo Seleccionado: " + filename;
            txtbox_MccComercio.IsEnabled = true;
            btn_MontoPromedio.IsEnabled = true;
            btn_Simulador.IsEnabled = true;
            txtBox_USDstep.IsEnabled = true;
            txtBox_cant_Trx.IsEnabled = true;
            txtBox_max_Trx.IsEnabled = true;
            btn_selectFile.IsEnabled = true;
            rd_btn_MCC.IsEnabled = true;
            rd_btn_Negocio.IsEnabled = true;

            //crea archivo para analisis de debug
            /*
            XLWorkbook wb = new XLWorkbook();
            wb.Worksheets.Add(dt, "test");
            wb.SaveAs("C:\\Users\\PVGL1262\\source\\repos\\Simulador y MontoPromedio\\Simulador y MontoPromedio\\archivoexcel.xlsx");
            */
        }



        private void LeerCSV(string filepath)
        {
            string strFilePath = filepath;
            char csvDelimiter = ';';

            using (StreamReader sr = new StreamReader(strFilePath))
            {
                string[] headers = sr.ReadLine().Split(csvDelimiter);
                foreach (string header in headers)
                {
                    try
                    {
                        dt.Columns.Add(header);
                    }
                    catch { }
                }
                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split(csvDelimiter);
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i];
                    }
                    dt.Rows.Add(dr);
                }
            }
        }

        private void ConteoGeneral()
        {
            dt.Columns.Add("TrxID"); //Implementar id para no leer datos duplicados

            cantDatos = 0;
            foreach (DataRow dr in dt.Rows)
            {
                //Id para cada columna
                cantDatos++;
                dr["TrxID"] = cantDatos;
                if (!mcc_lista.Contains(dr["MCC"].ToString())) mcc_lista.Add(dr["MCC"].ToString());//lista MCC                
                if (!comercio_lista.Contains(dr["CodComercio"].ToString())) comercio_lista.Add(dr["CodComercio"].ToString());//Lista Comercios

                string montoChar = dr["MontoFac"].ToString();
                montoChar = montoChar.Replace("$", "");
                montoChar = montoChar.Replace(",", "");
                montoChar = montoChar.Replace(".", ",");
                double montoConvert = double.Parse(montoChar);
                dr["MontoDouble"] = montoConvert;
            }
        }

        void AnalizarCSV(IProgress<int> progreso)
        {
            double montoAcomuladoComercio = 0;
            double cantTrxComercio = 0;
            double montoAcomuladoMCC = 0;
            double cantTrxMCC = 0;

            int rowsAnalizadas = 0;
            foreach (DataRow datarow1 in dt.Rows.Cast<DataRow>().Skip(rowsAnalizadas)) //llena las nuevas columnas de resumen de transaccion del dia y monto diario
            {
                //Analisis para Comercio
                if (datarow1["CantTrxDiaComercio"].ToString() == "")
                {
                    string tarjeta = datarow1["Tarjeta"].ToString(); //Tarjeta a analizar
                    string comercio = "";
                    string mcc = "";
                    string fecha = "";
                    foreach (DataRow datarow2 in dt.Rows.Cast<DataRow>().Skip(rowsAnalizadas))
                    {
                        if (datarow2["cantTrxDiaComercio"].ToString() == "")
                        {
                            string tarjeta2 = datarow2["Tarjeta"].ToString();
                            string comercio2 = datarow2["CodComercio"].ToString();
                            string fecha2 = datarow2["FechaTrx"].ToString();
                            string cantTrx2 = datarow2["cantTrxDiaComercio"].ToString();
                            string mcc2 = datarow2["MCC"].ToString(); ;

                            if (cantTrx2 == "" && tarjeta == tarjeta2) //si columnas nueva no tiene datos y tarjeta corresponde a tarjeta original, se analiza
                            {
                                if (comercio != comercio2 || fecha != fecha2) //Comercio - Si el comercio es nuevo y cambia de dia se resetean datos diarios
                                {
                                    cantTrxComercio = 1;
                                    montoAcomuladoComercio = double.Parse(datarow2["montoDouble"].ToString());
                                }
                                else //Si se mantiene comercio y fecha, se suma lo del dia anterior
                                {
                                    cantTrxComercio++;
                                    montoAcomuladoComercio += double.Parse(datarow2["montoDouble"].ToString());
                                }
                                if (comercio != comercio2 || fecha != fecha2) //MCC - Si el comercio es nuevo y cambia de dia se resetean datos diarios
                                {
                                    cantTrxMCC = 1;
                                    montoAcomuladoMCC = double.Parse(datarow2["montoDouble"].ToString());
                                }
                                else //MCC- Si se mantiene comercio y fecha, se suma lo del dia anterior
                                {
                                    cantTrxMCC++;
                                    montoAcomuladoMCC += double.Parse(datarow2["montoDouble"].ToString());
                                }
                                datarow2["CantTrxDiaComercio"] = cantTrxComercio;
                                datarow2["MontoAcomuladoDiaComercio"] = montoAcomuladoComercio;
                                datarow2["CantTrxDiaMCC"] = cantTrxComercio;
                                datarow2["MontoAcomuladoDiaMCC"] = montoAcomuladoComercio;
                                comercio = comercio2;
                                mcc = mcc2;
                                fecha = fecha2;
                                rowsAnalizadas++;
                                prcAnalisis = (rowsAnalizadas * 100) / (cantDatos);
                                progreso.Report(prcAnalisis);
                            }
                            if (cantTrx2 == "" && tarjeta != tarjeta2)
                            {
                                break;
                            }
                        }
                    }
                }

            }
        }

        //query
        private string GenerarQuery(bool mcc_comercio, string id, string fecha_ini, string fecha_fin, int precioDolar)
        {
            string query = "select * from tabla \n";
            query += "CASE IF PAIS LIKE CL THEN MONTO = (MONTO/"+ precioDolar+ ") \n";
            string query_where = "WHERE ";
            if (mcc_comercio)
            {
                query_where += "id_comercio = ";
            }
            else
            {
                query_where += "mcc = ";
            }

            query += query_where + id;
            query += "\n" + "and fecha_ini = " + fecha_ini + " and fecha_fin = " + fecha_fin + "\n";
            query += "ORDER BY HoraTrx, FechaTrx, CodComercio, Tarjeta";


            Clipboard.SetText(query);
            MessageBox.Show("Query copiada al portapapeles exitosamente", "Mensaje", MessageBoxButton.OK, MessageBoxImage.Information);

            return query;

        }

        void OrdenarDT()
        {
            dt = new DataView(dt, "", "HoraTrx", DataViewRowState.CurrentRows).ToTable();
            dt = new DataView(dt, "", "FechaTrx", DataViewRowState.CurrentRows).ToTable();
            dt = new DataView(dt, "", "CodComercio", DataViewRowState.CurrentRows).ToTable();
            dt = new DataView(dt, "", "Tarjeta", DataViewRowState.CurrentRows).ToTable();

            dt.Columns.Add("CantTrxDiaMCC");
            dt.Columns.Add("MontoAcomuladoDiaMCC");
            dt.Columns.Add("CantTrxDiaComercio");
            dt.Columns.Add("MontoAcomuladoDiaComercio");
            DataColumn montoFloat = new DataColumn("montoDouble");
            montoFloat.DataType = System.Type.GetType("System.Double");
            dt.Columns.Add(montoFloat);
        }

        void NuevoDataGrid() //para calcular monto promedio
        {
            DataTable copyDataTable;
            cantDatosCopy = 0;
            copyDataTable = dt.Copy();
            List<DataRow> toDelete = new List<DataRow>();

            if (rd_btn_MCC.IsChecked == true)
            {
                foreach (DataRow dr in copyDataTable.Rows)
                {
                    if (dr["MCC"].ToString() != txtbox_MccComercio.Text.ToString())
                    {
                        toDelete.Add(dr); //se agregan a una lista todos los datos que no correspondan al rubro
                    }
                    else cantDatosCopy++;
                }
                foreach (DataRow dr in toDelete)
                {
                    copyDataTable.Rows.Remove(dr); // se eliminan los datos de la lista anterior
                }
                CalcularMontoPromedio(copyDataTable); //se manda el datatable al calculo de monto promedio
            }
            if (rd_btn_Negocio.IsChecked == true)
            {
                foreach (DataRow dr in copyDataTable.Rows)
                {
                    if (dr["CodComercio"].ToString() != txtbox_MccComercio.Text.ToString())
                    {
                        toDelete.Add(dr); //se agregan a una lista todos los datos que no correspondan al rubro
                    }
                    else cantDatosCopy++;
                }
                foreach (DataRow dr in toDelete)
                {
                    copyDataTable.Rows.Remove(dr); // se eliminan los datos de la lista anterior
                }
                CalcularMontoPromedio(copyDataTable); //se manda el datatable al calculo de monto promedio
            }
        }

        double ClpCheck(double monto, string pais)
        {
            if (pais == "CL") monto /= valorUSD;
            return monto;
        }

        void CalcularMontoPromedio(DataTable ndt) //calcula monto promedio evitando los Outliers
        {
            ndt = new DataView(ndt, "", "MontoDouble", DataViewRowState.CurrentRows).ToTable();

            double prcCorte = 0;
            double cantDatosPromedio = 0;
            double montoTotalPromedio = 0;
            double promedioMCC = 100;
            double diferenciaPromedio = 100;

            bool loop = false;
            while (diferenciaPromedio > 5)
            {
                prcCorte += 1f;
                double cantCorte = (cantDatosCopy * prcCorte) / 100;
                double minCorte = cantCorte;
                double maxCorte = cantDatosCopy - cantCorte;
                int z = 0;

                foreach (DataRow dr in ndt.Rows)
                {
                    if (z > minCorte && z < maxCorte)
                    {
                        cantDatosPromedio++;
                        montoRow1 = ClpCheck(double.Parse(dr["MontoDouble"].ToString()), dr["Pais"].ToString());
                        montoTotalPromedio += (float)montoRow1;
                    }
                    z++;
                }
                promedioMCC = montoTotalPromedio / cantDatosPromedio;
                if (loop)
                {
                    diferenciaPromedio = (promedioMCC * 100) / lastMontoPromedio;
                    diferenciaPromedio = 100 - diferenciaPromedio;
                }
                loop = true;
                lastMontoPromedio = promedioMCC;
            }

            label_montoProm.Content = "Promedio: USD$" + promedioMCC;

        }


        private void ResetearListas() //se resetean para cada simulacion
        {
            list_Monto.Clear();
            list_prcDenegado.Clear();
            list_prcDenegadoF.Clear();
            list_prcDenegadoV.Clear();
            list_prcDenegadoFTotal.Clear();
            list_prcDenegadoVTotal.Clear();

            list_ClientesAfectados.Clear();
            list_TrxTotal.Clear();
            list_TrxTotalDenegadas.Clear();
            list_ClientesTotal.Clear();

            list_MontoDenegadoTotal.Clear();
            list_MontoDenegadoVenta.Clear();
            list_MontoDenegadoFraude.Clear();
        }

        private void ResetearVariables() //se resetean en cada Monto
        {
            totalRows = 0;
            denegadoTrxTotal = 0;
            denegadoTrxFraude = 0;
            denegadoTrxNoFraude = 0;
            montoTotalRechazado = 0;
            montoRechazadoFraude = 0;
            montoRechazadoNoFraude = 0;
            clienteAfectado.Clear();
            clientesTotal.Clear();
        }

        /*
         * 
         * var progreso = new Progress<int>(value =>
            {
                label_FileSelected.Content = "Analizando datos: " + value + "%";
                progress_file.Value = value;
            }
            );

            await Task.Run(() =>
            {
                LeerCSV(filepath);
                OrdenarDT();
                ConteoGeneral();
                AnalizarCSV(progreso);
            });
         */


        void Simulador(string stringCheck, string MCC_Comercio) //Sirve para definir si se simula con MCC o Codigo de Comercio, y si los datos de ingreso se elijen automaticamente
        {
            ResetearListas();
            CalcularMaxMonto(stringCheck, MCC_Comercio); //Para delimitar el maximo del simulador
            montoRecomendado = (int)(maxMontoMuestra / 12);
            trxRecomendado = maxTrxDiaria;
            bool autoVariables = false;
            if (cbox_autoVariables.IsChecked == true)
            {
                autoVariables = true; //si el checkbox esta checked se autodeterminan las variables
                txtBox_cant_Trx.Text = maxTrxDiaria.ToString();
                txtBox_USDstep.Text = montoRecomendado.ToString();
            }
            montoStep = Int32.Parse(txtBox_USDstep.Text.ToString());
            int maxTrx = Int32.Parse(txtBox_cant_Trx.Text.ToString());
            for (double montoTest = minMonto; montoTest <= maxMonto; montoTest += montoStep)
            {
                ResetearVariables();

                bool boolBreak;
                bool filtroEmisor = false;
                if (!autoVariables)
                {
                    boolBreak = BaseSimulador(montoTest, maxTrx, stringCheck, MCC_Comercio, filtroEmisor);
                }
                else
                {
                    boolBreak = BaseSimulador(montoTest, maxTrx, stringCheck, MCC_Comercio, filtroEmisor);

                }
                if (boolBreak || montoTest > Int32.Parse(txtBox_max_Trx.Text.ToString())) break;
            }
            ResumenSimulador(stringCheck, MCC_Comercio); //Calcula los datos de Resumen de muestra

        }

        private void CalcularMaxMonto(string str_Check, string MCC_Comercio)
        {
            maxMontoMuestra = 0;
            maxTrxDiaria = 0;
            foreach (DataRow dr in dt.Rows)
            {
                if (dr[str_Check].ToString() == txtbox_MccComercio.Text.ToString())
                {
                    double montoActual = ClpCheck(double.Parse(dr["MontoDouble"].ToString()), dr["Pais"].ToString());
                    if (maxMontoMuestra < montoActual) maxMontoMuestra = montoActual;
                    if (maxTrxDiaria < double.Parse(dr["CantTrxDia" + MCC_Comercio].ToString())) maxTrxDiaria = double.Parse(dr["CantTrxDia" + MCC_Comercio].ToString());
                }
            }
        }
        private void ResumenSimulador(string str_Check, string MCC_Comercio) //Calcula monto max, minimo, etc.
        {
            int cantDatosResume = 0;
            double minMonto = 0;
            double maxMonto = 0;
            double minMontoVenta = 0;
            double maxMontoVenta = 0;
            double minMontoFraude = 0;
            double maxMontoFraude = 0;
            double maxTrxVenta = 0;
            double maxTrxFraude = 0;

            double totalMonto = 0;
            bool primerMonto = true;
            bool primerMontoFraude = true;
            bool primerMontoVenta = true;
            foreach (DataRow dr in dt.Rows)
            {
                bool aprobadaBool = false;
                string trxAprobada = dr["codrpta"].ToString(); //gregar filro para que solo se lean los datos que fueron aprobados.
                if (trxAprobada == "0" || trxAprobada == "00" || trxAprobada == "000")
                {
                    aprobadaBool = true;
                }
                else aprobadaBool = false;
                if (dr[str_Check].ToString() == txtbox_MccComercio.Text.ToString() && aprobadaBool)
                {
                    cantDatosResume++;
                    double montoActual = ClpCheck(double.Parse(dr["MontoDouble"].ToString()), dr["Pais"].ToString());
                    double trxActual = double.Parse(dr["CantTrxDia" + MCC_Comercio].ToString());

                    if (primerMontoFraude && dr["indicador_fraude"].ToString() == "F")//solo corre en la primera vuelta
                    {
                        minMontoFraude = montoActual;
                        maxMontoFraude = montoActual;
                        primerMontoFraude = false;
                        maxTrxFraude = trxActual;

                    }
                    if (primerMontoVenta && dr["indicador_fraude"].ToString() != "F")//solo corre en la primera vuelta
                    {
                        minMontoVenta = montoActual;
                        maxMontoVenta = montoActual;
                        primerMontoVenta = false;
                        maxTrxVenta = trxActual;
                    }
                    if (primerMonto) //solo corre en la primera vuelta
                    {
                        minMonto = montoActual;
                        maxMonto = montoActual;
                        primerMonto = false;
                    }
                    if (minMonto > montoActual) minMonto = montoActual;
                    if (maxMonto < montoActual) maxMonto = montoActual;
                    totalMonto += montoActual;
                    if (dr["indicador_fraude"].ToString() == "F")
                    {
                        if (minMontoFraude > montoActual) minMontoFraude = montoActual;
                        if (maxMontoFraude < montoActual) maxMontoFraude = montoActual;
                        if (maxTrxFraude < trxActual) maxTrxFraude = trxActual;

                    }
                    else
                    {
                        if (minMontoVenta > montoActual) minMontoVenta = montoActual;
                        if (maxMontoVenta < montoActual) maxMontoVenta = montoActual;
                        if (maxTrxVenta < trxActual) maxTrxVenta = trxActual;
                    }
                }

            }
            if (minMonto == 0)
            {
                Console.WriteLine("test");
            }
            int cantClientes = clientesTotal.Count();
            double trx_prom = cantDatosResume / cantClientes;
            double monto_prom = totalMonto / cantDatosResume;
            lbl_resume_cantTrx.Content = cantDatosResume.ToString();
            lbl_resume_cantCli.Content = clientesTotal.Count().ToString();
            lbl_resume_trxProm.Content = trx_prom.ToString();
            lbl_resume_maxTtrxVenta.Content = maxTrxVenta.ToString();
            lbl_resume_maxTrxFraude.Content = maxTrxFraude.ToString();
            lbl_resume_montoProm.Content = "USD $" + monto_prom.ToString("n0", new CultureInfo("es-ES"));
            lbl_resume_montoMin.Content = "USD $" + minMonto.ToString("n0", new CultureInfo("es-ES")); //arreglar para que muestre decimales
            lbl_resume_montoMax.Content = "USD $" + maxMonto.ToString("n0", new CultureInfo("es-ES"));
            lbl_resume_montoMinVenta.Content = "USD $" + minMontoVenta.ToString("n0", new CultureInfo("es-ES"));
            lbl_resume_montoMaxVenta.Content = "USD $" + maxMontoVenta.ToString("n0", new CultureInfo("es-ES"));
            lbl_resume_montoMinFraude.Content = "USD $" + minMontoFraude.ToString("n0", new CultureInfo("es-ES"));
            lbl_resume_montoMaxFraude.Content = "USD $" + maxMontoFraude.ToString("n0", new CultureInfo("es-ES"));
            lbl_montoRecomendado.Content = "USD $" + bestDiferenciaMonto.ToString("n0", new CultureInfo("es-ES"));
        }

        private bool BaseSimulador(double montoTest, int maxTrx, string str_Check, string MCC_Comercio, bool filtroEmisor)
        {
            string emisor = "";
            foreach (DataRow dr in dt.Rows)
            {
                bool aprobadaBool = false;
                string trxAprobada = dr["codrpta"].ToString(); //gregar filro para que solo se lean los datos que fueron aprobados.
                if (trxAprobada == "0" || trxAprobada == "00" || trxAprobada == "000")
                {
                    aprobadaBool = true;
                }
                else aprobadaBool = false;
                if (dr[str_Check].ToString() == txtbox_MccComercio.Text.ToString() && aprobadaBool)
                {
                    totalRows++;
                    //Contar cant de Clientes totales
                    if (!clientesTotal.Contains(dr["Tarjeta"].ToString())) clientesTotal.Add(dr["Tarjeta"].ToString()); //sacar del loop pq solo se encesita una vez

                    montoRow = ClpCheck(double.Parse(dr["MontoDouble"].ToString()), dr["Pais"].ToString());
                    montoRowDiario = ClpCheck(double.Parse(dr["MontoAcomuladoDia" + MCC_Comercio].ToString()), dr["Pais"].ToString());
                    cantTrxdia = double.Parse(dr["CantTrxDia" + MCC_Comercio].ToString());

                    if (montoRowDiario > montoTest || cantTrxdia > maxTrx)
                    {
                        denegadoTrxTotal++;
                        montoTotalRechazado += montoRow;
                        //Contar cant de Clientes afectados
                        if (!clienteAfectado.Contains(dr["Tarjeta"].ToString())) clienteAfectado.Add(dr["Tarjeta"].ToString());
                        if (dr["indicador_fraude"].ToString() == "F")
                        {
                            denegadoTrxFraude++;
                            montoRechazadoFraude += montoRow;
                        }
                        else
                        {
                            denegadoTrxNoFraude++;
                            montoRechazadoNoFraude += montoRow;
                        }
                    }
                }
            }
            //Calculos de resultados
            double trxPromedio = montoTotal / totalRows;
            double prcDenegado = ((denegadoTrxTotal * 100) / totalRows);
            double prcDenegadoF = 0;
            double prcDenegadoNF = 0;
            double prcDenegadoFTotal = 0;
            double prcDenegadoNFTotal = 0;

            if (denegadoTrxTotal == 0)
            {
                prcDenegadoF = 0;
                prcDenegadoNF = 0;
                prcDenegadoFTotal = 0;
                prcDenegadoNFTotal = 0;
            }
            else
            {
                prcDenegadoF = ((denegadoTrxFraude * 100) / denegadoTrxTotal);
                prcDenegadoNF = ((denegadoTrxNoFraude * 100) / denegadoTrxTotal);
                prcDenegadoFTotal = ((denegadoTrxFraude * 100) / totalRows);
                prcDenegadoNFTotal = ((denegadoTrxNoFraude * 100) / totalRows);
            }
            //Recomendar mejor % de ventas contra fraudes
            lastDiferencia = prcDenegadoNF - prcDenegadoF;
            if (lastDiferencia < bestDiferencia)
            {
                bestDiferenciaMonto = montoTest;
                bestDiferencia = lastDiferencia;
            }
            //guardar datos en listas para creacion de grafico 1.
            list_Monto.Add(montoTest);
            list_MontoDenegadoTotal.Add(montoTotalRechazado);
            list_prcDenegado.Add(prcDenegado);
            list_prcDenegadoF.Add(prcDenegadoF);
            list_prcDenegadoV.Add(prcDenegadoNF);
            list_prcDenegadoFTotal.Add(prcDenegadoFTotal);
            list_prcDenegadoVTotal.Add(prcDenegadoNFTotal);

            //Variables para grafico 2
            list_TrxTotal.Add(totalRows);
            list_TrxTotalDenegadas.Add(denegadoTrxTotal);
            list_ClientesTotal.Add(clientesTotal.Count());
            list_ClientesAfectados.Add(clienteAfectado.Count());

            //Grafico 3
            list_MontoDenegadoFraude.Add(montoRechazadoFraude);
            list_MontoDenegadoVenta.Add(montoRechazadoNoFraude);

            if (prcDenegado <= 1 || montoTest > maxMontoMuestra) return true;
            return false;
        }

        private void btn_Simulador_Click(object sender, RoutedEventArgs e)
        {
            bestDiferenciaMonto = 0;
            bestDiferencia = 100;
            string mccCheck = txtbox_MccComercio.Text.ToString();
            if (rd_btn_MCC.IsChecked == true)
            {
                if (mcc_lista.Contains(mccCheck))
                {
                    Simulador("MCC", "MCC");
                    LlenarPlot();
                }
                else MessageBox.Show("No existe MCC en los datos");
            }
            if (rd_btn_Negocio.IsChecked == true)
            {
                if (comercio_lista.Contains(mccCheck))
                {
                    Simulador("CodComercio", "Comercio");
                    LlenarPlot();
                }
                else MessageBox.Show("No existe Comercio en los datos");
            }
        }

        private void LlenarPlot()
        {
            //Grafico 1
            double[] monto = new double[list_Monto.Count()];
            double[] prcDenegado = new double[list_prcDenegado.Count()];
            double[] prcDenegadoF = new double[list_prcDenegadoF.Count()];
            double[] prcDenegadoNF = new double[list_prcDenegadoV.Count()];

            double[] prcDenegadoFTotal = new double[list_prcDenegadoFTotal.Count()];
            double[] prcDenegadoNFTotal = new double[list_prcDenegadoVTotal.Count()];

            //grafico 2
            double[] arr_TrxTotal = new double[list_TrxTotal.Count()];
            double[] arr_TrxDenegadas = new double[list_TrxTotalDenegadas.Count()];
            double[] arr_clientesTotal = new double[list_ClientesTotal.Count()];
            double[] arr_clientesAfec = new double[list_ClientesAfectados.Count()];

            //Grafico 3
            double[] montoDenegadoTotal = new double[list_MontoDenegadoTotal.Count()];
            double[] arr_montoTotalFraude = new double[list_MontoDenegadoFraude.Count()];
            double[] arr_montoTotalVenta = new double[list_MontoDenegadoVenta.Count()];

            //Grafico 1
            for (int i = 0; i < list_Monto.Count(); i++) monto[i] = list_Monto[i];
            for (int i = 0; i < list_prcDenegado.Count(); i++) prcDenegado[i] = list_prcDenegado[i];
            for (int i = 0; i < list_prcDenegadoF.Count(); i++) prcDenegadoF[i] = list_prcDenegadoF[i];
            for (int i = 0; i < list_prcDenegadoV.Count(); i++) prcDenegadoNF[i] = list_prcDenegadoV[i];
            for (int i = 0; i < list_prcDenegadoFTotal.Count(); i++) prcDenegadoFTotal[i] = list_prcDenegadoFTotal[i];
            for (int i = 0; i < list_prcDenegadoVTotal.Count(); i++) prcDenegadoNFTotal[i] = list_prcDenegadoVTotal[i];

            //Grafico 2
            for (int i = 0; i < list_TrxTotal.Count(); i++) arr_TrxTotal[i] = list_TrxTotal[i];
            for (int i = 0; i < list_TrxTotalDenegadas.Count(); i++) arr_TrxDenegadas[i] = list_TrxTotalDenegadas[i];
            for (int i = 0; i < list_ClientesTotal.Count(); i++) arr_clientesTotal[i] = list_ClientesTotal[i];
            for (int i = 0; i < list_ClientesAfectados.Count(); i++) arr_clientesAfec[i] = list_ClientesAfectados[i];

            //Grafico 3
            for (int i = 0; i < list_MontoDenegadoTotal.Count(); i++) montoDenegadoTotal[i] = list_MontoDenegadoTotal[i] / 1000;
            for (int i = 0; i < list_MontoDenegadoFraude.Count(); i++) arr_montoTotalFraude[i] = list_MontoDenegadoFraude[i] / 1000;
            for (int i = 0; i < list_MontoDenegadoVenta.Count(); i++) arr_montoTotalVenta[i] = list_MontoDenegadoVenta[i] / 1000;

            sim_plot.Reset();
            sim_plot2.Reset();
            sim_plot3.Reset();
            sim_plot4.Reset();
            ScottPlot.Alignment legendplace = Alignment.UpperRight;

            //Grafico 1
            sim_plot.Plot.Title("% de Transacciones y Clientes Denegados");
            sim_plot.plt.XLabel("Monto (USD)");
            sim_plot.plt.YLabel("%");
            sim_plot.Plot.AddScatter(monto, prcDenegado, label: "% Denegado");
            sim_plot.Plot.AddScatter(monto, prcDenegadoFTotal, color: System.Drawing.Color.Red, label: "% Fraudes Denegados del Total");
            sim_plot.Plot.AddScatter(monto, prcDenegadoNFTotal, color: System.Drawing.Color.Green, label: "% Ventas Denegadas del Total");
            //sim_plot.Plot.AddFill(monto, prcDenegado);
            Crosshair = sim_plot.Plot.AddCrosshair(0, 0);

            sim_plot.plt.Legend();
            sim_plot.Plot.Legend(location: legendplace);



            //Grafico 2
            sim_plot2.Plot.Title("Total de Transacciones y Clientes Denegados");
            sim_plot2.plt.XLabel("Monto (USD)");
            sim_plot2.plt.YLabel("Cantidad");
            sim_plot2.Plot.AddScatter(monto, arr_TrxTotal, label: "Transacciones Totales");
            sim_plot2.Plot.AddScatter(monto, arr_TrxDenegadas, color: System.Drawing.Color.Cyan, label: "Transacciones Denegadas");
            sim_plot2.Plot.AddScatter(monto, arr_clientesTotal, color: System.Drawing.Color.SteelBlue, label: "Clientes Totales");
            sim_plot2.Plot.AddScatter(monto, arr_clientesAfec, color: System.Drawing.Color.Teal, label: "Clientes Afectados");

            Crosshair2 = sim_plot2.Plot.AddCrosshair(0, 0);
            sim_plot2.plt.Legend();
            sim_plot2.Plot.Legend(location: legendplace);

            //Grafico 3
            sim_plot3.Plot.Title("USD denegado por Monto de Parametro");
            sim_plot3.plt.XLabel("Monto (USD)");
            sim_plot3.plt.YLabel("Total USD Denegado (Miles)");
            sim_plot3.Plot.AddScatter(monto, montoDenegadoTotal, label: "USD Total Denegado (miles)");
            sim_plot3.Plot.AddScatter(monto, arr_montoTotalFraude, color: System.Drawing.Color.Red, label: "USD Total Denegado Fraude (miles)");
            sim_plot3.Plot.AddScatter(monto, arr_montoTotalVenta, color: System.Drawing.Color.Green, label: "USD Total Denegado Venta (miles)");
            sim_plot3.plt.Legend();
            sim_plot3.Plot.Legend(location: legendplace);
            Crosshair3 = sim_plot3.Plot.AddCrosshair(0, 0);

            //Grafico 4
            sim_plot4.Plot.Title("% Ventas contra Fraudes");
            sim_plot4.plt.XLabel("Monto (USD)");
            sim_plot4.plt.YLabel("%");
            sim_plot4.Plot.AddScatter(monto, prcDenegadoF, label: "% Fraudes Denegados", color: System.Drawing.Color.Red);
            sim_plot4.Plot.AddScatter(monto, prcDenegadoNF, label: "% Ventas Denegadas", color: System.Drawing.Color.Green);
            //sim_plot4.plt.Legend();
            //sim_plot4.Plot.Legend(location: legendplace);
            Crosshair4 = sim_plot4.Plot.AddCrosshair(0, 0);


            sim_plot.Refresh();
            sim_plot2.Refresh();
            sim_plot3.Refresh();
            sim_plot4.Refresh();
        }


        //---------------------------------------------------Metodos de la Vista-------------------------------------------------\\

        private void btn_selectFile_Click(object sender, RoutedEventArgs e)//ok
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "csv files (*.csv)|*.csv|All files (*.*)|*.*";
            openFileDialog.Title = "Selecciona archivo csv";
            string filepath = "";
            filename = "";
            if (openFileDialog.ShowDialog() == true)
            {
                filepath = openFileDialog.FileName;
                filename = System.IO.Path.GetFileName(openFileDialog.FileName);
            }
            if (filepath != "")
            {
                CargarArchivo(filepath);
            }
        }

        private void rd_btn_MCC_Checked(object sender, RoutedEventArgs e)//ok
        {
            if (rd_btn_Negocio.IsChecked == true) rd_btn_Negocio.IsChecked = false;

        }

        private void rd_btn_Negocio_Checked(object sender, RoutedEventArgs e)//ok
        {
            if (rd_btn_MCC.IsChecked == true) rd_btn_MCC.IsChecked = false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)//ok
        {
            TextBox textBox = sender as TextBox;
            int iValue = -1;

            if (Int32.TryParse(textBox.Text, out iValue) == false)
            {
                TextChange textChange = e.Changes.ElementAt<TextChange>(0);
                int iAddedLength = textChange.AddedLength;
                int iOffset = textChange.Offset;

                textBox.Text = textBox.Text.Remove(iOffset, iAddedLength);
            }
        }

        private void sim_plot_MouseMove(object sender, MouseEventArgs e)//ok
        {
            int pixelX = (int)e.MouseDevice.GetPosition(sim_plot).X;
            int pixelY = (int)e.MouseDevice.GetPosition(sim_plot).Y;

            (double coordinateX, double coordinateY) = sim_plot.GetMouseCoordinates();

            ActualizarCrossHair(coordinateX, coordinateY);

        }

        private void btn_MontoPromedio_Click(object sender, RoutedEventArgs e)
        {
            NuevoDataGrid();
        }

        private void sim_plot_MouseEnter(object sender, MouseEventArgs e)
        {
            Crosshair.IsVisible = true;

        }

        private void sim_plot_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void sim_plot2_MouseMove(object sender, MouseEventArgs e)
        {
            int pixelX = (int)e.MouseDevice.GetPosition(sim_plot2).X;
            int pixelY = (int)e.MouseDevice.GetPosition(sim_plot2).Y;

            (double coordinateX, double coordinateY) = sim_plot2.GetMouseCoordinates();

            ActualizarCrossHair(coordinateX, coordinateY);
        }

        private void sim_plot2_MouseEnter(object sender, MouseEventArgs e)
        {
            Crosshair2.IsVisible = true;
        }

        private void sim_plot2_MouseLeave(object sender, MouseEventArgs e)
        {
        }

        private void sim_plot3_MouseMove(object sender, MouseEventArgs e)
        {
            int pixelX = (int)e.MouseDevice.GetPosition(sim_plot3).X;
            int pixelY = (int)e.MouseDevice.GetPosition(sim_plot3).Y;

            (double coordinateX, double coordinateY) = sim_plot3.GetMouseCoordinates();

            ActualizarCrossHair(coordinateX, coordinateY);
        }

        private void sim_plot3_MouseEnter(object sender, MouseEventArgs e)
        {
            Crosshair3.IsVisible = true;
        }

        private void sim_plot3_MouseLeave(object sender, MouseEventArgs e)
        {
        }
        private void sim_plot4_MouseMove(object sender, MouseEventArgs e)
        {
            int pixelX = (int)e.MouseDevice.GetPosition(sim_plot4).X;
            int pixelY = (int)e.MouseDevice.GetPosition(sim_plot4).Y;

            (double coordinateX, double coordinateY) = sim_plot4.GetMouseCoordinates();

            ActualizarCrossHair(coordinateX, coordinateY);
        }
        private void ActualizarCrossHair(double coordinateX, double coordinateY)
        {
            Crosshair.X = coordinateX;
            Crosshair.Y = coordinateY;
            sim_plot.Refresh();

            Crosshair2.X = coordinateX;
            Crosshair2.Y = coordinateY;
            sim_plot2.Refresh();

            Crosshair3.X = coordinateX;
            Crosshair3.Y = coordinateY;
            sim_plot3.Refresh();

            Crosshair4.X = coordinateX;
            Crosshair4.Y = coordinateY;
            sim_plot4.Refresh();
        }

        private void cbox_autoVariables_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        private void cbox_autoVariables_Checked(object sender, RoutedEventArgs e)
        {
            if (cbox_autoVariables.IsChecked == true)
            {
                txtBox_cant_Trx.IsEnabled = false;
                txtBox_USDstep.IsEnabled = false;
            }
            else
            {
                txtBox_cant_Trx.IsEnabled = true;
                txtBox_USDstep.IsEnabled = true;
            }
        }
    }
}
