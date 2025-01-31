﻿using Dapper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TipOdFreqAdmin
{
    public partial class Form1 : Form
    {
        Timer nT = new Timer();
        public Form1()
        {
            InitializeComponent();

            Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.btnQuery.Click += BtnQuery_Click;
            this.btnExport.Click += BtnExport_Click;
            this.btnUpdatePathExport.Click += BtnUpdatePathExport_Click;
            this.cbPart.KeyDown += CbPart_KeyDown;

            #region Get part
            using (var connection = GlobalVariables.GetDbConnection())
            {
                var para = new DynamicParameters();
                para.Add("@part", "All");
                var _part = connection.Query<tblTipOdFreqModel>("sp_GetParts", para, commandType: CommandType.StoredProcedure).ToList();

                if (_part.Count > 0)
                {
                    foreach (var item in _part)
                    {
                        if (cbPart.InvokeRequired)
                        {
                            cbPart.Invoke(new Action(() =>
                            {
                                cbPart.Items.Add(item.ItemNumber);
                            }));
                        }
                        else
                        {
                            cbPart.Items.Add(item.ItemNumber);
                        }
                    }

                    if (cbPart.InvokeRequired)
                    {
                        cbPart.Invoke(new Action(() =>
                        {
                            cbPart.Items.Add("All");
                        }));
                    }
                    else
                    {
                        cbPart.Items.Add("All");
                    }
                }
            }
            #endregion

            nT.Interval = 100;
            nT.Enabled = true;
            nT.Tick += NT_Tick;
        }

        private void CbPart_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ComboBox _cb = (ComboBox)sender;

                using (var connection = GlobalVariables.GetDbConnection())
                {
                    var para = new DynamicParameters();
                    para.Add("@part", _cb.Text);
                    var _part = connection.Query<tblTipOdFreqModel>("sp_GetParts", para, commandType: CommandType.StoredProcedure).ToList();

                    if (_part.Count > 0)
                    {
                        _cb.Items.Clear();

                        foreach (var item in _part)
                        {
                            if (cbPart.InvokeRequired)
                            {
                                cbPart.Invoke(new Action(() =>
                                {
                                    cbPart.Items.Add(item.ItemNumber);
                                }));
                            }
                            else
                            {
                                cbPart.Items.Add(item.ItemNumber);
                            }
                        }

                        if (cbPart.InvokeRequired)
                        {
                            cbPart.Invoke(new Action(() =>
                            {
                                cbPart.Items.Add("All");
                            }));
                        }
                        else
                        {
                            cbPart.Items.Add("All");
                        }
                    }
                }
            }
        }

        private void BtnUpdatePathExport_Click(object sender, EventArgs e)
        {
            try
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    DialogResult result = fbd.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        GlobalVariables.PathExport = fbd.SelectedPath;

                        Properties.Settings.Default.PathExport = GlobalVariables.PathExport;
                        Properties.Settings.Default.Save();
                    }
                }
            }
            catch
            {

            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(GlobalVariables.PathExport))
                {
                    using (var fbd = new FolderBrowserDialog())
                    {
                        DialogResult result = fbd.ShowDialog();

                        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                        {
                            GlobalVariables.PathExport = fbd.SelectedPath;

                            Properties.Settings.Default.PathExport = GlobalVariables.PathExport;
                            Properties.Settings.Default.Save();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(GlobalVariables.PathExport))
                {
                    //string[] files = Directory.GetFiles(fbd.SelectedPath);
                    var _from = dateTimePickerFrom.Text;
                    var _to = dateTimePickerTo.Text;
                    var _logType = comboBoxLogType.Text;
                    var _part = cbPart.Text;

                    using (var connection = GlobalVariables.GetDbConnection())
                    {
                        string _where = null;
                        if (_part == "All")
                        {
                            _where = $"CreatedDate >= '{_from}' and CreatedDate <= '{_to}' and LogStyle = '{_logType}'";
                        }
                        else
                        {
                            _where = $"CreatedDate >= '{_from}' and CreatedDate <= '{_to}' and LogStyle = '{_logType}' and Part = '{_part}'";
                        }

                        #region Sanding
                        var dataSanding = connection.Query<tblDataLogSandingModel>("select Station, ShaftNumber, CreatedDate, WorkOrder, Part,Freq01Reading,MotorSandingSpeed,Freq02Reading,FreqTarget,FormulaGId,LogStyle " +
                    "from tblDataLogSanding " +
                    $"Where {_where} " +
                    $"Order by CreatedDate asc").ToList();
                        if (dataSanding.Count > 0)
                        {
                            string csvHeaderRow = String.Join(",", typeof(tblDataLogSandingModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => x.Name).ToArray<string>());

                            var sb = new StringBuilder();

                            sb.AppendLine(csvHeaderRow.Substring(3));

                            foreach (var item in dataSanding)
                            {
                                sb.AppendLine(item.Station + "," + item.ShaftNumber + "," + item.CreatedDate + ", " + item.WorkOrder + ", " + item.Part + ", " + item.Freq01Reading
                                    + ", " + item.MotorSandingSpeed + ", " + item.Freq02Reading + ", " + item.FreqTarget + ", " + item.FormulaGId + ", " + item.LogStyle);
                            }

                            File.WriteAllText(Path.Combine(GlobalVariables.PathExport, $"{DateTime.Now.ToString("yyyyMMddHHmmss")}DataSanding.csv"), sb.ToString());
                        }
                        #endregion

                        #region Tip OD

                        if (_part == "All")
                        {
                            _where = $"CreatedDate >= '{_from}' and CreatedDate <= '{_to}' and LogType = '{_logType}'";
                        }
                        else
                        {
                            _where = $"CreatedDate >= '{_from}' and CreatedDate <= '{_to}' and LogType = '{_logType}' and Part = '{_part}'";
                        }

                        var dataTipOd = connection.Query<tblDataLogTipOdModel>("select Station, ShaftNumber, CreatedDate, WorkOrder, Part,DiamReading,MeasType,DiamLL,DiamUL,PassFail,LogType" +
                    " from tblDataLogTipOd " +
                    $"Where {_where} " +
                    $"Order by CreatedDate asc").ToList();
                        if (dataTipOd.Count > 0)
                        {
                            string csvHeaderRow = String.Join(",", typeof(tblDataLogTipOdModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => x.Name).ToArray<string>());

                            var sb = new StringBuilder();

                            sb.AppendLine(csvHeaderRow.Substring(3));

                            foreach (var item in dataTipOd)
                            {
                                sb.AppendLine(item.Station + "," + item.ShaftNumber + "," + item.CreatedDate + ", " + item.WorkOrder + ", " + item.Part + ", " + item.DiamReading
                                    + ", " + item.MeasType + ", " + item.DiamLL + ", " + item.DiamUL + ", " + item.PassFail + ", " + item.LogType);
                            }

                            File.WriteAllText(Path.Combine(GlobalVariables.PathExport, $"{DateTime.Now.ToString("yyyyMMddHHmmss")}DataTipOD.csv"), sb.ToString());
                        }
                        #endregion

                        #region polishing
                        var dataPolishing = connection.Query<tblDataLogPolishingModel>("select Station, ShaftNumber, CreatedDate, WorkOrder, Part,FreqReading,FreqTarget,MortorPolishing,FormulaPO,LogType " +
                    "from tblDataLogPolishing " +
                     $"Where {_where} " +
                     $"Order by CreatedDate asc").ToList();
                        if (dataPolishing.Count > 0)
                        {
                            string csvHeaderRow = String.Join(",", typeof(tblDataLogPolishingModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => x.Name).ToArray<string>());

                            var sb = new StringBuilder();

                            sb.AppendLine(csvHeaderRow.Substring(3));

                            foreach (var item in dataPolishing)
                            {
                                sb.AppendLine(item.Station + "," + item.ShaftNumber + "," + item.CreatedDate + ", " + item.WorkOrder + ", " + item.Part + ", " + item.FreqReading
                                    + ", " + item.FreqTarget + ", " + item.MortorPolishing + ", " + item.FormulaPO + ", " + item.LogType);
                            }

                            File.WriteAllText(Path.Combine(GlobalVariables.PathExport, $"{DateTime.Now.ToString("yyyyMMddHHmmss")}DataPolishing.csv"), sb.ToString());
                        }
                        #endregion

                        MessageBox.Show($"Xuất file thành công.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show($"Không tìm thấy đường dẫn lưu file, mời chọn lạ vị trí lưu file mới.", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch
            {

            }
        }

        private void BtnQuery_Click(object sender, EventArgs e)
        {
            try
            {
                var _from = dateTimePickerFrom.Text;
                var _to = dateTimePickerTo.Text;
                var _logType = comboBoxLogType.Text;
                var _part = cbPart.Text;

                #region Clear dataGrid
                if (dataGridViewSanding.InvokeRequired)
                {
                    dataGridViewSanding.Invoke(new Action(() =>
                    {
                        dataGridViewSanding.DataSource = null;
                    }));
                }
                else
                {
                    dataGridViewSanding.DataSource = null;
                }

                if (dataGridViewTipOd.InvokeRequired)
                {
                    dataGridViewTipOd.Invoke(new Action(() =>
                    {
                        dataGridViewTipOd.DataSource = null;
                    }));
                }
                else
                {
                    dataGridViewTipOd.DataSource = null;
                }

                if (dataGridViewPolishing.InvokeRequired)
                {
                    dataGridViewPolishing.Invoke(new Action(() =>
                    {
                        dataGridViewPolishing.DataSource = null;
                    }));
                }
                else
                {
                    dataGridViewPolishing.DataSource = null;
                }
                #endregion

                using (var connection = GlobalVariables.GetDbConnection())
                {
                    string _where = null;
                    if (_part == "All")
                    {
                        _where = $"CreatedDate >= '{_from}' and CreatedDate <= '{_to}' and LogStyle = '{_logType}'";
                    }
                    else
                    {
                        _where = $"CreatedDate >= '{_from}' and CreatedDate <= '{_to}' and LogStyle = '{_logType}' and Part = '{_part}'";
                    }

                    var dataSanding = connection.Query<tblDataLogSandingModel>("select Station, ShaftNumber, CreatedDate, WorkOrder, Part,Freq01Reading,MotorSandingSpeed,Freq02Reading,FreqTarget,FormulaGId,LogStyle " +
                        "from tblDataLogSanding " +
                        $"Where {_where} " +
                        $"Order by CreatedDate asc").ToList();
                    if (dataSanding.Count > 0)
                    {
                        if (dataGridViewSanding.InvokeRequired)
                        {
                            dataGridViewSanding.Invoke(new Action(() =>
                            {
                                dataGridViewSanding.DataSource = dataSanding;
                                dataGridViewSanding.Columns["Id"].Visible = false;
                                dataGridViewSanding.AutoResizeColumns();
                            }));
                        }
                        else
                        {
                            dataGridViewSanding.DataSource = dataSanding;
                            dataGridViewSanding.Columns["Id"].Visible = false;
                            dataGridViewSanding.AutoResizeColumns();
                        }
                    }

                    if (_part == "All")
                    {
                        _where = $"CreatedDate >= '{_from}' and CreatedDate <= '{_to}' and LogType = '{_logType}'";
                    }
                    else
                    {
                        _where = $"CreatedDate >= '{_from}' and CreatedDate <= '{_to}' and LogType = '{_logType}' and Part = '{_part}'";
                    }

                    var dataTipOd = connection.Query<tblDataLogTipOdModel>("select Station, ShaftNumber, CreatedDate, WorkOrder, Part,DiamReading,MeasType,DiamLL,DiamUL,PassFail,LogType" +
                        " from tblDataLogTipOd " +
                        $"Where {_where} " +
                        $"Order by CreatedDate asc").ToList();
                    if (dataTipOd.Count > 0)
                    {
                        if (dataGridViewTipOd.InvokeRequired)
                        {
                            dataGridViewTipOd.Invoke(new Action(() =>
                            {
                                dataGridViewTipOd.DataSource = dataTipOd;
                                dataGridViewTipOd.Columns["Id"].Visible = false;
                                dataGridViewTipOd.AutoResizeColumns();
                            }));
                        }
                        else
                        {
                            dataGridViewTipOd.DataSource = dataTipOd;
                            dataGridViewTipOd.Columns["Id"].Visible = false;
                            dataGridViewTipOd.AutoResizeColumns();
                        }
                    }

                    var dataPolishing = connection.Query<tblDataLogPolishingModel>("select Station, ShaftNumber, CreatedDate, WorkOrder, Part,FreqReading,FreqTarget,MortorPolishing,FormulaPO,LogType " +
                        "from tblDataLogPolishing " +
                         $"Where {_where} " +
                         $"Order by CreatedDate asc").ToList();
                    if (dataPolishing.Count > 0)
                    {
                        if (dataGridViewPolishing.InvokeRequired)
                        {
                            dataGridViewPolishing.Invoke(new Action(() =>
                            {
                                dataGridViewPolishing.DataSource = dataPolishing;
                                dataGridViewPolishing.Columns["Id"].Visible = false;
                                dataGridViewPolishing.AutoResizeColumns();
                            }));
                        }
                        else
                        {
                            dataGridViewPolishing.DataSource = dataPolishing;
                            dataGridViewPolishing.Columns["Id"].Visible = false;
                            dataGridViewPolishing.AutoResizeColumns();
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Loi: { ex.Message}");
            }
        }

        private void NT_Tick(object sender, EventArgs e)
        {
            Timer _nt = (Timer)sender;

            _nt.Enabled = false;

            if (labStatus.InvokeRequired)
            {
                labStatus.Invoke(new Action(() =>
                {
                    labStatus.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                }));
            }
            else
                labStatus.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            _nt.Enabled = true;
        }

        private void btnUpdateData_Click(object sender, EventArgs e)
        {
            try
            {
                int countExecute = 0;

                #region Tip Od Freq
                string[] lines = System.IO.File.ReadAllLines(GlobalVariables.PathOd);

                if (lines.Count() > 0)
                {
                    var dataTip = new List<tblTipOdFreqModel>();

                    int index = 0;
                    foreach (string line in lines)
                    {
                        if (index != 0)
                        {
                            string[] columns = line.Split(',');
                            dataTip.Add(new tblTipOdFreqModel()
                            {
                                ItemNumber = columns[0],
                                FreqTarget = int.TryParse(columns[1], out int value) ? value : 0,
                                DiamLL = double.TryParse(columns[2], out double value1) ? value1 : 0,
                                DiamUL = double.TryParse(columns[3], out value1) ? value1 : 0,
                                TipOdLength = columns[4],
                                FormulaGId = int.TryParse(columns[5], out value) ? value : 0,
                                FormulaPoId = int.TryParse(columns[6], out value) ? value : 0,
                            });
                        }
                        index += 1;
                    }

                    using (var connection = GlobalVariables.GetDbConnection())
                    {
                        connection.Execute("delete tblTipOdFreq");

                        var count = connection.Execute(@"insert tblTipOdFreq (ItemNumber, FreqTarget, DiamLL,DiamUL,TipOdLength,FormulaGId,FormulaPoId) 
                                                    values (@ItemNumber, @Freqtarget,@DiamLL,@DiamUL,@TipOdLength,@FormulaGId,@FormulaPoId)", dataTip);

                        if (dataTip.Count() == count)
                        {
                            countExecute += 1;
                        }
                    }
                }
                #endregion

                #region Formula G
                //lines = System.IO.File.ReadAllLines(GlobalVariables.PathFormulaG);

                //if (lines.Count() > 0)
                //{
                //    var dataFormulaG = new List<tblFormulaGModel>();

                //    int index = 0;
                //    foreach (string line in lines)
                //    {
                //        if (index != 0)
                //        {
                //            string[] columns = line.Split(',');
                //            dataFormulaG.Add(new tblFormulaGModel()
                //            {
                //                Id = int.TryParse(columns[0], out int value) ? value : 0,
                //                U = int.TryParse(columns[1], out value) ? value : 0,
                //                V = int.TryParse(columns[2], out value) ? value : 0,
                //                X = double.TryParse(columns[3], out double value1) ? value1 : 0,
                //                Y = double.TryParse(columns[4], out value1) ? value1 : 0,
                //                Z = int.TryParse(columns[5], out value) ? value : 0,
                //                P = int.TryParse(columns[6], out value) ? value : 0,
                //            });
                //        }
                //        index += 1;
                //    }

                //    using (var connection = GlobalVariables.GetDbConnection())
                //    {
                //        connection.Execute("delete tblFormulaG");

                //        var count = connection.Execute(@"insert tblFormulaG (Id, U, V,X,Y,Z,P) 
                //                                        values (@Id, @U,@V,@X,@Y,@Z,@P)", dataFormulaG);

                //        if (dataFormulaG.Count() == count)
                //        {
                //            countExecute += 1;
                //        }
                //    }
                //}
                #endregion

                #region Formula PO
                //lines = System.IO.File.ReadAllLines(GlobalVariables.PathFormulaPo);

                //if (lines.Count() > 0)
                //{
                //    var dataFormulaPo = new List<tblFormulaPoModel>();

                //    int index = 0;
                //    foreach (string line in lines)
                //    {
                //        if (index != 0)
                //        {
                //            string[] columns = line.Split(',');
                //            dataFormulaPo.Add(new tblFormulaPoModel()
                //            {
                //                Id = int.TryParse(columns[0], out int value) ? value : 0,
                //                U = int.TryParse(columns[1], out value) ? value : 0,
                //                V = int.TryParse(columns[2], out value) ? value : 0,
                //                X = double.TryParse(columns[3], out double value1) ? value1 : 0,
                //                Y = double.TryParse(columns[4], out value1) ? value1 : 0,
                //                Z = int.TryParse(columns[5], out value) ? value : 0,
                //                P = int.TryParse(columns[6], out value) ? value : 0,
                //            });
                //        }
                //        index += 1;
                //    }

                //    using (var connection = GlobalVariables.GetDbConnection())
                //    {
                //        connection.Execute("delete tblFormulaPo");

                //        var count = connection.Execute(@"insert tblFormulaPo (Id, U, V,X,Y,Z,P) 
                //                                        values (@Id, @U,@V,@X,@Y,@Z,@P)", dataFormulaPo);

                //        if (dataFormulaPo.Count() == count)
                //        {
                //            countExecute += 1;
                //        }
                //    }
                //}
                #endregion

                if (countExecute == 1)
                {
                    MessageBox.Show("Impport data successfull.");
                }
                else
                {
                    MessageBox.Show("Fail.");
                }
            }
            catch
            {

            }
        }

        public static void OpenFile(string fileName)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = fileName;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Normal;
            Process p = new Process();
            p.StartInfo = info;
            p.Start();
        }

        private void cbPart_TextChanged(object sender, EventArgs e)
        {
            ComboBox _cb = (ComboBox)sender;

            using (var connection = GlobalVariables.GetDbConnection())
            {
                var para = new DynamicParameters();
                para.Add("@part", _cb.Text);
                var _part = connection.Query<tblTipOdFreqModel>("sp_GetParts", para, commandType: CommandType.StoredProcedure).ToList();

                if (_part.Count > 0)
                {
                    _cb.Items.Clear();

                    foreach (var item in _part)
                    {
                        if (cbPart.InvokeRequired)
                        {
                            cbPart.Invoke(new Action(() =>
                            {
                                cbPart.Items.Add(item.ItemNumber);
                            }));
                        }
                        else
                        {
                            cbPart.Items.Add(item.ItemNumber);
                        }
                    }

                    if (cbPart.InvokeRequired)
                    {
                        cbPart.Invoke(new Action(() =>
                        {
                            cbPart.Items.Add("All");
                        }));
                    }
                    else
                    {
                        cbPart.Items.Add("All");
                    }
                }
            }
        }
    }
}
