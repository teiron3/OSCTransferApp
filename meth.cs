
/*
    Copyright (c) 2023 teiron
    Released under the MIT license
    https://github.com/teiron3/OSCTestServer/blob/main/LICENSE
*/

using System;
using System.IO;
using System.Media;
using System.Windows.Forms;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace test
{
    partial class Form1 : Form
    {

        private byte[] loopbackarray = System.Text.Encoding.ASCII.GetBytes("/loopback/");
        private byte[] outsidearray = System.Text.Encoding.ASCII.GetBytes("/outside/");
        private byte[] sendkeysarray = new byte[12];
        private byte[] voicevoxarray = new byte[12];
        private Queue<(int, string)> voicevoxlist = new Queue<(int, string)>();
        SoundPlayer player = new SoundPlayer();
        private void SettingArray()
        {
            for (int count = 0; count < 8; count++)
            {
                sendkeysarray[count] = 0;
                voicevoxarray[count] = 0;
            }
            Encoding.ASCII.GetBytes("/sendkeys").CopyTo(sendkeysarray, 0);
            Encoding.ASCII.GetBytes("/voicevox").CopyTo(voicevoxarray, 0);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            IPEndPoint refIP = null;
            byte[] receiveBytes = UdpDestSocket.EndReceive(ar, ref refIP);

            //受信の終了
            //最初のデータが q の場合に受信を終了します
            if (receiveBytes[0] == 0x71)
            {
                Console.WriteLine("stop process");
                UdpDestSocket.Close();
                return;
            }

            //OSCのアドレスの最初に /loopback/ (/loopback だけだとダメ)が入っている場合の処理
            //バーチャルキャストの受信ポートにデータをそのまま戻します
            for (int count = 0; count < loopbackarray.Length; count++)
            {
                if (receiveBytes[count] != loopbackarray[count]) break;
                if (count == loopbackarray.Length - 1)
                {
                    if (CheckboxRemoveloopback.Checked)
                    {
                        byte[] sendBytes = removeaddress(receiveBytes, "/loopback");
                        UdpSendSocket.Send(sendBytes, sendBytes.Length, VirtualCastDestIP);
                    }
                    else
                    {
                        UdpSendSocket.Send(receiveBytes, receiveBytes.Length, VirtualCastDestIP);
                    }
                    UdpDestSocket.BeginReceive(new AsyncCallback(ReceiveCallback), null);
                    return;
                }
            }

            //OSCのアドレスの最初に /outside/ (/outside だけだとダメ)が入っている場合の処理
            //外部のIPアドレスにデータをそのまま送信します
            for (int count = 0; count < outsidearray.Length; count++)
            {
                if (receiveBytes[count] != outsidearray[count]) break;
                if (count == outsidearray.Length - 1)
                {
                    if (CheckboxRemoveoutside.Checked)
                    {
                        byte[] sendBytes = removeaddress(receiveBytes, "/outside");
                        UdpSendSocket.Send(sendBytes, sendBytes.Length, OutsideSendIP);
                    }
                    else
                    {
                        UdpSendSocket.Send(receiveBytes, receiveBytes.Length, OutsideSendIP);
                    }
                    UdpDestSocket.BeginReceive(new AsyncCallback(ReceiveCallback), null);
                    return;
                }
            }

            //OSCのアドレスが /voicevox の場合の処理
            //int と blob のみ場合だけVoiceVoxのAPIに送信します
            for (int count = 0; count < voicevoxarray.Length; count++)
            {
                if (receiveBytes[count] != voicevoxarray[count]) break;
                if (count == voicevoxarray.Length - 1)
                {
                    if (
                        receiveBytes[12] == Convert.ToByte(',') &&
                        receiveBytes[13] == Convert.ToByte('i') &&
                        receiveBytes[14] == Convert.ToByte('b') &&
                        receiveBytes[15] == 0
                    )
                    {
                        voicevoxlist.Enqueue(
                         (BitConverter.ToInt32(new byte[] { receiveBytes[19], receiveBytes[20], 0, 0 }, 0),
                          Encoding.UTF8.GetString(receiveBytes, 24, receiveBytes.Length - 24)
                          )
                          );

                    };

                }

            }

            //OSCのアドレスが /sendkeys の場合の処理
            //string1個のみ場合だけSendkeysでアクティブウィンドウにキーを送信します
            if (this.CheckboxSendkeysIsEnable.Checked)
            {
                for (int count = 0; count < sendkeysarray.Length; count++)
                {
                    if (receiveBytes[count] != sendkeysarray[count]) break;
                    if (count == sendkeysarray.Length - 1 && receiveBytes[count + 1] == 0)
                    {
                        int subcount = count + 1;
                        while ((char)receiveBytes[++subcount] != ',') { }
                        if ((char)receiveBytes[subcount + 1] != 's' && receiveBytes[subcount + 2] != 0) break;
                        subcount += 2;
                        while (receiveBytes[++subcount] == 0) { if (subcount + 1 == receiveBytes.Length) break; }
                        if (subcount + 1 == receiveBytes.Length) break;
                        int stringstartpoint = subcount;
                        while (receiveBytes[++subcount] != 0) { if (subcount + 1 == receiveBytes.Length) break; }
                        int stringlength = subcount - stringstartpoint;
                        SendKeys.SendWait(System.Text.Encoding.ASCII.GetString((new ArraySegment<byte>(receiveBytes, stringstartpoint, stringlength)).ToArray()));
                        UdpDestSocket.BeginReceive(new AsyncCallback(ReceiveCallback), null);
                        return;
                    }
                }
            }

            //上記以外のものはそのまま送信
            UdpSendSocket.Send(receiveBytes, receiveBytes.Length, ThisAppSendIP);
            UdpDestSocket.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }

        ///OSCのアドレスから不要な部分を取り除く処理
        private byte[] removeaddress(byte[] bytes, string removestring)
        {
            int bytesstartpoint = removestring.Length;
            int bytesaddressendpoint = bytesstartpoint;
            int addresslenght;
            int padzerolength;
            int datastartpoint;
            byte[] returnbytes;
            while (bytes[++bytesaddressendpoint] != 0) ;
            --bytesaddressendpoint;
            addresslenght = bytesaddressendpoint - bytesstartpoint + 1;
            padzerolength = addresslenght % 4;
            padzerolength = (padzerolength == 0) ? 4 : 4 - padzerolength;
            datastartpoint = bytesaddressendpoint + 1;
            while (bytes[++datastartpoint] != (byte)',') ;

            returnbytes = new byte[addresslenght + padzerolength + (bytes.Length - datastartpoint)];
            int writepoint = 0;
            while (writepoint + bytesstartpoint <= bytesaddressendpoint)
            {
                returnbytes[writepoint] = bytes[writepoint + bytesstartpoint];
                writepoint++;
            }
            for (int writecount = 0; writecount < padzerolength; writecount++)
            {
                returnbytes[writepoint] = 0;
                writepoint++;
            }
            for (int writecount = 0; writecount + datastartpoint < bytes.Length; writecount++)
            {
                returnbytes[writepoint] = bytes[writecount + datastartpoint];
                writepoint++;
            }

            return returnbytes;
        }

        private async void VoiceVoxAsync()
        {
            while (true)
            {
                if (voicevoxlist.Count > 0)
                {
                    (int id, string voice) = voicevoxlist.Dequeue();
                    await SendVoicevoxAsync(id, voice);
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
        }

        private async Task SendVoicevoxAsync(int id, string voice)
        {
            foreach (string text0 in voice.Replace(" ", "").Split('\n'))

            {
                foreach (string text in text0.Split('。'))
                {
                    if (text.Length < 1) continue;
                    string query;
                    var voicesList = new List<MemoryStream>();
                    using (var httpClient = new HttpClient())
                    {
                        var speaker = id < 1 ? "1" : id.ToString();
                        // 音声クエリを生成
                        using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:50021/audio_query?text={text}&speaker={speaker}"))
                        {
                            request.Headers.TryAddWithoutValidation("accept", "application/json");

                            request.Content = new StringContent("");
                            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
                            try
                            {
                                var response = await httpClient.SendAsync(request);
                                var responseResult = response.Content.ReadAsStringAsync().Result;
                                if (responseResult.StartsWith("{\"accent_phrases\":"))
                                {
                                    query = Regex.Replace(
                                        responseResult,
                                        "\"volumeScale\":(\\d+(\\.\\d+)?),\"prePhonemeLength\":(\\d+(\\.\\d+)?)",
                                        string.Format("\"volumeScale\":{0:f1},\"prePhonemeLength\":0.5", ((double)TrackbarVoiceVoxVolume.Value) / 50.0)
                                    );
                                }
                                else
                                {
                                    throw new Exception("request error");
                                }

                            }
                            catch (Exception ex)
                            {
                                return;
                            }

                        }
                        /// 音声クエリから音声合成
                        using (var request = new HttpRequestMessage(new HttpMethod("POST"), $"http://localhost:50021/synthesis?speaker={speaker}&enable_interrogative_upspeak=true"))
                        {
                            request.Headers.TryAddWithoutValidation("accept", "audio/wav");

                            request.Content = new StringContent(query);
                            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                            try
                            {

                                var response = httpClient.SendAsync(request).Result;
                                // 音声を保存
                                using (var ms = (MemoryStream)response.Content.ReadAsStreamAsync().Result)
                                {
                                    player.Stream = ms;
                                    player.PlaySync();

                                }
                            }
                            catch (Exception ex)
                            {
                                return;
                            }

                        }

                    }
                }

            }
        }
    }
}