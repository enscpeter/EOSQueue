using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EOSDigital.API;
using EOSDigital.SDK;
using System.Threading.Tasks;
using System.Reflection;
using MahApps.Metro.Controls;

namespace WpfExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public class setting {

        string EXP;
        string ISO;
        string AP;

        public setting() {
            EXP = null;
            ISO = null;
            AP = null;
        }

        public setting(string expo, string sens, string aper) {
            EXP = expo;
            ISO = sens;
            AP = aper;
        }

        public void setEXP(string expo) {
            EXP = expo;
        }

        public void setISO(string sens)
        {
            ISO = sens;
        }

        public void setAP(string aper) {
            AP = aper;
        }

        public string getEXP() {
            return EXP;
        }

        public string getISO()
        {
            return ISO;
        }

        public string getAP()
        {
            return AP;
        }

    }

    public partial class MainWindow
    {
        #region Variables

        CanonAPI APIHandler;
        Camera MainCamera;
        CameraValue[] AvList;
        CameraValue[] TvList;
        CameraValue[] ISOList;
        List<Camera> CamList;
        bool IsInit = false;
        ImageBrush bgbrush = new ImageBrush();
        Action<BitmapImage> SetImageAction;
        System.Windows.Forms.FolderBrowserDialog SaveFolderBrowser = new System.Windows.Forms.FolderBrowserDialog();
        int ErrCount;
        object ErrLock = new object();

        #endregion

        #region Queue
        Queue q = new Queue();
        setting goset = new setting();
        // temporary strings to store values for manual settings
        string ISOtemp = null;
        string EXPtemp = null;
        string APtemp = null;
        //TextReader textread = new StreamReader(@"script.txt");
        //string queueText = textread.ReadLine();
        #endregion

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                APIHandler = new CanonAPI();
                APIHandler.CameraAdded += APIHandler_CameraAdded;
                ErrorHandler.SevereErrorHappened += ErrorHandler_SevereErrorHappened;
                ErrorHandler.NonSevereErrorHappened += ErrorHandler_NonSevereErrorHappened;
                SavePathTextBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "RemotePhoto");
                SetImageAction = (BitmapImage img) => { bgbrush.ImageSource = img; };
                SaveFolderBrowser.Description = "Save Images To...";
                RefreshCamera();
                IsInit = true;
            }
            catch (DllNotFoundException) { ReportError("Canon DLLs not found!", true); }
            catch (Exception ex) { ReportError(ex.Message, true); }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                IsInit = false;
                MainCamera?.Dispose();
                APIHandler?.Dispose();
            }
            catch (Exception ex) { ReportError(ex.Message, false); }
        }

        #region API Events

        private void APIHandler_CameraAdded(CanonAPI sender)
        {
            try { Dispatcher.Invoke((Action)delegate { RefreshCamera(); }); }
            catch (Exception ex) { ReportError(ex.Message, false); }
        }

        private void MainCamera_StateChanged(Camera sender, StateEventID eventID, int parameter)
        {
            try { if (eventID == StateEventID.Shutdown && IsInit) { Dispatcher.Invoke((Action)delegate { CloseSession(); }); } }
            catch (Exception ex) { ReportError(ex.Message, false); }
        }

        private void MainCamera_ProgressChanged(object sender, int progress)
        {
            try { MainProgressBar.Dispatcher.Invoke((Action)delegate { MainProgressBar.Value = progress; }); }
            catch (Exception ex) { ReportError(ex.Message, false); }
        }

        private void MainCamera_LiveViewUpdated(Camera sender, Stream img)
        {
            try
            {
                using (WrapStream s = new WrapStream(img))
                {
                    img.Position = 0;
                    BitmapImage EvfImage = new BitmapImage();
                    EvfImage.BeginInit();
                    EvfImage.StreamSource = s;
                    EvfImage.CacheOption = BitmapCacheOption.OnLoad;
                    EvfImage.EndInit();
                    EvfImage.Freeze();
                    Application.Current.Dispatcher.BeginInvoke(SetImageAction, EvfImage);
                }
            }
            catch (Exception ex) { ReportError(ex.Message, false); }
        }

        private void MainCamera_DownloadReady(Camera sender, DownloadInfo Info)
        {
            try
            {
                string dir = null;
                SavePathTextBox.Dispatcher.Invoke((Action)delegate { dir = SavePathTextBox.Text; });
                sender.DownloadFile(Info, dir);
                MainProgressBar.Dispatcher.Invoke((Action)delegate { MainProgressBar.Value = 0; });
            }
            catch (Exception ex) { ReportError(ex.Message, false); }
        }

        private void ErrorHandler_NonSevereErrorHappened(object sender, ErrorCode ex)
        {
            ReportError($"SDK Error code: {ex} ({((int)ex).ToString("X")})", false);
        }

        private void ErrorHandler_SevereErrorHappened(object sender, Exception ex)
        {
            ReportError(ex.Message, true);
        }

        #endregion

        #region Session

        private void OpenSessionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MainCamera?.SessionOpen == true) CloseSession();
                else OpenSession();
            }
            catch (Exception ex) { ReportError(ex.Message, false); }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try { RefreshCamera(); }
            catch (Exception ex) { ReportError(ex.Message, false); }
        }

        #endregion

        #region Settings

        private void AvCoBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (AvCoBox.SelectedIndex < 0) return;
                APtemp = (((string)AvCoBox.SelectedItem));
            }
            catch (Exception ex) { ReportError(ex.Message, false); }

        }

        private void TvCoBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (TvCoBox.SelectedIndex < 0) return;
                EXPtemp = (((string)TvCoBox.SelectedItem));
            }
            catch (Exception ex) { ReportError(ex.Message, false); }
        }

        private void ISOCoBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ISOCoBox.SelectedIndex < 0) return;
                ISOtemp = (((string)ISOCoBox.SelectedItem));
            }
            catch (Exception ex) { ReportError(ex.Message, false); }

        }

        private void Queue_Click(object sender, RoutedEventArgs e)
        {
            setting set = new setting(EXPtemp, ISOtemp, APtemp);  // create new class instance for each photo to be taken
            q.Enqueue(set); // queue class instance
            QueueList.Text += ((string)TvCoBox.SelectedItem) + "     " + ((string)AvCoBox.SelectedItem) + "     " + ((string)ISOCoBox.SelectedItem) + Environment.NewLine;
            // display queued setting in textbox
        }

        public async Task WaitAsynchronously() {
            await Task.Delay(60000); //60s delay until next capture sequence
        }


        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            int totalimg = q.Count;
            OverallProg.Maximum = totalimg;
            OverallProg.Minimum = 0;

            while (q.Count > 0)
            {
                try
                {
                    goset = (setting)q.Dequeue(); //do not update goset with new value if an exposure was not taken due to failure
                    MainCamera.SetSetting(PropertyID.Tv, TvValues.GetValue(goset.getEXP()).IntValue); // set exposure
                    MainCamera.SetSetting(PropertyID.Av, AvValues.GetValue(goset.getAP()).IntValue); // set aperture
                    MainCamera.SetSetting(PropertyID.ISO, ISOValues.GetValue(goset.getISO()).IntValue); // set ISO sensitivity
                    MainCamera.TakePhotoAsync(); // capture image
                    statustext.Text = "Capturing image " + (totalimg - q.Count) + " of " + totalimg;
                }
                catch (Exception ex)
                {
                    ReportError(ex.Message, false);
                    break; // stop while loop if error arises     
                }
                finally
                {
                    OverallProg.Value = totalimg - q.Count;
                    await WaitAsynchronously();

                }
            }
            statustext.Text = "Complete";
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(SavePathTextBox.Text)) SaveFolderBrowser.SelectedPath = SavePathTextBox.Text;
                if (SaveFolderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    SavePathTextBox.Text = SaveFolderBrowser.SelectedPath;
                }
            }
            catch (Exception ex) { ReportError(ex.Message, false); }
        }

        #endregion

        #region Live view

        private void StarLVButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!MainCamera.IsLiveViewOn)
                {
                    LVCanvas.Background = bgbrush;
                    MainCamera.StartLiveView();
                    StarLVButton.Content = "Stop LV";
                }
                else
                {
                    MainCamera.StopLiveView();
                    StarLVButton.Content = "Start LV";
                    LVCanvas.Background = Brushes.LightGray;
                }
            }
            catch (Exception ex) { ReportError(ex.Message, false); }
        }

        #endregion`

        #region Subroutines

        private void CloseSession()
        {
            MainCamera.CloseSession();
            AvCoBox.Items.Clear();
            TvCoBox.Items.Clear();
            ISOCoBox.Items.Clear();
            QueueList.Text = "";
            SettingsGroupBox.IsEnabled = false;
            LiveViewGroupBox.IsEnabled = false;
            SessionButton.Content = "Open Session";
            SessionLabel.Content = "No open session";
            StarLVButton.Content = "Start LV";
            button.IsEnabled = false;
            BrowseButton.IsEnabled = false;
            q.Clear();
        }

        private void RefreshCamera()
        {
            CameraListBox.Items.Clear();
            CamList = APIHandler.GetCameraList();
            foreach (Camera cam in CamList) CameraListBox.Items.Add(cam.DeviceName);
            if (MainCamera?.SessionOpen == true) CameraListBox.SelectedIndex = CamList.FindIndex(t => t.ID == MainCamera.ID);
            else if (CamList.Count > 0) CameraListBox.SelectedIndex = 0;
        }

        private void OpenSession()
        {
            if (CameraListBox.SelectedIndex >= 0)
            {
                MainCamera = CamList[CameraListBox.SelectedIndex];
                MainCamera.OpenSession();
                MainCamera.LiveViewUpdated += MainCamera_LiveViewUpdated;
                MainCamera.ProgressChanged += MainCamera_ProgressChanged;
                MainCamera.StateChanged += MainCamera_StateChanged;
                MainCamera.DownloadReady += MainCamera_DownloadReady;

                if (IsInit)
                {
                    try
                    {
                        MainCamera.SetSetting(PropertyID.SaveTo, (int)SaveTo.Host); //new
                        MainCamera.SetCapacity(); //new
                        BrowseButton.IsEnabled = true;
                        button.IsEnabled = true; //new
                    }
                    catch (Exception ex) { ReportError(ex.Message, false); }
                }

                SessionButton.Content = "Close Session";
                SessionLabel.Content = MainCamera.DeviceName;
                AvList = MainCamera.GetSettingsList(PropertyID.Av);
                TvList = MainCamera.GetSettingsList(PropertyID.Tv);
                ISOList = MainCamera.GetSettingsList(PropertyID.ISO);
                foreach (var Av in AvList) AvCoBox.Items.Add(Av.StringValue);
                foreach (var Tv in TvList) TvCoBox.Items.Add(Tv.StringValue);
                foreach (var ISO in ISOList) ISOCoBox.Items.Add(ISO.StringValue);
                AvCoBox.SelectedIndex = AvCoBox.Items.IndexOf(AvValues.GetValue(MainCamera.GetInt32Setting(PropertyID.Av)).StringValue);
                TvCoBox.SelectedIndex = TvCoBox.Items.IndexOf(TvValues.GetValue(MainCamera.GetInt32Setting(PropertyID.Tv)).StringValue);
                ISOCoBox.SelectedIndex = ISOCoBox.Items.IndexOf(ISOValues.GetValue(MainCamera.GetInt32Setting(PropertyID.ISO)).StringValue);
                SettingsGroupBox.IsEnabled = true;
                LiveViewGroupBox.IsEnabled = true;
                //SaveFolderBrowser.SelectedPath = SavePathTextBox.Text; //new
            }
        }

        private void ReportError(string message, bool lockdown)
        {
            int errc;
            lock (ErrLock) { errc = ++ErrCount; }

            if (lockdown) EnableUI(false);

            if (errc < 4) MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else if (errc == 4) MessageBox.Show("Many errors happened!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            lock (ErrLock) { ErrCount--; }
        }

        private void EnableUI(bool enable)
        {
            if (!Dispatcher.CheckAccess()) Dispatcher.Invoke((Action)delegate { EnableUI(enable); });
            else
            {
                SettingsGroupBox.IsEnabled = enable;
                InitGroupBox.IsEnabled = enable;
                LiveViewGroupBox.IsEnabled = enable;
            }
        }

        #endregion

        private void button_Click(object sender, RoutedEventArgs e)
        {
            int count = 0;
            var ofd = new Microsoft.Win32.OpenFileDialog() { Filter = "Text Files (*.txt)|*.txt" };
            var result = ofd.ShowDialog();
            if (result == false) return;
            //ofd.FileName;

            try
            {
                Assembly curr_assembly = Assembly.GetExecutingAssembly();

                StreamReader readtxt = new StreamReader(ofd.FileName); //access emebed resource text file
                while (readtxt.Peek() >= 0) // read line by line until none are left
                {
                    string[] TempParam = readtxt.ReadLine().Split(' '); // parse strings using space as a delimiter 
                    foreach (string param in TempParam) // iterate through each element of parsed string array
                    {
                        if (count == 0)
                        {
                            EXPtemp = param; // store first element of line as temporary exposure
                            if (EXPtemp == "20\"" || EXPtemp == "10\"" || EXPtemp == "6\"" || EXPtemp == "0\"3" || EXPtemp == "1/6" || EXPtemp == "1/10" || EXPtemp == "1/20")
                            {
                                EXPtemp += " (1/3)"; // append (1/3) to the expsoures that require it
                            }
                            count++; // increment count to access next value (aperture)
                        }
                        else if (count == 1)
                        {
                            APtemp = param; // store second element of line as temporary aperture size
                            if (APtemp == "3.5" || APtemp == "13")
                            {
                                APtemp += " (1/3)"; // append (1/3) to the fstops that require it
                            }
                            count++; // increment count to access next value (ISO)
                        }
                        else if (count == 2)
                        {
                            ISOtemp = "ISO " + param; // store last element of line as a temporary ISO
                            count = 0; // reset count to 0 so that the first element of the NEXT line will be accessed
                            setting setf = new setting(EXPtemp, ISOtemp, APtemp);  // create new class instance for each photo to be taken
                            q.Enqueue(setf); // queue class instance
                            QueueList.Text += EXPtemp + "     " + APtemp + "     " + ISOtemp + Environment.NewLine; // display queue in textbox
                        }
                    }
                }
            }
            catch
            {
                //placeholder until I find out what I should do
            }
        }
    }
}