using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_MockServer.Models.DTOs
{
    public partial class DeviceDataStruct : ObservableObject
    {
        [ObservableProperty] private int id;
        public string BindIp { get; set; }
        public string BindPort { get; set; }
        public string DstIp { get; set; }
        public string DstPort { get; set; }
        public bool IsUdp { get; set; }
        public ObservableCollection<LogDataItem> logs { get; set; } = new ObservableCollection<LogDataItem>();
    }

    public class LogDataItem
    {
        public string TimeStamp { get; set; }
        public string Type { get; set; }
        public byte Length { get; set; }
        public ushort ControlWord { get; set; }
        public byte[] Data { get; set; }
    }
}
