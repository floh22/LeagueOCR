//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using CaptureSampleCore;
using Composition.WindowsRuntimeHelpers;
using Microsoft.Owin;
using OCR;
using Server;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.UI.Composition;

[assembly: OwinStartup(typeof(Startup))]

namespace LoLOCRHub
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IntPtr hwnd;
        private Compositor compositor;
        private CompositionTarget target;
        private ContainerVisual root;

        DispatcherTimer findLoLTimer, OCREngineTimer;

        private BasicSampleApplication sample;
        private ObservableCollection<Process> processes;
        private Process currentProcess = null;

        private List<Task> OCRTasks;
        private Server.HttpServer HttpServer;

        private System.Drawing.Size actualSize;
        private double dpiX = 1.0;
        private double dpiY = 1.0;

        private bool finishedInit = false;
        public MainWindow()
        {
            InitializeComponent();
            StartTimers();
            OCRTasks = new List<Task>();
        }

        //Window Functions

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var interopWindow = new WindowInteropHelper(this);
            hwnd = interopWindow.Handle;

            var presentationSource = PresentationSource.FromVisual(this);
            if (presentationSource != null)
            {
                dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
                dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
            }
            var controlsWidth = (float)(ControlsGrid.ActualWidth * dpiX);

            //actualSize = new System.Drawing.Size((int)((MainWindow.ActualWidth - 200) * dpiX), (int)(MainWindow.ActualHeight * dpiY);
            actualSize = new System.Drawing.Size((int)Main.ActualWidth - 200, (int)Main.ActualHeight);

            InitComposition(controlsWidth);
            InitCaptureComponent();
            InitWindowList();
        }

        void Window_Closing(object sender, CancelEventArgs e)
        {
            if (sample.CapturingLoL)
            {
                string msg = "LoL currently running. Stop game analysis?";
                MessageBoxResult result =
                  MessageBox.Show(
                    msg,
                    "LeagueOCR",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    // If user doesn't want to close, cancel closure
                    e.Cancel = true;
                    return;
                }
            }

            OCRTasks.ForEach((t) => { if (t.IsCompleted) t.Dispose(); });
        }

        private void ScaleChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!finishedInit)
                return;
            if (sender is Slider slider)
            {
                sample.UpdateScale((float)slider.Value);
            }
        }

        private void UpdateWindowSize(object sender, System.Drawing.Size e)
        {
            e = new System.Drawing.Size((int)(e.Width * sample.RenderScale), (int)(e.Height * sample.RenderScale));
            Main.MaxWidth = (e.Width + 216) * dpiX;
            Main.MinWidth = (e.Width + 216) * dpiX;
            Main.MaxHeight = (e.Height + 39) * dpiY;
            Main.MinHeight = (e.Height + 39) * dpiY;
        }

        private void ResetWindowSize(object sender, EventArgs e)
        {
            //Update Window
            Main.MaxWidth = 800;
            Main.MinWidth = 800;
            Main.MaxHeight = 450;
            Main.MinHeight = 450;

            StopCapture();
        }

        private void InitWindowList()
        {
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                var processesWithWindows = from p in Process.GetProcesses()
                                           where !string.IsNullOrWhiteSpace(p.MainWindowTitle) && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle)
                                           select p;
                processes = new ObservableCollection<Process>(processesWithWindows);
                WindowComboBox.ItemsSource = processes;
            }
            else
            {
                WindowComboBox.IsEnabled = false;
            }
        }

        private void InitComposition(float controlsWidth)
        {
            // Create the compositor.
            compositor = new Compositor();

            // Create a target for the window.
            target = compositor.CreateDesktopWindowTarget(hwnd, true);

            // Attach the root visual.
            root = compositor.CreateContainerVisual();
            root.RelativeSizeAdjustment = Vector2.One;
            root.Size = new Vector2(-controlsWidth, 0);
            root.Offset = new Vector3(controlsWidth, 0, 0);
            target.Root = root;


        }

        //Window Capture Functions

        private void InitCaptureComponent()
        {
            // Setup the rest of the sample application.
            sample = new BasicSampleApplication(compositor, actualSize);
            sample.ContentSizeUpdated += UpdateWindowSize;
            root.Children.InsertAtTop(sample.Visual);
            sample.CaptureWindowClosed += ResetWindowSize;

            HttpServer = new HttpServer(3002);

            sample.BitmapCreated += FindValuesInLoL;

            FindLoLProcess(this, EventArgs.Empty);
        }

        private void StartHwndCapture(IntPtr hwnd, string pName)
        {
            GraphicsCaptureItem item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null)
            {
                sample.StartCaptureFromItem(item);

                if (pName.Equals("League of Legends (TM) Client")) {
                    //Capturing League. Start OCR and Data Server
                    sample.CapturingLeagueOfLegends();

                    //Starting doing OCR on the scene since we now know where to look for what
                    OCREngineTimer.Start();

                    //Start data server
                    HttpServer.AOIList = sample.GetAreasOfInterest();
                    HttpServer.StartServer();
                }
            }

            findLoLTimer.Stop();
            finishedInit = true;
        }

        private void StopCapture()
        {
            currentProcess = null;

            if (sample.CapturingLoL)
            {
                //Was capturing league. Stop OCR and Data Server
                OCREngineTimer.Stop();
                HttpServer.StopServer();
                sample.CapturingLoL = false;
            }
            sample.StopCapture();
            if (!findLoLTimer.IsEnabled)
                findLoLTimer.Start();
        }

        private void FindLoLProcess(object sender, EventArgs e)
        {
            if (!ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
                return;
            var processesWithWindows = new ObservableCollection<Process>(from p in Process.GetProcesses()
                                                                         where !string.IsNullOrWhiteSpace(p.MainWindowTitle) && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle)
                                                                         where p.MainWindowTitle == "League of Legends (TM) Client"
                                                                         select p);
            if (processesWithWindows.Count > 0)
            {
                sample.StopCapture();
                Process first = processesWithWindows.First();
                StartHwndCapture(first.MainWindowHandle, first.MainWindowTitle);
                currentProcess = first;
            }
        }

        private void FindValuesInLoL(object sender, Bitmap bitmap)
        {

            OCRTasks.ForEach((t) => { if (t.IsCompleted) t.Dispose(); });
            OCRTasks.Add(Task.Factory.StartNew(() =>
            {
                HttpServer.AOIList = sample.GetAreasOfInterest();
                HttpServer.AOIList.GetAllAreaOfInterests().ForEach((aoi) =>
                {
                    var engine = new OCREngine();
                    aoi.CurrentContent = engine.GetTextInSubregion(bitmap, aoi.Rect);
                });
            }));


        }

        private void RequestUpdatedBitmap(object sender, EventArgs e)
        {
            sample.RequestCurrentBitmap();
        }

        //UI Elements

        private void ShowPreviewButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ShowPreviewButton_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopCapture();
            WindowComboBox.SelectedIndex = -1;
        }

        private void ESportsTimerButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ESportsTimerButton_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void WindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var process = (Process)comboBox.SelectedItem;

            if (process != null)
            {
                StopCapture();
                var hwnd = process.MainWindowHandle;
                try
                {
                    StartHwndCapture(hwnd, process.MainWindowTitle);
                    currentProcess = process;
                }
                catch (Exception)
                {
                    Debug.WriteLine($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");
                    processes.Remove(process);
                    comboBox.SelectedIndex = -1;
                }
            }
        }

        //Timer Init
        private void StartTimers()
        {
            findLoLTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            findLoLTimer.Tick += FindLoLProcess;
            findLoLTimer.Start();

            OCREngineTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            OCREngineTimer.Tick += RequestUpdatedBitmap;
        }
    }
}
