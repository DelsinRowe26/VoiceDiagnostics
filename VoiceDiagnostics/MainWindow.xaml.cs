using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.SoundOut;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
using static System.Net.WebRequestMethods;

namespace VoiceDiagnostics
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        [DllImport("BiblZvuk.dll", CallingConvention = CallingConvention.Cdecl)]
        //unsafe
        public static extern int vizualzvuk(string filename, string secfile, int[] Rdat, int ParV);

        private FileInfo fileInfo1 = new FileInfo("Data_Load.tmp");

        private SimpleMixer mMixer, mMixerRight;
        private int SampleRate;//44100;
        //private Equalizer equalizer;
        private WasapiOut mSoundOut, mSoundOut1;
        private WasapiCapture mSoundIn, mSoundIn1;
        private SampleDSP mDsp;
        private MMDeviceCollection mOutputDevices;
        private MMDeviceCollection mInputDevices;

        string langindex;
        private int BtnSetClick = 0, ImgBtnRecordClick = 0;

        private static string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static string pathDesk = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private static string path2;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnSettings_MouseMove(object sender, MouseEventArgs e)
        {
            string uri = @"VoiceDiagnostics\button\button-settings-hover.png";
            ImgBtnSettings.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
        }

        private void btnSettings_MouseLeave(object sender, MouseEventArgs e)
        {
            string uri = @"VoiceDiagnostics\button\button-settings-inactive.png";
            ImgBtnSettings.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            BtnSetClick++;
            string uri = @"VoiceDiagnostics\button\button-settings-active.png";
            ImgBtnSettings.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
            if (BtnSetClick == 1)
            {
                tabNFTSet.SelectedItem = TabSettings;
                lbSetSpeaker.Visibility = Visibility.Visible;
                lbSetMicrophone.Visibility = Visibility.Visible;
                //btnAudition1.Visibility = Visibility.Hidden;
                //imgShadowNFT.Visibility = Visibility.Hidden;
            }
            else
            {
                lbSetSpeaker.Visibility = Visibility.Hidden;
                lbSetMicrophone.Visibility = Visibility.Hidden;
                //btnAudition1.Visibility = Visibility.Visible;
                tabNFTSet.SelectedItem = TabNFT;
                BtnSetClick = 0;
            }
        }

        private void btnRecord_MouseMove(object sender, MouseEventArgs e)
        {
            string uri = @"VoiceDiagnostics\button\button-record-hover.png";
            ImgRecordingBtn.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
        }

        private void btnRecord_MouseLeave(object sender, MouseEventArgs e)
        {
            if (ImgBtnRecordClick == 1)
            {
                string uri = @"VoiceDiagnostics\button\button-record-active.png";
                ImgRecordingBtn.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
            }
            else
            {
                string uri = @"VoiceDiagnostics\button\button-record-inactive.png";
                ImgRecordingBtn.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
            }
        }

        private void btnRecord_Click(object sender, RoutedEventArgs e)
        {
            ImgBtnRecordClick = 1;
            string uri = @"VoiceDiagnostics\button\button-record-active.png";
            ImgRecordingBtn.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;

            btnRecord.IsEnabled = false;
            if (langindex == "0")
            {
                LogClass.LogWrite("Начало записи голоса.");
            }
            else
            {
                LogClass.LogWrite("Start voice recording.");
            }
        }

        private void VoiceDiagnostics_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SoftCl.IsSoftwareInstalled("Microsoft Visual C++ 2015-2022 Redistributable (x86) - 14.32.31332") == false)
                {
                    Process.Start("VC_redist.x86.exe");
                }

                MMDeviceEnumerator deviceEnum = new MMDeviceEnumerator();
                mInputDevices = deviceEnum.EnumAudioEndpoints(DataFlow.Capture, DeviceState.Active);
                MMDevice activeDevice = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);

                SampleRate = activeDevice.DeviceFormat.SampleRate;

                foreach (MMDevice device in mInputDevices)
                {
                    cmbInput.Items.Add(device.FriendlyName);
                    if (device.DeviceID == activeDevice.DeviceID) cmbInput.SelectedIndex = cmbInput.Items.Count - 1;
                }


                //Находит устройства для вывода звука и заполняет комбобокс
                activeDevice = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                mOutputDevices = deviceEnum.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);

                foreach (MMDevice device in mOutputDevices)
                {
                    cmbOutput.Items.Add(device.FriendlyName);
                    if (device.DeviceID == activeDevice.DeviceID) cmbOutput.SelectedIndex = cmbOutput.Items.Count - 1;
                }

                string[] filename = File.ReadAllLines(fileInfo1.FullName);
                if (filename.Length == 1)
                {
                    Languages();
                }

                if (!File.Exists("log.tmp"))
                {
                    File.Create("log.tmp").Close();
                }
                else
                {
                    if (File.ReadAllLines("log.tmp").Length > 1000)
                    {
                        File.WriteAllText("log.tmp", " ");
                    }
                }

                if (langindex == "0")
                {
                    string msg = "Подключите проводную аудио-гарнитуру к компьютеру.\nЕсли на данный момент гарнитура не подключена,\nто подключите проводную гарнитуру, и перезапустите программу для того, чтобы звук подавался в наушники.";
                    MessageBox.Show(msg);
                }
                else
                {
                    string msg = "Connect a wired audio headset to your computer.\nIf a headset is not currently connected,\nthen connect a wired headset and restart the program so that the sound is played through the headphones.";
                    MessageBox.Show(msg);
                }

                btnRecordShadow.Opacity = 1;
            }
            catch (Exception ex)
            {
                if (langindex == "0")
                {
                    string msg = "Ошибка в Loaded: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
                else
                {
                    string msg = "Error in Loaded: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
            }
        }

        private void Mixer()
        {
            try
            {

                mMixer = new SimpleMixer(1, SampleRate) //стерео, 44,1 КГц
                {
                    //Right = true,
                    //Left = true,
                    FillWithZeros = true,
                    DivideResult = true, //Для этого установлено значение true, чтобы избежать звуков тиков из-за превышения -1 и 1.
                };
            }
            catch (Exception ex)
            {
                if (langindex == "0")
                {
                    string msg = "Ошибка в Mixer: \r\n" + ex.Message;
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
                else
                {
                    string msg = "Error in Mixer: \r\n" + ex.Message;
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
            }
        }

        private async void Recording()
        {
            try
            {
                StreamReader FileRecord = new StreamReader("Data_Create.tmp");
                StreamReader FileCutRecord = new StreamReader("Data_cutCreate.tmp");
                myfile = FileRecord.ReadToEnd();
                cutmyfile = FileCutRecord.ReadToEnd();
                //NFTRecordClick = 1;
                //myfile = "MyRecord1.wav";
                //cutmyfile = "cutMyRecord1.wav";
                FileRecord.Close();
                FileCutRecord.Close();
                /*if (File.Exists(myfile))
                {
                    File.Delete(myfile);
                }
                if (File.Exists(cutmyfile))
                {
                    File.Delete(cutmyfile);
                }*/
                using (mSoundIn = new WasapiCapture())
                {
                    mSoundIn.Device = mInputDevices[cmbInput.SelectedIndex];
                    mSoundIn.Initialize();
                    lbRecordPB.Visibility = Visibility.Visible;
                    mSoundIn.Start();
                    using (WaveWriter record = new WaveWriter(cutmyfile, mSoundIn.WaveFormat))
                    {
                        mSoundIn.DataAvailable += (s, data) => record.Write(data.Data, data.Offset, data.ByteCount);
                        for (int i = 0; i < 100; i++)
                        {
                            pbRecord.Value++;
                            await Task.Delay(40);
                            if (pbRecord.Value == 25)
                            {
                                string uri1 = @"Neurotuners\progressbar\Group 13.png";
                                ImgPBRecordBack.ImageSource = new ImageSourceConverter().ConvertFromString(uri1) as ImageSource;
                            }
                            else if (pbRecord.Value == 50)
                            {
                                string uri2 = @"Neurotuners\progressbar\Group 12.png";
                                ImgPBRecordBack.ImageSource = new ImageSourceConverter().ConvertFromString(uri2) as ImageSource;
                            }
                            else if (pbRecord.Value == 75)
                            {
                                string uri3 = @"Neurotuners\progressbar\Group 11.png";
                                ImgPBRecordBack.ImageSource = new ImageSourceConverter().ConvertFromString(uri3) as ImageSource;
                            }
                            else if (pbRecord.Value == 95)
                            {
                                string uri4 = @"Neurotuners\progressbar\Group 10.png";
                                ImgPBRecordBack.ImageSource = new ImageSourceConverter().ConvertFromString(uri4) as ImageSource;
                            }
                        }
                        //Thread.Sleep(5000);

                        mSoundIn.Stop();
                        lbRecordPB.Visibility = Visibility.Hidden;
                        pbRecord.Value = 0;

                    }
                    Thread.Sleep(100);
                    string uri = @"Neurotuners\element\progressbar-backgrnd1.png";
                    ImgPBRecordBack.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
                    int[] Rdat = new int[150000];
                    int Ndt;
                    Ndt = vizualzvuk(cutmyfile, myfile, Rdat, 1);
                    //NFT_drawing1(myfile);
                    //File.Move(myfile, @"Record\" + myfile);
                    //CutRecord cutRecord = new CutRecord();
                    //cutRecord.CutFromWave(cutmyfile, myfile, start, end);

                }
                if (langindex == "0")
                {
                    ImgBtnRecordClick = 0;
                    string uri = @"Neurotuners\button\button-record-inactive.png";
                    ImgRecordingBtn.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
                    btnRecord.IsEnabled = false;
                    string msg = "Запись и обработка завершена.";
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                }
                else
                {
                    ImgBtnRecordClick = 0;
                    string uri = @"Neurotuners\button\button-record-inactive.png";
                    ImgRecordingBtn.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
                    btnRecord.IsEnabled = false;
                    string msg = "Recording and processing completed.";
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                }
            }
            catch (Exception ex)
            {
                if (langindex == "0")
                {
                    string msg = "Ошибка в Recording: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
                else
                {
                    string msg = "Error in Recording: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
            }
        }

        private void Languages()
        {
            try
            {
                StreamReader FileLanguage = new StreamReader("Data_Language.tmp");
                File.WriteAllText("Data_Load.tmp", "1");
                File.WriteAllText("DataTemp.tmp", "0");
                langindex = FileLanguage.ReadToEnd();
                if (langindex == "0")
                {
                    btnRecord.ToolTip = "Запись";
                    btnSettings.ToolTip = "Настройки";
                    lbSetMicrophone.Content = "Выбор микрофона";
                    lbSetSpeaker.Content = "Выбор динамиков";
                    Title = "Диагностика голоса";
                }
                else
                {
                    btnRecord.ToolTip = "Record";
                    btnSettings.ToolTip = "Settings";
                    lbSetMicrophone.Content = "Microphone selection";
                    lbSetSpeaker.Content = "Speaker selection";
                    Title = "Voice diagnostics";
                }
            }
            catch (Exception ex)
            {
                if (langindex == "0")
                {
                    string msg = "Ошибка в Languages: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
                else
                {
                    string msg = "Error in Languages: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
            }
        } 
    }
}
