using CSCore;
using CSCore.Codecs;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.SoundOut;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private IWaveSource mSource;
        private ISampleSource mMp3;
        private SimpleMixer mMixer, mMixerRight;
        private int SampleRate;//44100;
        //private Equalizer equalizer;
        private WasapiOut mSoundOut;
        private WasapiCapture mSoundIn;
        private SampleDSP mDsp;
        private MMDeviceCollection mOutputDevices;
        private MMDeviceCollection mInputDevices;

        string myfile, myfileDel;
        string cutmyfile, cutmyfileDel, fileDeleteRec1, fileDeleteCutRec1;
        string langindex;
        private int BtnSetClick = 0, ImgBtnRecordClick = 0, count = 0, indStop = 0;

        private static string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static string pathDesk = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private static string path2;

        BackgroundWorker worker;

        public MainWindow()
        {
            InitializeComponent();

            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    if (worker.CancellationPending == true)
                    {
                        //e.Cancel = true;
                        (sender as BackgroundWorker).ReportProgress(100);
                        break;
                        //return;
                    }
                    (sender as BackgroundWorker).ReportProgress(i);
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                if (langindex == "0")
                {
                    string msg = "Ошибка в worker_DoWork: \r\n" + ex.Message;
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
                else
                {
                    string msg = "Error in worker_DoWork: \r\n" + ex.Message;
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                PBNFT.Value = e.ProgressPercentage;
                if (PBNFT.Value == 100)
                {
                    if (!File.Exists(pathDesk + @"\MyRecord" + count + ".bmp"))
                    {
                        SaveToBmp(Image1, pathDesk + @"\MyRecord" + count + ".bmp");
                        if (langindex == "0")
                        {
                            string msg = "NFT картинка сохранена на рабочий стол.";
                            LogClass.LogWrite(msg);
                            MessageBox.Show(msg);
                        }
                        else
                        {
                            string msg = "NFT picture saved to desktop.";
                            LogClass.LogWrite(msg);
                            MessageBox.Show(msg);
                        }
                    }
                    //imgPBNFTBack.Visibility = Visibility.Hidden;
                }
            }
            catch (Exception ex)
            {
                if (langindex == "0")
                {
                    string msg = "Ошибка в worker_ProgressChanged: \r\n" + ex.Message;
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
                else
                {
                    string msg = "Error in worker_ProgressChanged: \r\n" + ex.Message;
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
            }
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
            if (WinInf.RecInd == 1)
            {
                ImgBtnRecordClick = 1;
                string uri = @"VoiceDiagnostics\button\button-record-active.png";
                ImgRecordingBtn.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
                pbRecord.Visibility = Visibility.Visible;
                btnSettings.IsEnabled = false;
                btnAudition.IsEnabled = false;
                Recording1();
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
            else
            {
                WinInf win = new WinInf();
                win.ShowDialog();
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
                    string msg = "Подключите проводную аудио-гарнитуру к компьютеру.\nЕсли на данный момент гарнитура не подключена,\nто подключите проводную гарнитуру, и перезапустите программу для того, чтобы звук подавался в наушники.\nДля записи гарнитура необязательна, но обязательно нужно чтобы была тишина вокруг.";
                    MessageBox.Show(msg);
                }
                else
                {
                    string msg = "Connect a wired audio headset to your computer.\nIf a headset is not currently connected,\nthen connect a wired headset and restart the program so that the sound is played through the headphones.\nA headset is not required for recording, but it is imperative that there is silence around.";
                    MessageBox.Show(msg);
                }

                btnRecordShadow.Opacity = 1;
                btnAudition.IsEnabled = false;
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
        
        private ChannelMask SoundOut()
        {
            try
            {

                mSoundOut = new WasapiOut(/*false, AudioClientShareMode.Exclusive, 1*/);
                Dispatcher.Invoke(() => mSoundOut.Device = mOutputDevices[cmbOutput.SelectedIndex]);
                //mSoundOut.Device = mOutputDevices[cmbOutput.SelectedIndex];



                mSoundOut.Initialize(mMixer.ToWaveSource(32).ToMono());


                mSoundOut.Play();
                mSoundOut.Volume = 10;
                return ChannelMask.SpeakerFrontLeft;
            }
            catch (Exception ex)
            {
                if (langindex == "0")
                {
                    string msg = "Ошибка в SoundOut: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                    return ChannelMask.SpeakerFrontLeft;
                }
                else
                {
                    string msg = "Error in SoundOut: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                    return ChannelMask.SpeakerFrontLeft;
                }
            }
        }

        private void Stop()
        {
            try
            {
                if (mMixer != null)
                {
                    mMixer.Dispose();
                    mMp3.ToWaveSource(32).Loop().ToSampleSource().Dispose();
                    mMixer = null;
                }
                if (mSoundOut != null)
                {
                    mSoundOut.Stop();
                    mSoundOut.Dispose();
                    mSoundOut = null;
                }
                if (mSoundIn != null)
                {
                    mSoundIn.Stop();
                    mSoundIn.Dispose();
                    mSoundIn = null;
                }
                if (mSource != null)
                {
                    mSource.Dispose();
                    mSource = null;
                }
                if (mMp3 != null)
                {
                    mMp3.Dispose();
                    mMp3 = null;
                }
            }
            catch (Exception ex)
            {
                /*if (langindex == "0")
                {
                    string msg = "Ошибка в Stop: \r\n" + ex.Message;
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
                else
                {
                    string msg = "Error in Stop: \r\n" + ex.Message;
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }*/
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

        private void TimerRec()
        {
            int i = 3;
            while(i > 0)
            {
                Dispatcher.Invoke(() => lbTimer.Content = i.ToString());
                Thread.Sleep(1000);
                i--;
            }
            Dispatcher.Invoke(() => lbTimer.Content = i.ToString());
        }

        private async void VoiceDiagnostics_Activated(object sender, EventArgs e)
        {
            if(WinInf.RecInd == 1)
            {
                lbTimer.Visibility = Visibility.Visible;
                await Task.Run(() => TimerRec());
                lbTimer.Visibility = Visibility.Hidden;
                WinInf.RecInd = 0;
                ImgBtnRecordClick = 1;
                string uri = @"VoiceDiagnostics\button\button-record-active.png";
                ImgRecordingBtn.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
                pbRecord.Visibility = Visibility.Visible;
                btnSettings.IsEnabled = false;
                btnAudition.IsEnabled = false;
                btnRecord.IsEnabled = false;
                Recording1();
                if (langindex == "0")
                {
                    LogClass.LogWrite("Начало записи голоса.");
                }
                else
                {
                    LogClass.LogWrite("Start voice recording.");
                }
            }
        }

        private async void Recording1()
        {
            try
            {
                //StreamReader FileRecord = new StreamReader("Data_Create.tmp");
                //StreamReader FileCutRecord = new StreamReader("Data_cutCreate.tmp");
                //myfile = FileRecord.ReadToEnd();
                //cutmyfile = FileCutRecord.ReadToEnd();
                //NFTRecordClick = 1;
                while(File.Exists(pathDesk + @"\MyRecord" + count + ".bmp"))
                {
                    count++;
                }
                myfile = "MyRecord" + count + ".wav";
                cutmyfile = "cutMyRecord" + count + ".wav";
                fileDeleteRec1 = myfile;
                fileDeleteCutRec1 = cutmyfile;
                if (count != 0)
                {
                    count--;
                    myfileDel = "MyRecord" + count + ".wav";
                    cutmyfileDel = "cutMyRecord" + count + ".wav";
                    count++;
                }
                //fileDeleteRec1 = myfile;
                //fileDeleteCutRec1 = cutmyfile;
                //FileRecord.Close();
                //FileCutRecord.Close();
                if (File.Exists(myfileDel))
                {
                    
                    File.Delete(myfileDel);
                }
                if (File.Exists(cutmyfileDel))
                {
                    File.Delete(cutmyfileDel);
                }
                using (mSoundIn = new WasapiCapture())
                {
                    mSoundIn.Device = mInputDevices[cmbInput.SelectedIndex];
                    mSoundIn.Initialize();

                    mSoundIn.Start();
                    lbRecordPB.Visibility = Visibility.Visible;
                    using (WaveWriter record = new WaveWriter(cutmyfile, mSoundIn.WaveFormat))
                    {
                        mSoundIn.DataAvailable += (s, data) => record.Write(data.Data, data.Offset, data.ByteCount);
                        for (int i = 0; i < 100; i++)
                        {
                            pbRecord.Value++;
                            await Task.Delay(35);
                            if (pbRecord.Value == 25)
                            {
                                string uri1 = @"VoiceDiagnostics\progressbar\Group 13.png";
                                ImgPBRecordBack.ImageSource = new ImageSourceConverter().ConvertFromString(uri1) as ImageSource;
                            }
                            else if (pbRecord.Value == 50)
                            {
                                string uri2 = @"VoiceDiagnostics\progressbar\Group 12.png";
                                ImgPBRecordBack.ImageSource = new ImageSourceConverter().ConvertFromString(uri2) as ImageSource;
                            }
                            else if (pbRecord.Value == 75)
                            {
                                string uri3 = @"VoiceDiagnostics\progressbar\Group 11.png";
                                ImgPBRecordBack.ImageSource = new ImageSourceConverter().ConvertFromString(uri3) as ImageSource;
                            }
                            else if (pbRecord.Value == 95)
                            {
                                string uri4 = @"VoiceDiagnostics\progressbar\Group 10.png";
                                ImgPBRecordBack.ImageSource = new ImageSourceConverter().ConvertFromString(uri4) as ImageSource;
                            }
                        }
                        //Thread.Sleep(5000);

                        mSoundIn.Stop();
                        lbRecordPB.Visibility = Visibility.Hidden;
                        pbRecord.Value = 0;
                        pbRecord.Visibility = Visibility.Hidden;

                    }
                    Thread.Sleep(100);
                    string uri = @"VoiceDiagnostics\progressbar\progressbar-backgrnd.png";
                    ImgPBRecordBack.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
                    int[] Rdat = new int[150000];
                    int Ndt;
                    Ndt = vizualzvuk(cutmyfile, myfile, Rdat, 1);
                    NFT_drawing1(myfile);
                    //File.Move(myfile, @"Record\" + myfile);
                    //CutRecord cutRecord = new CutRecord();
                    //cutRecord.CutFromWave(cutmyfile, myfile, start, end);

                }
                if (langindex == "0")
                {
                    ImgBtnRecordClick = 0;
                    string uri = @"VoiceDiagnostics\button\button-record-inactive.png";
                    ImgRecordingBtn.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
                    //btnPlayer.IsEnabled = true;
                    btnRecordShadow.Opacity = 1;
                    btnRecord.IsEnabled = true;
                    btnSettings.IsEnabled = true;
                    btnAudition.IsEnabled = true;
                    string msg = "Запись и обработка завершена. Сейчас появится графическое изображение вашего голоса. Нажав на картинку вы сможете прослушать запись.";
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    //btnPlayerEffect.Opacity = 1;
                    //WinSkip skip = new WinSkip();
                    //skip.ShowDialog();
                }
                else
                {
                    ImgBtnRecordClick = 0;
                    string uri = @"VoiceDiagnostics\button\button-record-inactive.png";
                    ImgRecordingBtn.ImageSource = new ImageSourceConverter().ConvertFromString(uri) as ImageSource;
                    //btnPlayer.IsEnabled = true;
                    btnRecordShadow.Opacity = 0;
                    btnRecord.IsEnabled = true;
                    btnSettings.IsEnabled = true;
                    btnAudition.IsEnabled = true;
                    string msg = "Recording and processing completed. A graphic representation of your voice will now appear. You can listen to the recording by clicking on the picture.";
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    //btnPlayerEffect.Opacity = 1;
                }
            }
            catch (Exception ex)
            {
                if (langindex == "0")
                {
                    string msg = "Ошибка в Recording1: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
                else
                {
                    string msg = "Error in Recording1: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
            }
        }

        private void btnAudition_Click(object sender, RoutedEventArgs e)
        {
            indStop++;
            
            if (indStop == 1)
            {
                string msg = "При повторном нажатии на картинку запись остановится.";
                MessageBox.Show(msg);
                btnRecord.IsEnabled = false;
                btnSettings.IsEnabled = false;
                Audition();
            }
            else
            {
                btnRecord.IsEnabled = true;
                btnSettings.IsEnabled = true;
                Stop();
                indStop = 0;
            }
        }

        private void VoiceDiagnostics_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                Stop();
                if (File.Exists(fileDeleteRec1))
                {
                    File.Delete(fileDeleteRec1);
                }
                if (File.Exists(fileDeleteCutRec1))
                {
                    File.Delete(fileDeleteCutRec1);
                }
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                if (langindex == "0")
                {
                    string msg = "Ошибка в SimpleNeurotuner_Closing: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
                else
                {
                    string msg = "Error in SimpleNeurotuner_Closing: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
            }
        }

        private async void Audition()
        {
            try
            {
                //StreamReader FileRecord = new StreamReader("Data_Create.tmp");
                //myfile = FileRecord.ReadToEnd();
                //Stop();
                Mixer();
                mMp3 = CodecFactory.Instance.GetCodec(/*@"Record\" + */myfile).ToMono().ToSampleSource();
                mMixer.AddSource(mMp3.ChangeSampleRate(mMixer.WaveFormat.SampleRate).ToWaveSource(32).Loop().ToSampleSource());
                await Task.Run(() => SoundOut());
                //Block();
            }
            catch (Exception ex)
            {
                if (langindex == "0")
                {
                    string msg = "Ошибка в Audition1: \r\n" + ex.Message;
                    LogClass.LogWrite(msg);
                    MessageBox.Show(msg);
                    Debug.WriteLine(msg);
                }
                else
                {
                    string msg = "Error in Audition1: \r\n" + ex.Message;
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
                    lbRecordPB.Content = "Идёт запись...";
                    btnRecord.ToolTip = "Запись";
                    btnSettings.ToolTip = "Настройки";
                    lbSetMicrophone.Content = "Выбор микрофона";
                    lbSetSpeaker.Content = "Выбор динамиков";
                    Title = "Диагностика голоса";
                }
                else
                {
                    lbRecordPB.Content = "Recording in progress...";
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

        private async void NFT_drawing1(string filename)
        {
            int[] Rdat = new int[250000];
            int Ndt;
            int Ww, Hw, k, ik, dWw, dHw;
            worker.RunWorkerAsync();
            Ndt = await Task.Run(() =>
            {
                return vizualzvuk(filename, filename, Rdat, 0);
            });
            Hw = (int)Math.Sqrt(Ndt);
            Ww = (int)((double)(Ndt) / (double)(Hw) + 0.5);
            dWw = (int)((Image1.Width - (double)Ww) / 2.0) - 5;
            if (dWw < 0)
                dWw = 0;
            dHw = (int)((Image1.Height - (double)Hw) / 2.0) - 5;
            if (dHw < 0)
                dHw = 0;
            WriteableBitmap wb = new WriteableBitmap((int)Image1.Width, (int)Image1.Height, Ww, Hw, PixelFormats.Bgra32, null);

            // Define the update square (which is as big as the entire image).
            Int32Rect rect = new Int32Rect(0, 0, (int)Image1.Width, (int)Image1.Height);

            byte[] pixels = new byte[(int)Image1.Width * (int)Image1.Height * wb.Format.BitsPerPixel / 8];
            //Random rand = new Random();
            k = 0;
            ik = 0;
            int Wwt = 2, Hwt = 2, it0 = Ww / 2, jt0 = Hw / 2, it = 0, jt = 0;
            int R = 0, G = 0, B = 0, A = 0;
            int pixelOffset, poffp = 0, kt = 0;
            while (k < Ndt)
            {
                if (ik % 4 == 0)
                {
                    R = Rdat[3 * k];
                    G = Rdat[3 * k + 1];
                    B = Rdat[3 * k + 2];
                    A = 255;
                    pixelOffset = (dWw + it0 + it + (dHw + jt0 + jt) * wb.PixelWidth) * wb.Format.BitsPerPixel / 8;
                    pixels[pixelOffset] = (byte)B;
                    pixels[pixelOffset + 1] = (byte)G;
                    pixels[pixelOffset + 2] = (byte)R;
                    pixels[pixelOffset + 3] = (byte)A;
                    jt++;
                    if (jt == Hwt)
                    {
                        ik++;
                    }
                }
                else if (ik % 4 == 1)
                {
                    R = Rdat[3 * k];
                    G = Rdat[3 * k + 1];
                    B = Rdat[3 * k + 2];
                    A = 255;
                    pixelOffset = (dWw + it0 + it + (dHw + jt0 + jt) * wb.PixelWidth) * wb.Format.BitsPerPixel / 8;
                    pixels[pixelOffset] = (byte)B;
                    pixels[pixelOffset + 1] = (byte)G;
                    pixels[pixelOffset + 2] = (byte)R;
                    pixels[pixelOffset + 3] = (byte)A;
                    it++;
                    if (it == Wwt)
                    {
                        ik++;
                    }
                }
                else if (ik % 4 == 2)
                {
                    R = Rdat[3 * k];
                    G = Rdat[3 * k + 1];
                    B = Rdat[3 * k + 2];
                    A = 255;
                    pixelOffset = (dWw + it0 + it + (dHw + jt0 + jt) * wb.PixelWidth) * wb.Format.BitsPerPixel / 8;
                    pixels[pixelOffset] = (byte)B;
                    pixels[pixelOffset + 1] = (byte)G;
                    pixels[pixelOffset + 2] = (byte)R;
                    pixels[pixelOffset + 3] = (byte)A;
                    jt--;
                    if (jt == -1)
                    {
                        ik++;
                        //jt0--;
                    }
                }
                else
                {
                    R = Rdat[3 * k];
                    G = Rdat[3 * k + 1];
                    B = Rdat[3 * k + 2];
                    A = 255;
                    pixelOffset = (dWw + it0 + it + (dHw + jt0 + jt) * wb.PixelWidth) * wb.Format.BitsPerPixel / 8;
                    pixels[pixelOffset] = (byte)B;
                    pixels[pixelOffset + 1] = (byte)G;
                    pixels[pixelOffset + 2] = (byte)R;
                    pixels[pixelOffset + 3] = (byte)A;
                    it--;
                    if (it == -1)
                    {
                        it = 0;
                        jt = 0;
                        ik++;
                        it0--;
                        jt0--;
                        Hwt += 2;
                        Wwt += 2;
                    }
                }
                int stride = ((int)Image1.Width * wb.Format.BitsPerPixel) / 8;
                wb.WritePixels(rect, pixels, stride, 0);
                k++;
            }
            // Show the bitmap in an Image element.
            Image1.Source = wb;
            Image1.UpdateLayout();
            //NFTShadow = 1;
            //imgShadowNFT.Visibility = Visibility.Visible;

            worker.CancelAsync();
        }

        public static void SaveToBmp(FrameworkElement visual, string fileName)
        {
            var encoder = new BmpBitmapEncoder();
            SaveUsingEncoder(visual, fileName, encoder);
        }

        public static void SaveUsingEncoder(FrameworkElement visual, string fileName, BitmapEncoder encoder)
        {
            RenderTargetBitmap bitmap = new RenderTargetBitmap((int)visual.Width, (int)visual.Height, 99, 98, PixelFormats.Pbgra32);
            Size visualSize = new Size(visual.Width, visual.Height);
            visual.Measure(visualSize);
            visual.Arrange(new Rect(visualSize));
            bitmap.Render(visual);
            BitmapFrame frame = BitmapFrame.Create(bitmap);
            bitmap.Render(visual);
            encoder.Frames.Add(frame);

            using (var stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }
    }
}
