using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NativeWiFi;
using System.Data.SQLite;
using MetroFramework.Forms;

namespace WiFi
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        string connectionstring;
        public Form1()
        {
            InitializeComponent();
            timer1.Start();
            //locate database (sqlite)
            connectionstring = "Data Source=|DataDirectory|\\wifi.db";
        }
        string name;
        public static string test1;
        public static double x, y;
        string[] aps = new string[50];
        string[] found = new string[3];
        int m=0;

        public void UpDatePos(string apn, string dst, string RSsi)
        {
            try
            {
                if (apn != "" && dst != "" && RSsi != "")
                {
                    using (SQLiteConnection conn1 = new SQLiteConnection(connectionstring))
                    {
                        conn1.Open();
                        SQLiteCommand cmd = new SQLiteCommand();
                        cmd.Connection = conn1;
                        cmd.CommandText = "UPDATE accesspoints SET Distance = '" + dst + "', RSSI_Value = '"+RSsi+"' WHERE APName = '" + apn + "' ";
                        cmd.ExecuteNonQuery();               
                        conn1.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void Display()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    SQLiteCommand cmmd = new SQLiteCommand();
                    cmmd.Connection = conn;
                    cmmd.CommandText = "SELECT APName, RSSI_Value, Distance FROM accesspoints";
                    SQLiteDataReader read = cmmd.ExecuteReader();
                    int e = 0;
                    while (read.Read())
                    {
                        apTable1.Rows.Add(new object[]
                        {
                            read.GetValue(read.GetOrdinal("APName")),
                            read.GetValue(read.GetOrdinal("RSSI_Value")),
                            read.GetValue(read.GetOrdinal("Distance"))
                        });
                        aps[e] = read.GetValue(read.GetOrdinal("APName")).ToString();
                        e++;
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //Coordinates display in second table
        public void DisplayCor()
        {
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    SQLiteCommand cmmd = new SQLiteCommand();
                    cmmd.Connection = conn;
                    cmmd.CommandText = "SELECT APName, X_Coordinate, Y_Coordinate FROM accesspoints";
                    SQLiteDataReader read = cmmd.ExecuteReader();
                    while (read.Read())
                    {
                        coordinateTab.Rows.Add(new object[]
                        {
                            read.GetValue(read.GetOrdinal("APName")),
                            read.GetValue(read.GetOrdinal("X_Coordinate")),
                            read.GetValue(read.GetOrdinal("Y_Coordinate"))
                        });
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void rssi_signals()
        {
            var client = new WlanClient();
            foreach (var @interface in client.Interfaces)
            {
                name = @interface.InterfaceName;
                var networks = @interface.GetAvailableNetworkList(0);

                foreach (var network in networks.Skip(1))
                {
                    var ssid = network.dot11Ssid;
                    var networkName = Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
                    double dbm = Convert.ToDouble((Convert.ToInt16((network.wlanSignalQuality)) / 2) - 100);
                    double calc = (-50 - (dbm)) / (10 * 2);
                    double d = (Math.Pow(calc, 10));                    
                apTable1.Rows.Add(new object[] {
                    networkName,
                    dbm,
                    d.ToString("0." + new string('#', 339))
                });
                }

            }
            foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
            {

                Wlan.WlanBssEntry[] wlanBssEntries = wlanIface.GetNetworkBssList();

                foreach (Wlan.WlanBssEntry network in wlanBssEntries)
                {
                    int rss = network.rssi;
                    //     MessageBox.Show(rss.ToString());
                    byte[] macAddr = network.dot11Bssid;

                    string tMac = "";

                    for (int i = 0; i < macAddr.Length; i++)
                    {

                        tMac += macAddr[i].ToString("x2").PadLeft(2, '0').ToUpper();

                    }
                    coordinateTab.Rows.Add(new object[] {
                    System.Text.ASCIIEncoding.ASCII.GetString(network.dot11Ssid.SSID).ToString(),
                    tMac,
                    rss.ToString(),
                    network.linkQuality
                });

                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            apTable1.Rows.Clear();
            coordinateTab.Rows.Clear();
            rssi_signals();
            Display();
            DisplayCor();
            UpdatFromTable();
            timer2.Start();
        }

        private void dataGridView1_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {
            int rowindex = apTable1.CurrentCell.RowIndex;
            apname.Text = apTable1.Rows[rowindex].Cells[0].Value.ToString();
            rssiv.Text = apTable1.Rows[rowindex].Cells[1].Value.ToString();
            dist.Text = apTable1.Rows[rowindex].Cells[2].Value.ToString();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            for (int d = 0; d < apTable1.RowCount; d++)
            {
                for (int f = 0; f < aps.Length; f++)
                {
                    if (apTable1.Rows[d].Cells[0].ToString() == aps[f])
                    {
                        found[d] = aps[f];
                        continue;
                    }
                }
            }

                if(found[0] != null && found[1] != null && found[2] != null)
                {
                UpdatFromTable();
                }
            if (m == 1)
            {              
                graph gp = new graph();
                gp.Show();
            }
            apname.Text = m.ToString();
            m++;
            timer2.Stop();
        }

        public void UpdatFromTable()
        {
            string distan, name1,rssiv;
            for (int i = 0; i < (apTable1.RowCount) - 1; i++)
            {
                distan = apTable1.Rows[i].Cells[2].Value.ToString();
                name1 = apTable1.Rows[i].Cells[0].Value.ToString();
                rssiv = apTable1.Rows[i].Cells[1].Value.ToString();
                UpDatePos(name1, distan, rssiv);
            }
        }
    }
}
