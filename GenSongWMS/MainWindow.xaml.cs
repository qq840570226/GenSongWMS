using System;
using System.Threading.Tasks;
using System.Windows;
using GenSongWMS.BLL;
using System.Xml.Linq;
using System.Data;

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
            dataTableForkliftStatus = new DataTable("叉车状态列表");
            DataGridForkliftStatus.ItemsSource = dataTableForkliftStatus.DefaultView;
            // 刷新叉车状态
            Task refreshDataGrid = Task.Factory.StartNew(RefreshForkliftStatusDataGridAsync);
        }

        /// <summary>
        /// 增加agv
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAddAGV_Click(object sender, RoutedEventArgs e)
        {
            // 裁剪ip
            string[] temp = txtNewAGVIp.Text.Split('.');
            // 校验ip
            if (txtNewAGVIp.Text == "" && temp.Length < 4)
            {
                return;
            }
            // 校验客户端
            if (MainFlow.Clients.ContainsKey(txtNewAGVIp.Text))
            {
                MessageBox.Show("控制端已连接上该小车");
                return;
            }
            // 启动客户端
            MainFlow.AddForkliftAsync(txtNewAGVIp.Text).Wait();
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
        private void BtnStockIn_Click(object sender, RoutedEventArgs e)
        {
            Task task = MainFlow.SetTask(unloadNodeTextbox.Text, targetStorageLocation.Text);
            task.Start();
        }

        /// <summary>
        /// 出库
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnStockOut_Click(object sender, RoutedEventArgs e)
        {
            Task task = MainFlow.SetTask(takeStockNode.Text, loadNodeTextbox.Text);
            task.Start();
        }

        /// <summary>
        /// 反复刷新datagrid
        /// </summary>
        /// <returns></returns>
        private async Task RefreshForkliftStatusDataGridAsync()
        {
            while (syncForkliftStatusData)
            {
                //清空叉车状态
                dataTableForkliftStatus.Clear();
                //查询所有叉车的状态
                foreach (var item in DataCache.DictionaryForkLiftStatus)
                {
                    // 更新datatable
                    dataTableForkliftStatus.Rows.Add(item.Key,
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
                await DataGridForkliftStatus.Dispatcher.BeginInvoke(new Action(() => { DataGridForkliftStatus.Items.Refresh(); }));
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
    }
}
