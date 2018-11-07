using System;
using System.Threading.Tasks;
using System.Windows;
using GenSongWMS.BLL;
using System.Xml.Linq;
using System.Data;
using System.Windows.Controls;
using System.Windows.Media;

namespace GenSongWMS
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 叉车状态
        /// </summary>
        private DataTable dataTableForkliftStatus;

        public bool syncForkliftStatusData = true;

        public MainWindow()
        {
            // 画页面
            InitializeComponent();
            InitDataTableForkliftStatus();
            DataGridForkliftStatus.ItemsSource = dataTableForkliftStatus.DefaultView;
            //DataCache.DictionaryForkLiftStatus.TryAdd("1", new ForkLiftStatus(ForkliftStatusEnum.Free, 0, 0, 0, 0, 0, 0, 0, 0));
            //DataCache.DictionaryTrafficJam.TryAdd("1", false);
            Task.Factory.StartNew(() => RefreshForkliftStatusDataGridAsync(), TaskCreationOptions.LongRunning);
            //btnLoadMap_Click(this, new RoutedEventArgs());
            for (int i = 0; i < 50; i++)
            {
                AddAgvLabel(i.ToString());
            }
        }

        /// <summary>
        /// 增加agv
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnAddAGV_Click(object sender, RoutedEventArgs e)
        {
            // 裁剪ip
            string[] temp = txtNewAGVIp.Text.Split('.');
            // 校验ip
            if (txtNewAGVIp.Text == "" && temp.Length < 4)
            {
                return;
            }
            // 校验客户端
            foreach (var item in MainFlow.Clients.Keys)
            {
                if (item.Contains(txtNewAGVIp.Text + ":" + txtNewAGVPort.Text))
                {
                    MessageBox.Show("控制端已连接上该小车,请不要重复操作!");
                    return;
                }
            }
            // 启动客户端
            string ip = txtNewAGVIp.Text;
            string port = txtNewAGVPort.Text;
            byte id = Convert.ToByte(txtNewAGVNum.Text);
            await MainFlow.AddForkliftAsync(ip, port, id);
            AddAgvLabel(id.ToString());
            txtNewAGVNum.Text = (id + 1).ToString();
        }

        /// <summary>
        /// 地图初始化
        /// </summary>
        private void InitializeMap()
        {
            System.Windows.Forms.OpenFileDialog openDlg = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "地图文件(*.xml)|*.xml|所有文件(*.*)|*.*",
                InitialDirectory = @"..\MapData"
            };
            if (openDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string file = openDlg.FileName;
                XDocument xdoc = XDocument.Load(file);
                XDocument myXDoc = DataCache.MapInit(xdoc);
                System.Windows.Forms.SaveFileDialog saveDlg = new System.Windows.Forms.SaveFileDialog
                {
                    Filter = "路径文件(*.xml)|*.xml",
                    InitialDirectory = @"..\MapData"
                };
                if (saveDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string xmlPath = saveDlg.FileName;
                    myXDoc.Save(xmlPath);//保存此结构（即：我们预期的xml文件）
                }
            }
        }

        /// <summary>
        /// 入库
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnStockIn_Click(object sender, RoutedEventArgs e)
        {
            await MainFlow.SetTask(unloadNodeTextbox.Text, targetStorageLocation.Text, txtNewAGVNum.Text);
            //await MainFlow.SetTask2(unloadNodeTextbox.Text, targetStorageLocation.Text);
            //var x = MainFlow.GetVs();
            //string y = "";
            //foreach (var item in x)
            //{
            //    y += item.ToString("X2");
            //}
            //MessageBox.Show(y);
            //await Task.Delay(100);
        }

        /// <summary>
        /// 出库
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnStockOut_Click(object sender, RoutedEventArgs e)
        {
            await MainFlow.SetTask(takeStockNode.Text, loadNodeTextbox.Text, txtNewAGVNum.Text);
        }

        /// <summary>
        /// 初始化状态表
        /// </summary>
        private void InitDataTableForkliftStatus()
        {
            dataTableForkliftStatus = new DataTable("叉车状态列表");
            dataTableForkliftStatus.Columns.Add("FORKLIFTID", Type.GetType("System.String"));
            dataTableForkliftStatus.Columns.Add("CURSTATUS", Type.GetType("System.String"));
            dataTableForkliftStatus.Columns.Add("CURNODE", Type.GetType("System.String"));
            dataTableForkliftStatus.Columns.Add("TAGETAGVANGLE", Type.GetType("System.String"));
            dataTableForkliftStatus.Columns.Add("ELECTRICITYVALUE", Type.GetType("System.String"));
            dataTableForkliftStatus.Columns.Add("POS_UX", Type.GetType("System.String"));
            dataTableForkliftStatus.Columns.Add("POS_UY", Type.GetType("System.String"));
            dataTableForkliftStatus.Columns.Add("IN_JAM", Type.GetType("System.Boolean"));
        }

        /// <summary>
        /// 反复刷新datagrid
        /// </summary>
        /// <returns></returns>
        private async Task RefreshForkliftStatusDataGridAsync()
        {
            Action action = DataGridForkliftStatus.Items.Refresh;
            while (syncForkliftStatusData)
            {
                //清空叉车状态
                dataTableForkliftStatus.Rows.Clear();
                //查询所有叉车的状态
                foreach (var item in DataCache.DictionaryForkLiftStatus)
                {
                    // 更新datatable
                    dataTableForkliftStatus.Rows.Add(MainFlow.ClientsID[item.Key],
                            item.Value.state.ToString(),
                            item.Value.currentNodeNum.ToString(),
                            item.Value.currentAngle.ToString(),
                            item.Value.electricityValue.ToString(),
                            item.Value.pos_ux.ToString(),
                            item.Value.pos_uy.ToString(),
                            DataCache.DictionaryTrafficJam[item.Key]
                            );
                }
                // 刷新叉车状态
                await DataGridForkliftStatus.Dispatcher.BeginInvoke(action);
                // 延迟1s
                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            syncForkliftStatusData = false;
            MainFlow.CloseMainFlow().Wait(2000);
        }

        /// <summary>
        /// 读取地图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnLoadMap_Click(object sender, RoutedEventArgs e)
        {
            InitializeMap();
            //DataCache.AllArcPaths = new System.Collections.Concurrent.ConcurrentDictionary<PointToPoint, System.Collections.Generic.List<uint>>();
            //DataCache.AllPointPaths = new System.Collections.Concurrent.ConcurrentDictionary<PointToPoint, System.Collections.Generic.List<uint>>();
            //DataCache.AllArcPaths.TryAdd(new PointToPoint(1, 2), new System.Collections.Generic.List<uint>() { 12 });
            //DataCache.AllArcPaths.TryAdd(new PointToPoint(2, 1), new System.Collections.Generic.List<uint>() { 21 });
            ////DataCache.AllArcPaths.TryAdd(new PointToPoint(2, 1), new System.Collections.Generic.List<uint>() { 21 });
            ////DataCache.AllArcPaths.TryAdd(new PointToPoint(5, 6), new System.Collections.Generic.List<uint>() { 56 });
            //DataCache.AllArcPaths.TryAdd(new PointToPoint(4, 5), new System.Collections.Generic.List<uint>() { 45 });
            //DataCache.AllArcPaths.TryAdd(new PointToPoint(5, 4), new System.Collections.Generic.List<uint>() { 54 });
            //DataCache.AllArcPaths.TryAdd(new PointToPoint(6, 7), new System.Collections.Generic.List<uint>() { 67 });
            //DataCache.AllArcPaths.TryAdd(new PointToPoint(7, 6), new System.Collections.Generic.List<uint>() { 76 });
            //DataCache.AllArcPaths.TryAdd(new PointToPoint(8, 9), new System.Collections.Generic.List<uint>() { 89 });
            //DataCache.AllArcPaths.TryAdd(new PointToPoint(9, 8), new System.Collections.Generic.List<uint>() { 98 });
            ////DataCache.AllArcPaths.TryAdd(new PointToPoint(1, 6), new System.Collections.Generic.List<uint>() { 12, 23, 34, 45, 56 });
            ////DataCache.AllArcPaths.TryAdd(new PointToPoint(6, 1), new System.Collections.Generic.List<uint>() { 65, 57, 78, 82, 21 });
            ////DataCache.AllArcPaths.TryAdd(new PointToPoint(2, 5), new System.Collections.Generic.List<uint>() { 12, 23, 34, 45 });
            ////DataCache.AllArcPaths.TryAdd(new PointToPoint(5, 2), new System.Collections.Generic.List<uint>() { 65, 57, 78, 82 });
            ////DataCache.AllArcPaths.TryAdd(new PointToPoint(2, 3), new System.Collections.Generic.List<uint>() { 23 });
            ////DataCache.AllArcPaths.TryAdd(new PointToPoint(3, 4), new System.Collections.Generic.List<uint>() { 34 });

            //DataCache.AllPointPaths.TryAdd(new PointToPoint(1, 2), new System.Collections.Generic.List<uint>() { 1, 2 });
            //DataCache.AllPointPaths.TryAdd(new PointToPoint(2, 1), new System.Collections.Generic.List<uint>() { 2, 1 });
            ////DataCache.AllPointPaths.TryAdd(new PointToPoint(2, 1), new System.Collections.Generic.List<uint>() { 2, 1 });
            ////DataCache.AllPointPaths.TryAdd(new PointToPoint(5, 6), new System.Collections.Generic.List<uint>() { 5, 6 });
            //DataCache.AllPointPaths.TryAdd(new PointToPoint(4, 5), new System.Collections.Generic.List<uint>() { 4, 5 });
            //DataCache.AllPointPaths.TryAdd(new PointToPoint(5, 4), new System.Collections.Generic.List<uint>() { 5, 4 });
            //DataCache.AllPointPaths.TryAdd(new PointToPoint(6, 7), new System.Collections.Generic.List<uint>() { 6, 7 });
            //DataCache.AllPointPaths.TryAdd(new PointToPoint(7, 6), new System.Collections.Generic.List<uint>() { 7, 6 });
            //DataCache.AllPointPaths.TryAdd(new PointToPoint(8, 9), new System.Collections.Generic.List<uint>() { 8, 9 });
            //DataCache.AllPointPaths.TryAdd(new PointToPoint(9, 8), new System.Collections.Generic.List<uint>() { 9, 8 });
            ////DataCache.AllPointPaths.TryAdd(new PointToPoint(1, 6), new System.Collections.Generic.List<uint>() { 1, 2, 3, 4, 5, 6 });
            ////DataCache.AllPointPaths.TryAdd(new PointToPoint(6, 1), new System.Collections.Generic.List<uint>() { 6, 5, 7, 8, 2, 1 });
            ////DataCache.AllPointPaths.TryAdd(new PointToPoint(2, 5), new System.Collections.Generic.List<uint>() { 2, 3, 4, 5 });
            ////DataCache.AllPointPaths.TryAdd(new PointToPoint(5, 2), new System.Collections.Generic.List<uint>() { 5, 7, 8, 2 });
            ////DataCache.AllPointPaths.TryAdd(new PointToPoint(2, 3), new System.Collections.Generic.List<uint>() { 2, 3 });
            ////DataCache.AllPointPaths.TryAdd(new PointToPoint(3, 4), new System.Collections.Generic.List<uint>() { 3, 4 });
            //DataCache.AllArcPaths.TryAdd(new PointToPoint(1, 5), new System.Collections.Generic.List<uint>() { 12, 23, 34, 45 });
            //DataCache.AllArcPaths.TryAdd(new PointToPoint(5, 1), new System.Collections.Generic.List<uint>() { 54, 43, 32, 21 });
            //DataCache.AllArcPaths.TryAdd(new PointToPoint(6, 9), new System.Collections.Generic.List<uint>() { 67, 73, 38, 89 });
            //DataCache.AllArcPaths.TryAdd(new PointToPoint(9, 6), new System.Collections.Generic.List<uint>() { 98, 83, 37, 76 });

            //DataCache.AllPointPaths.TryAdd(new PointToPoint(1, 5), new System.Collections.Generic.List<uint>() { 1, 2, 3, 4, 5 });
            //DataCache.AllPointPaths.TryAdd(new PointToPoint(5, 1), new System.Collections.Generic.List<uint>() { 5, 4, 3, 2, 1 });
            //DataCache.AllPointPaths.TryAdd(new PointToPoint(6, 9), new System.Collections.Generic.List<uint>() { 6, 7, 3, 8, 9 });
            //DataCache.AllPointPaths.TryAdd(new PointToPoint(9, 6), new System.Collections.Generic.List<uint>() { 9, 8, 3, 7, 6 });
        }

        private void AddAgvLabel(string id)
        {
            //int x = 0, y = 0, count = 0;
            //foreach (var item in ConnectGrid.Children)
            //{
            //    if (item is Label)
            //    {
            //        count++;
            //    }
            //}
            int count = Convert.ToInt32(id);

            int x = (count % 10) * 100 + 10;
            int y = (count / 10) * 100 + 10;

            Label label = new Label()
            {
                Content = id,
                FontSize = 20,
                Width = 80,
                Height = 80,
                Margin = new Thickness(x, y, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.CornflowerBlue
            };

            ConnectGrid.Children.Add(label);
        }

        public void RemoveAgvLabel(string id)
        {
            foreach (Label item in ConnectGrid.Children)
            {
                if (item.Content.ToString() == id)
                {
                    ConnectGrid.Children.Remove(item);
                }
            }
        }

        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            //e.Delta
            ConnectGrid.Margin = new Thickness(0, ConnectGrid.Margin.Top + e.Delta, 0, 0);
            Panel.SetZIndex(ConnectGrid, -10);
        }
    }
}
