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

using Common;
using Composition.WindowsRuntimeHelpers;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.Windows;
using Windows.System;

namespace LoLOCRHub
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            _controller = CoreMessagingHelper.CreateDispatcherQueueControllerForCurrentThread();

            //Create Logger
            new Logging(GetLogLevel());
            int currentBuild = GetWinBuild();
            if (currentBuild < 19041)
            {
                Logging.Warn($"Windows {currentBuild} installed. Stopping now to prevent confusion");
                string msg = $"Please update Windows. LeagueOCR requires Windows Build 19041 (20H1) or newer to function. Currently running Build {currentBuild}!";
                MessageBoxResult result =
                  MessageBox.Show(
                    msg,
                    "LeagueOCR",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                if(result == MessageBoxResult.OK)
                {
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }

        private DispatcherQueueController _controller;

        private Logging.LogLevel GetLogLevel()
        {
            return (Logging.LogLevel)Enum.Parse(typeof(Logging.LogLevel), ConfigurationManager.AppSettings["LoggingMode"].ToString(), true);
        }

        private int GetWinBuild()
        {
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

            var buildNumber = registryKey.GetValue("CurrentBuildNUmber").ToString();
            Logging.Info($"Running Windows Build: {buildNumber}");
            return Int32.Parse(buildNumber);
        }

        public static void AddOrUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Logging.Warn("Error updating app settings");
            }
        }
    }
}
