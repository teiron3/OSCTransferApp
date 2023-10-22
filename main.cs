/*
    Copyright (c) 2023 teiron
    Released under the MIT license
    https://github.com/teiron3/OSCTestServer/blob/main/LICENSE
*/

using System;
using System.Windows;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading.Tasks;

namespace test
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.Run(new Form1());
        }
    }
    partial class Form1 : Form
    {
        private Button ButtonStart;
        private Button ButtonStop;
        private Label LabelSendport;
        private TextBox TextboxSendport;
        private Label LabelDestport;
        private TextBox TextboxDestport;

        private Label LabelOutsideIP;
        private TextBox TextboxOutsideIP;
        private Label LabelThisAppSendPort;
        private TextBox TextboxThisAppSendPort;

        private Label LabelRemoveloopback;
        private CheckBox CheckboxRemoveloopback;

        private Label LabelRemoveoutside;
        private CheckBox CheckboxRemoveoutside;

        private Label LavelSendkeysIsEnable;
        private CheckBox CheckboxSendkeysIsEnable;

        private Label LabelVoiceVoxVolume;
        private TrackBar TrackbarVoiceVoxVolume;

        private UdpClient UdpSendSocket;
        private UdpClient UdpDestSocket;
        private IPEndPoint VirtualCastSendIP;
        private IPEndPoint VirtualCastDestIP;
        private IPEndPoint OutsideSendIP;
        private IPEndPoint ThisAppSendIP;

        public Form1()
        {
            this.Size = new Size(460, 400);
            this.Location = new Point(100, 100);
            this.Text = "OSC転送アプリ";

            ///
            int lableX = 10;
            int textboxx = 220;

            int locationy = 50;
            int addy = 25;

            UdpSendSocket = new UdpClient();
            Size labelSize = new Size(200, 20);
            Size labeloutsideSize = new Size(70, 20);
            Size textboxSize = new Size(200, 20);

            LabelThisAppSendPort = new Label
            {
                Parent = this,
                Size = labelSize,
                Location = new Point(lableX, locationy),
                Text = "このアプリからの送信ポート"
            };

            TextboxThisAppSendPort = new TextBox
            {
                Parent = this,
                Size = textboxSize,
                Location = new Point(textboxx, locationy),
                Text = "18101"
            };
            locationy += addy;

            LabelSendport = new Label
            {
                Parent = this,
                Size = labelSize,
                Text = "バーチャルキャストで設定した送信ポート",
                Location = new Point(lableX, locationy)
            };

            TextboxSendport = new TextBox
            {
                Parent = this,
                Size = textboxSize,
                Location = new Point(textboxx, locationy),
                Text = "18100"
            };
            locationy += addy;

            LabelDestport = new Label
            {
                Parent = this,
                Size = labelSize,
                Text = "バーチャルキャストで設定した受信ポート",
                Location = new Point(lableX, locationy)
            };

            TextboxDestport = new TextBox
            {
                Parent = this,
                Size = textboxSize,
                Location = new Point(textboxx, locationy),
                Text = "19100"
            };
            locationy += addy * 2;

            LabelOutsideIP = new Label
            {
                Parent = this,
                Size = labelSize,
                Location = new Point(lableX, locationy),
                Text = "送信先外部IPアドレス"
            };

            TextboxOutsideIP = new TextBox
            {
                Parent = this,
                Size = textboxSize,
                Location = new Point(textboxx, locationy),
                Text = "127.0.0.1"
            };

            locationy += addy * 2;

            LabelRemoveloopback = new Label
            {
                Parent = this,
                Size = labelSize,
                Location = new Point(lableX, locationy),
                Text = "アドレスから/loopbackを除く"
            };

            CheckboxRemoveloopback = new CheckBox
            {
                Parent = this,
                Location = new Point(textboxx, locationy),
                Checked = true
            };
            locationy += addy;

            LabelRemoveoutside = new Label
            {
                Parent = this,
                Size = labelSize,
                Location = new Point(lableX, locationy),
                Text = "アドレスから/outsideを除く"
            };

            CheckboxRemoveoutside = new CheckBox
            {
                Parent = this,
                Location = new Point(textboxx, locationy),
                Checked = true
            };
            locationy += addy;

            LavelSendkeysIsEnable = new Label
            {
                Parent = this,
                Size = labelSize,
                Location = new Point(lableX, locationy),
                Text = "SendKeysを有効にする"
            };

            CheckboxSendkeysIsEnable = new CheckBox
            {
                Parent = this,
                Location = new Point(textboxx, locationy),
                Checked = false
            };
            locationy += addy * 2;

            LabelVoiceVoxVolume = new Label
            {
                Parent = this,
                Size = labelSize,
                Location = new Point(lableX, locationy),
                Text = "VoiceVoxの音量"
            };

            TrackbarVoiceVoxVolume = new TrackBar
            {
                Parent = this,
                Location = new Point(textboxx, locationy),
                Size = new Size(200, 20),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                TickFrequency = 5
            };

            ButtonStart = new Button
            {
                Parent = this,
                Location = new Point(10, 10),
                Size = new Size(100, 30),
                Text = "start"
            };
            ButtonStart.Click += (obj, e) =>
            {
                Button btn = (Button)obj;
                string sendport = TextboxSendport.Text;
                string destport = TextboxDestport.Text;
                string thisappsendport = TextboxThisAppSendPort.Text;
                if (sendport == destport || sendport == thisappsendport || destport == thisappsendport)
                {
                    MessageBox.Show("ポート番号が重複しています。");
                    return;
                }
                try
                {
                    VirtualCastSendIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Int32.Parse(TextboxSendport.Text));
                    VirtualCastDestIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Int32.Parse(TextboxDestport.Text));
                    OutsideSendIP = new IPEndPoint(IPAddress.Parse(TextboxOutsideIP.Text), Int32.Parse(TextboxThisAppSendPort.Text));
                    ThisAppSendIP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Int32.Parse(TextboxThisAppSendPort.Text));
                    UdpDestSocket = new UdpClient(VirtualCastSendIP);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("どれかのアドレスまたはポートの設定が間違っています。\n(UDPの受信ポートが塞がってる可能性もあります。)\n" + ex.Message);
                    return;
                }
                UdpDestSocket.BeginReceive(new AsyncCallback(ReceiveCallback), null);
                btn.Enabled = false;
            };

            ButtonStop = new Button
            {
                Parent = this,
                Location = new Point(130, 10),
                Size = new Size(100, 30),
                Text = "stop"
            };
            ButtonStop.Click += (obj, e) =>
            {
                UdpSendSocket.Send(new byte[] { 0x71 }, 1, VirtualCastSendIP);
                if (!ButtonStart.Enabled)
                {
                    ButtonStart.Enabled = true;
                }
            };

            //アドレス判別用の配列を作成
            this.SettingArray();
            Task.Run(VoiceVoxAsync);
        }

    }
}