﻿//  ---------------------------------------------------------------------------------
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
using Common;
using Composition.WindowsRuntimeHelpers;
using Microsoft.Owin;
using Microsoft.Win32;
using OCR;
using Server;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.UI.Composition;
using static Common.Utils;
using static LoLOCRHub.Utils;

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

        DispatcherTimer findLoLTimer;
        private double OCRInterval = 1000;
        public long lastRequested;

        public static BasicSampleApplication sample;
        public static DataManager dataManager;
        private Server.HttpServer HttpServer;
        private List<OCREngine> engines;

        private System.Drawing.Size actualSize;
        private double dpiX = 1.0;
        private double dpiY = 1.0;

        private bool finishedInit;
        public static bool saveNextFrame;
        public static bool showPreview;
        public static string saveName;

        private static EventHandler<FinishOCREventArgs> OCRFinished;

        private bool doIM;

        private struct ObjectiveRequest
        {
            public AreaOfInterest aoi;
            public Server.Models.Objective o;

            public ObjectiveRequest(AreaOfInterest aoi, Server.Models.Objective o)
            {
                this.aoi = aoi;
                this.o = o;
            }
        }
        //Not a nice solution but it'll have to do for now
        private Dictionary<ObjectiveRequest, int> currentObjectiveRequests = new Dictionary<ObjectiveRequest, int>();

        private enum PerformanceLevel
        {
            Low = 1,
            Medium = 5,
            High = 15
        }

        private PerformanceLevel perfLevel;

        public MainWindow()
        {
            InitializeComponent();
            StartTimers();
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

            if(ShowPreviewButton.IsChecked.Value)
            {
                //actualSize = new System.Drawing.Size((int)((MainWindow.ActualWidth - 200) * dpiX), (int)(MainWindow.ActualHeight * dpiY);
                actualSize = new System.Drawing.Size((int)Main.ActualWidth - 200, (int)Main.ActualHeight);
                showPreview = true;
            }
            dataManager = new DataManager();

            InitComposition(controlsWidth);
            InitCaptureComponent();
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

        private void UpdateWindowSize(System.Drawing.Size e)
        {
            if (!showPreview)
                return;
            e = new System.Drawing.Size((int)(e.Width * sample.RenderScale), (int)(e.Height * sample.RenderScale));
            Main.MaxWidth = (e.Width + 216) * dpiX;
            Main.MinWidth = (e.Width + 216) * dpiX;
            Main.MaxHeight = (e.Height + 39) * dpiY;
            Main.MinHeight = (e.Height + 39) * dpiY;
        }

        private void UpdateWindowSize(object sender, System.Drawing.Size e)
        {
            UpdateWindowSize(e);
        }

        private void ResetWindowSize(object sender, EventArgs e)
        {
            if(showPreview)
            {
                //Update Window
                Main.MaxWidth = 800;
                Main.MinWidth = 800;
                Main.MaxHeight = 450;
                Main.MinHeight = 450;
            }
            StopCapture();
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
            sample = new BasicSampleApplication(compositor, actualSize, showPreview);
            sample.ContentSizeUpdated += UpdateWindowSize;
            root.Children.InsertAtTop(sample.Visual);
            sample.CaptureWindowClosed += ResetWindowSize;
            sample.CaptureWindowClosed += StopCapture;

            HttpServer = new HttpServer(3002);

            //by default only linear behavior
            HttpServer.OnlyIncreaseGold = false;

            sample.BitmapCreated += FindValues;

            FindLoLProcess(this, EventArgs.Empty);

            perfLevel = (PerformanceLevel)Enum.Parse(typeof(PerformanceLevel), ConfigurationManager.AppSettings["PerformanceLevel"].ToString(), true);

            engines = new List<OCREngine>();
            for (int i = 0; i < AOIList.OCRAreaOfInterestCount() + 1; i++)
            {
                engines.Add(new OCREngine());
            }

            Console.WriteLine("Started " + engines.Count + " OCR Engine Instances");

            OCRFinished += OCRThreadsFinished;
            sample.BitmapCreatedImmediate += GetTeamForObjectiveLate;
        }

        private void StartHwndCapture(IntPtr hwnd, string pName)
        {
            GraphicsCaptureItem item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null)
            {
                sample.StartCaptureFromItem(item);

                if (pName.Equals("League of Legends (TM) Client"))
                {
                    //Capturing League. Start OCR and Data Server
                    sample.CapturingLeagueOfLegends(ESportsTimerButton.IsChecked.Value);

                    //Starting doing OCR on the scene since we now know where to look for what
                    CustomTimer.Instance.Start();

                    //Make sure to get dragon Types on startup
                    doIM = true;

                    //Start data server
                    HttpServer.StartServer();
                }
            }

            findLoLTimer.Stop();
            finishedInit = true;
            lastRequested = DateTime.Now.Ticks;
        }

        private void StopCapture()
        {

            if (sample.CapturingLoL)
            {
                //Was capturing league. Stop OCR and Data Server
                CustomTimer.Instance.Stop();
                HttpServer.StopServer();
                sample.CapturingLoL = false;
                drakeCount.Value = 0;
                baronCount.Value = 0;
                AOIList.DisposeAll();
            }
            sample.StopCapture();
            if (!findLoLTimer.IsEnabled)
                findLoLTimer.Start();
        }

        private void StopCapture(object sender, EventArgs e)
        {
            StopCapture();
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

            }
        }

        private void FindValues(object sender, Bitmap bitmap)
        {
            //Hardcode upscale factor for now since this shouldn't get changed by users in UI
            //TODO Config file
            var upscalingFactor = (float)4;

            drakeCount.Value = HttpServer.dragon.TimesTakenInMatch;
            baronCount.Value = HttpServer.baron.TimesTakenInMatch;

            if (doIM && HttpServer.dragon.Cooldown != int.MaxValue)
            {
                doIM = false;
                if (!DoDragonTypeImageMatching(OCR.Utils.ApplyCrop(bitmap, AOIList.Dragon_Type.Rect), out AOIList.Dragon_Type.CurrentContent))
                    doIM = true;
            }

            //If set to low, perform OCR consecutively, otherwise use thread pooling to greatly speed up OCR

            //FindValuesThreaded(bitmap, upscalingFactor);
            FindValuesThreadPool(bitmap, upscalingFactor);
        }

        private void FindValuesThreadPool(Bitmap bitmap, float upscalingFactor) {
            var time = DateTime.Now.Ticks;

            //Crop bitmap sequentially since Graphics cannot be accessed in parallel
            //Store results in a Dictionary to do OCR afterwards
            var bmpDict = new Dictionary<AreaOfInterest, Bitmap>();
            var task = Task.Run(() => {
                AOIList.GetOCRAreaOfInterests().ForEach(aoi =>
                {
                    bmpDict.Add(aoi, OCR.Utils.ApplyUpscale(upscalingFactor, (Bitmap)bitmap.Clone(aoi.Rect, bitmap.PixelFormat)));
                });
            });
            task.Wait();

            var threadCount = bmpDict.Count;
            //Track total Threads in Pool
            var list = new List<int>(threadCount);
            for (var i = 0; i < threadCount; i++) list.Add(i);

            using (var countdownEvent = new CountdownEvent(threadCount))
            {
                for (var i = 0; i < threadCount; i++)
                {
                    var singleOCR = new SingleOCRThread(bmpDict.ElementAt(i), upscalingFactor, engines.ElementAt(i), i);

                    ThreadPool.QueueUserWorkItem(x =>
                    {
                        singleOCR.Process();
                        countdownEvent.Signal();
                    }, list[i]);

                }

                //Wait for OCR to finish
                countdownEvent.Wait();
            }

            OCRFinished.Invoke(this, new FinishOCREventArgs(bitmap, DateTime.Now.Ticks - time));
        }

        private void OCRThreadsFinished(object sender, FinishOCREventArgs e)
        {

            TimeSpan elapsedSpan = new TimeSpan(e.OCRDuration);
            //Console.WriteLine("OCR in: " + elapsedSpan.TotalMilliseconds + "ms");

            PostOCR(e.bmp, (long)elapsedSpan.TotalMilliseconds);

            if (MainWindow.saveNextFrame)
                MainWindow.saveNextFrame = false;

        }


        [ObsoleteAttribute("This method does all OCR on a single Thread. Use FindValuesThreadPool instead.")]
        private void FindValuesThreaded(Bitmap bitmap, float upscalingFactor)
        {
            var timeStart = DateTime.Now.Ticks;
            OCRThread ocrT = new OCRThread(bitmap, upscalingFactor, new OCRCallback((time) =>
            {
                TimeSpan elapsedSpan = new TimeSpan(DateTime.Now.Ticks - timeStart);
                //Console.WriteLine("OCR in: " + elapsedSpan.TotalMilliseconds + "ms");
                PostOCR(bitmap, time);
            }));
            Thread OCRThread = new Thread(new ThreadStart(ocrT.StartGoldOCR));
            OCRThread.Start();
        }

        private bool DoDragonTypeImageMatching(Bitmap bmp, out string content)
        {
            var resultList = dataManager.GetClosestDragonType(bmp).OrderBy(x => x.distance);
            var result = resultList.ElementAt(0);
            content = Enum.GetName(typeof(DragonType), result.type);

            //Try to filter out some invalid dragon possibilities
            if (!IsValidDragon(result))
            {
                return false;
            }

            Console.WriteLine("Valid Dragon Type: " + content + " (" + result.confidence.ToString("0.000") + ")");
            return true;
        }

        private void PostOCR(Bitmap bitmap, long time)
        {
            //If OCR takes longer than the timer interval, slow down OCR
            //Conversely, if OCR is far faster than the interval, speed up to 1x second

            if (time >= (long)OCRInterval)
            {
                OCRInterval += 1000 * Math.Floor((double)(time / OCRInterval));
                CustomTimer.Instance.SetInterval((int)OCRInterval);
            }
            else if (OCRInterval > 1000 && (long)OCRInterval > time * 2)
            {
                OCRInterval = Math.Max(OCRInterval / 2, 1000);
                CustomTimer.Instance.SetInterval((int)OCRInterval);
            }

            var oldDragonIsAlive = HttpServer.dragon.IsAlive;
            var oldBaronIsAlive = HttpServer.baron.IsAlive;
            HttpServer.UpdateNeutralTimers();
            if (!(HttpServer.dragon.Cooldown == 0) && oldDragonIsAlive)
            {
                HttpServer.oldTypes.Add((DragonType)Enum.Parse(typeof(DragonType), HttpServer.dragon.Type));
                HttpServer.dragon.TimesTakenInMatch++;
                GetTeamInAreaOfInterest(HttpServer.dragon, AOIList.DragonTeam, bitmap);
                if (!DoDragonTypeImageMatching(OCR.Utils.ApplyCrop(bitmap, AOIList.Dragon_Type.Rect), out AOIList.Dragon_Type.CurrentContent))
                    doIM = true;
            }
            if (!(HttpServer.baron.Cooldown == 0) && oldBaronIsAlive)
            {
                HttpServer.baron.TimesTakenInMatch++;
                GetTeamInAreaOfInterest(HttpServer.baron, AOIList.BaronTeam, bitmap);
            }
            HttpServer.UpdateTeams();
        }

        private void GetTeamInAreaOfInterest(Server.Models.Objective o, AreaOfInterest aoi, Bitmap bmp)
        {
            var outMap = OCR.Utils.ApplyCrop(bmp, aoi.Rect);
            var task = Task.Run(() => { var found = GetTeamForObjective(o, aoi, outMap, 0); if (found) Console.WriteLine("Found objective killing team"); });
        }

        private bool GetTeamForObjective(Server.Models.Objective o, AreaOfInterest aoi, Bitmap regionBitmap, int i)
        {
            if (i >= 10)
            {
                //Tried too often to determine team! Stop now and let the user manually override
                o.LastTakenBy = -1;
                o.IsAlive = false;
                return false;
            }

            var searchString = o.Type == "Baron" ? "Baron" : "Dragon";
            var engine = engines.ElementAt(engines.Count - 1);
            var team = engine.GetTeamInBitmap(regionBitmap);

            if (team.Length != 0 && team.Contains(searchString))
            {
                if (team.Contains("Bl") || team.Contains("ue") || team.Contains("Bu"))
                {
                    o.LastTakenBy = 0;
                    o.IsAlive = false;
                    return true;
                }
                else if (team.Contains("Re") || team.Contains("ed") || team.Contains("Rd"))
                {
                    o.LastTakenBy = 1;
                    o.IsAlive = false;
                    return true;
                }

                Console.WriteLine("Found Objective but not Team! Considering Objective dead but not setting killer!");
                o.LastTakenBy = -1;
                o.IsAlive = false;
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(200));
            ScheduleImmediateBitmap(new ObjectiveRequest(aoi, o), ++i);
            return false;
        }

        private void ScheduleImmediateBitmap(ObjectiveRequest objectiveRequest, int i)
        {
            Console.WriteLine("Could not determine team for objective.");
            currentObjectiveRequests.Add(objectiveRequest, i);
            sample.RequestImmediateBitmap();
        }

        private void GetTeamForObjectiveLate(object sender, Bitmap bmp)
        {
            if (currentObjectiveRequests.Count > 2)
            {
                Console.WriteLine("Something went very wrong! Trying to map more teams to objects than there are objectives!");
            }
            if (currentObjectiveRequests.Count == 0)
                return;

            List<Bitmap> bmps = new List<Bitmap>();
            for (int i = 0; i < currentObjectiveRequests.Count; i++)
            {
                bmps.Add(OCR.Utils.ApplyCrop(bmp, currentObjectiveRequests.ElementAt(i).Key.aoi.Rect));

            }

            _ = Task.Run(() =>
            {
                for (int i = 0; i < currentObjectiveRequests.Count; i++)
                {
                    var pair = currentObjectiveRequests.ElementAt(i);
                    Console.WriteLine(pair.Value + " attempts to determine this objective team");
                    var aoi = pair.Key.aoi;
                    var o = pair.Key.o;
                    currentObjectiveRequests.Remove(pair.Key);
                    var found = GetTeamForObjective(o, aoi, bmps.ElementAt(i), pair.Value);
                    if (found)
                        Console.WriteLine("Found objective killing team");
                }
            });
        }


        private bool IsValidDragon(DragonTypeResult result)
        {
            /*
            return !((result.type == DragonType.elder && HttpServer.oldTypes.Count < 4) ||
                (HttpServer.oldTypes.Count >= 3 && (result.type != HttpServer.oldTypes.ElementAt(2) || result.type != DragonType.elder)) ||
                (HttpServer.oldTypes.Count < 3 && HttpServer.oldTypes.Contains(result.type)) ||
                result.confidence < 0.75f);
            */

            //Simpler validation since confidence seems to be a rather good indicator if the result is valid
            return result.confidence > 0.75f;
        }

        //UI Elements

        private void ShowPreviewButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sample == null)
                return;
            showPreview = true;
            sample.RenderPreview = true;

            UpdateWindowSize(sample.CaptureSize);
        }

        private void ShowPreviewButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sample == null)
                return;
            showPreview = false;
            sample.RenderPreview = false;

            ReduceWindowToControls();
        }

        private void ReduceWindowToControls()
        {
            Main.MaxWidth = 216;
            Main.MinWidth = 216;

            Main.MaxHeight = 450;
            Main.MinHeight = 450;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopCapture();
        }

        private void ESportsTimerButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sample != null)
                sample.UpdateESportsTimers();
        }

        private void ESportsTimerButton_Unchecked(object sender, RoutedEventArgs e)
        {
            sample.UpdateNormalTimers();
        }

        private void DisableSkip(object sender, RoutedEventArgs e)
        {
            HttpServer.OnlyIncreaseGold = true;
            HttpServer.oldValues.ForEach((l) => l.Clear());
        }

        private void AllowSkip(object sender, RoutedEventArgs e)
        {
            HttpServer.OnlyIncreaseGold = false;
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            if (dialog.ShowDialog() == true)
            {
                var temp = dialog.FileName;
                saveName = temp.Replace(".png", "").Replace(".jpg", "");
                saveNextFrame = true;
            }
        }
        private void drakeCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            HttpServer.dragon.TimesTakenInMatch = drakeCount.Value.Value;
        }

        private void baronCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            HttpServer.baron.TimesTakenInMatch = baronCount.Value.Value;
        }

        //Timer Init
        private void StartTimers()
        {
            Debug.WriteLine("Starting Timers");
            if (findLoLTimer != null)
                return;

            findLoLTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            findLoLTimer.Tick += FindLoLProcess;
            findLoLTimer.Start();

            CustomTimer.Instance.SetInterval((int)OCRInterval);
            CustomTimer.Instance.AddDelegate(() => { sample.RequestCurrentBitmap(); });
        }

    }

    public delegate void OCRCallback(long OCRDuration);

    class OCRThread
    {
        private readonly Bitmap bitmap;
        private readonly float upscaleFactor;
        private readonly OCRCallback callback;

        public OCRThread(Bitmap bitmap, float upscaleFactor, OCRCallback callback)
        {
            this.bitmap = bitmap;
            this.upscaleFactor = upscaleFactor;
            this.callback = callback;
        }

        public void StartGoldOCR()
        {
            long startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            AOIList.GetOCRAreaOfInterests().ForEach((aoi) =>
            {
                var regionBitmap = OCR.Utils.ApplyCrop(bitmap, aoi.Rect);
                if (upscaleFactor != 1)
                {
                    regionBitmap = OCR.Utils.ApplyUpscale(upscaleFactor, regionBitmap);
                }

                //Do Color Processing to make text clearer
                switch (aoi.type)
                {
                    case Common.AOIType.BlueGold:
                        OCR.Utils.BlueTextColorPass(regionBitmap);
                        aoi.CurrentContent = DoGoldOCR(regionBitmap);
                        break;
                    case Common.AOIType.RedGold:
                        //OCR.Utils.RedTextColorPass(regionBitmap);
                        OCR.Utils.ApplyBrightnessColorMask(regionBitmap);
                        aoi.CurrentContent = DoGoldOCR(regionBitmap);
                        break;
                    case Common.AOIType.ESportsTimer:
                        OCR.Utils.ApplyBrightnessColorMask(regionBitmap);
                        aoi.CurrentContent = DoTimeOCR(regionBitmap);
                        break;
                    case Common.AOIType.NormalTimer:
                        OCR.Utils.ApplyBrightnessColorMask(regionBitmap);
                        aoi.CurrentContent = DoTimeOCR(regionBitmap);
                        break;
                }

                if (MainWindow.saveNextFrame)
                    regionBitmap.Save(MainWindow.saveName + nameof(aoi) + ".png", ImageFormat.Png);

            });
            if (MainWindow.saveNextFrame)
                MainWindow.saveNextFrame = false;

            callback?.Invoke(DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime);
        }

        private string DoGoldOCR(Bitmap bmp)
        {
            return new OCREngine().GetGoldInBitmap(bmp);
        }

        private string DoTimeOCR(Bitmap bmp)
        {
            return new OCREngine().GetTimeInBitmap(bmp);
        }
    }

    public class SingleOCRThread
    {
        private readonly float upscaleFactor;
        private KeyValuePair<AreaOfInterest, Bitmap> area;
        private OCREngine engine;
        private int threadID;

        public SingleOCRThread(KeyValuePair<AreaOfInterest, Bitmap> area, float upscaleFactor, OCREngine engine, int threadID)
        {
            this.area = area;
            this.upscaleFactor = upscaleFactor;
            this.threadID = threadID;
            this.engine = engine;
        }

        public void Process()
        {
            //this.engine = new OCREngine();
            //Do Color Processing to make text clearer
            switch (area.Key.type)
            {
                case Common.AOIType.BlueGold:
                    OCR.Utils.BlueTextColorPass(area.Value);
                    area.Key.CurrentContent = DoGoldOCR(area.Value);
                    break;
                case Common.AOIType.RedGold:
                    //OCR.Utils.RedTextColorPass(regionBmp);
                    OCR.Utils.ApplyBrightnessColorMask(area.Value);
                    area.Key.CurrentContent = DoGoldOCR(area.Value);
                    break;
                case Common.AOIType.ESportsTimer:
                    OCR.Utils.ApplyBrightnessColorMask(area.Value);
                    area.Key.CurrentContent = DoTimeOCR(area.Value);
                    break;
                case Common.AOIType.NormalTimer:
                    OCR.Utils.ApplyBrightnessColorMask(area.Value);
                    area.Key.CurrentContent = DoTimeOCR(area.Value);
                    break;
            }

            if (MainWindow.saveNextFrame)
                area.Value.Save(MainWindow.saveName + nameof(area.Key) + ".png", ImageFormat.Png);

            //OCRCallbackAggregation.FinishThread(threadID);

        }

        private string DoGoldOCR(Bitmap bmp)
        {
            return engine.GetGoldInBitmap(bmp);
        }

        private string DoTimeOCR(Bitmap bmp)
        {
            return engine.GetTimeInBitmap(bmp);
        }
    }

    class FinishOCREventArgs
    {
        public long OCRDuration;
        public Bitmap bmp;

        public FinishOCREventArgs(Bitmap bmp, long OCRDuration)
        {
            this.bmp = bmp;
            this.OCRDuration = OCRDuration;
        }
    }
}
