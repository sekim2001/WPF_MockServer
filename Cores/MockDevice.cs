using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using WPF_MockServer.Services;

namespace WPF_MockServer.Core
{
    public class MockDevice
    {
        public int DeviceId { get; }
        public string BindIp { get; }
        public int BindPort { get; }
        public string DestIp { get; }
        public int DestPort { get; }

        private readonly INetworkService _networkService;
        public SmartResponseEngine SmartEngine { get; }

        private CancellationTokenSource? _deviceCts;
        private bool _disposed;

        // UI에 로그를 전달하기 위한 이벤트
        public event Action<string>? OnLogMessage;

        public MockDevice(int id, string bindIp, int bindPort, string destIp, int destPort, bool isUdp = true)
        {
            DeviceId = id;
            BindIp = bindIp;
            BindPort = bindPort;
            DestIp = destIp;
            DestPort = destPort;

            _networkService = isUdp ? new UdpService() : new TcpService(); // TCP는 나중에 구현 가능
            SmartEngine = new SmartResponseEngine(this);

            _networkService.OnPacketReceived += HandlePacketReceived;
            _networkService.OnError += (err) => Log($"통신 에러: {err}");
        }

        public void Start()
        {
            _deviceCts?.Dispose();
            _deviceCts = new CancellationTokenSource();
            _networkService.Start(BindIp, BindPort);
            Log($"장치 시작됨 (Bind: {BindIp}:{BindPort})");
        }

        public void Stop()
        {
            _deviceCts?.Cancel();
            _networkService.Stop();
            Log("장치 중지됨");
        }

        public async Task SendAsync(byte[] data)
        {
            await _networkService.SendAsync(data, DestIp, DestPort);
        }

        private void HandlePacketReceived(byte[] rawData)
        {
            Log($"패킷 수신됨 ({rawData.Length} bytes)");

            if (rawData.Length < 2) return;

            // ControlWord 파싱 (빅 엔디안 가정)
            ushort cw = BinaryPrimitives.ReadUInt16BigEndian(rawData.AsSpan().Slice(0, 2));

            // 스마트 엔진에 응답 평가 위임
            if (_deviceCts != null)
            {
                SmartEngine.EvaluateAndRespond(cw, _deviceCts.Token);
            }
        }

        internal void Log(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            OnLogMessage?.Invoke($"[{time}] [Device {DeviceId}] {message}");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Stop();
            _deviceCts?.Dispose();

            // 이벤트 핸들러 해제로 메모리 누수 방지
            _networkService.OnPacketReceived -= HandlePacketReceived;
        }
    }
}