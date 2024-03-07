using Ruination_v2.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace Ruination_v2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetDllDirectory(string lpPathName);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Directory.CreateDirectory(Utils.Utils.AppDataFolder);
            Utils.Logger.Init();
            SetDllDirectory(Directory.GetCurrentDirectory());

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Logger.LogError(e.ExceptionObject.ToString());
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            try
            {
                if(Config.GetConfig() != new ConfigObject())
                    Config.Save();
            } catch(Exception ex) {}
        }
    }
}
