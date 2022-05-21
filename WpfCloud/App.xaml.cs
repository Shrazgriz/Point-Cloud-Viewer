using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WpfCloud
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            string email = "nichongben@sjtu.edu.cn";
            string uuid = "1210000042500615X0-20200511";
            string sn = "c983746addf9873dcf21f97b948d1022";
            AnyCAD.Platform.GlobalInstance.Application.SetLogFileName(new AnyCAD.Platform.Path("anycad.net.sdk.log"));
            AnyCAD.Platform.GlobalInstance.RegisterSDK(email, uuid, sn);
        }
    }
}
