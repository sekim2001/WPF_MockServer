using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace WPF_MockServer.Services
{
    public class UdpService : INetworkService
    {
        private Socket? _socket;
        private CancellationTokenSource? _cts;

        public event Action<byte[]>? OnPacketReceived;
        public event Action<string>? OnError;

        public void Start(string bindIp, int bindPort)
        {
            _cts = new CancellationTokenSource();
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // ConnectionReset 예외 무시 설정
                const int SIO_UDP_CONNRESET = -1744830452;
                _socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);

                var endPoint = new IPEndPoint(IPAddress.Parse(bindIp), bindPort);
                _socket.Bind(endPoint);

                Task.Run(() => ReceiveLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"[UDP Bind Error] {ex.Message}");
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = new byte[2048];
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (!token.IsCancellationRequested && _socket != null)
                {
                    var result = await _socket.ReceiveFromAsync(new Memory<byte>(buffer), SocketFlags.None, sender, token);
                    if (result.ReceivedBytes > 0)
                    {
                        // 수신된 크기만큼 잘라서 이벤트 발생
                        byte[] receivedData = new byte[result.ReceivedBytes];
                        Array.Copy(buffer, receivedData, result.ReceivedBytes);
                        OnPacketReceived?.Invoke(receivedData);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                OnError?.Invoke($"[UDP Receive Error] {ex.Message}");
            }
        }

        public async Task SendAsync(byte[] data, string destIp, int destPort)
        {
            if (_socket == null) return;
            try
            {
                var targetEndpoint = new IPEndPoint(IPAddress.Parse(destIp), destPort);
                await _socket.SendToAsync(data, SocketFlags.None, targetEndpoint);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"[UDP Send Error] {ex.Message}");
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _socket?.Close();
            _socket = null;
        }
    }
}