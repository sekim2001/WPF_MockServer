using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using WPF_MockServer.Models.DTOs;

namespace WPF_MockServer.ViewModels
{
    public partial class MainWinodwViewModel : ObservableValidator
    {
        // 바인딩 가능한 IP 목록
        [ObservableProperty]
        private ObservableCollection<string> availableIps = new();

        // 장비 목록
        [ObservableProperty]
        private ObservableCollection<DeviceDataStruct> devices = new();

        // comnobox에서 선택한 IP 정보
        [ObservableProperty]
        private string? selectedBindIp;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [CustomValidation(typeof(MainWinodwViewModel), nameof(ValidatePort))]
        private string bindPort;

        [ObservableProperty]
        private string dstIpString;

        [ObservableProperty]
        private string dstPortString;

        [ObservableProperty]
        private bool isSelectedUdp = true;

        [ObservableProperty]
        private DeviceDataStruct selectedDevice;

        public MainWinodwViewModel()
        {
            LoadIpAddresses();
            BindPort = GetRandomAvailablePort().ToString();
        }

        [RelayCommand]
        public void AddDevice()
        {
            int deviceId = devices.Count + 1;

            DeviceDataStruct device = new DeviceDataStruct()
            {
                Id = deviceId,
                BindIp = SelectedBindIp,
                BindPort = BindPort,
                DstIp = dstIpString,
                DstPort = dstPortString,
                IsUdp = isSelectedUdp
            };

            devices.Add(device);

            SelectedDevice = devices.FirstOrDefault();

            BindPort = GetRandomAvailablePort().ToString();
        }

        [RelayCommand]
        public void DeleteDevice(int deviceId)
        {
            devices.RemoveAt(deviceId - 1);

            for (int i = 0; i < devices.Count; i++)
            {
                devices[i].Id = i+1;
            }

            SelectedDevice = devices.FirstOrDefault();
        }

        /// <summary>
        /// OS에 요청하여 현재 사용 가능한 임의의 빈 포트를 가져옵니다.
        /// </summary>
        private int GetRandomAvailablePort()
        {
            // 포트를 0으로 설정하여 리스너를 시작하면 OS가 비어있는 포트를 자동 할당합니다.
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int availablePort = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop(); // 포트 번호만 얻고 즉시 닫습니다.

            return availablePort;
        }

        public static ValidationResult ValidatePort(string portStr, ValidationContext context)
        {
            if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
            {
                return new ValidationResult("1에서 65535 사이의 유효한 포트 번호를 입력하세요.");
            }

            if (!IsPortAvailable(port))
            {
                return new ValidationResult($"포트 {port}는 이미 다른 프로그램에서 사용 중입니다.");
            }

            return ValidationResult.Success;
        }

        /// <summary>
        /// 활성화된 포트 정보를 조희하여 입력한 포트가 사용가능한지 확인
        /// </summary>
        private static bool IsPortAvailable(int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            // 현재 사용 중인 TCP 리스너 및 연결, UDP 리스너 조회
            var tcpListeners = ipGlobalProperties.GetActiveTcpListeners();
            var udpListeners = ipGlobalProperties.GetActiveUdpListeners();
            var tcpConnections = ipGlobalProperties.GetActiveTcpConnections();

            if (tcpListeners.Any(endpoint => endpoint.Port == port)) return false;
            if (udpListeners.Any(endpoint => endpoint.Port == port)) return false;
            if (tcpConnections.Any(conn => conn.LocalEndPoint.Port == port)) return false;

            return true;
        }

        /// <summary>
        /// 사용 가능한 IP 목록을 조회하여 AvailableIps 배열에 저장하고 combobox에 전시
        /// </summary>
        private void LoadIpAddresses()
        {
            AvailableIps.Clear();

            // 기본 바인딩 주소 추가 (모든 IP, 로컬호스트)
            AvailableIps.Add("0.0.0.0");
            AvailableIps.Add("127.0.0.1");

            // 활성화된 네트워크 어댑터의 IPv4 주소 검색
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface netInterface in interfaces)
            {
                if (netInterface.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                    foreach (UnicastIPAddressInformation ip in ipProps.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string ipString = ip.Address.ToString();

                            if (!AvailableIps.Contains(ipString))
                            {
                                AvailableIps.Add(ipString);
                            }
                        }
                    }
                }
            }

            if (AvailableIps.Count > 0)
            {
                SelectedBindIp = AvailableIps[0];
            }
        }
    }
}
