using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace WPF_MockServer.Services
{
    public class TcpService : INetworkService
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;

        public event Action<byte[]>? OnPacketReceived;
        public event Action<string>? OnError;

        public void Start(string bindIp, int bindPort)
        {
            _cts = new CancellationTokenSource();
            try
            {
                var endPoint = new IPEndPoint(IPAddress.Parse(bindIp), bindPort);
                _listener = new TcpListener(endPoint);
                _listener.Start();

                // 연결 수신 루프 백그라운드 실행
                Task.Run(() => AcceptLoopAsync(_cts.Token));
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"[TCP Bind Error] {ex.Message}");
            }
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && _listener != null)
                {
                    // 클라이언트 접속 대기
                    TcpClient client = await _listener.AcceptTcpClientAsync(token);

                    // 접속한 클라이언트마다 독립적인 수신 Task 할당
                    _ = Task.Run(() => HandleClientAsync(client, token), token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                OnError?.Invoke($"[TCP Accept Error] {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                byte[] buffer = new byte[2048];
                try
                {
                    while (!token.IsCancellationRequested && client.Connected)
                    {
                        // 데이터 읽기 대기
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);

                        // 연결이 종료되었을 경우
                        if (bytesRead == 0) break;

                        byte[] receivedData = new byte[bytesRead];
                        Array.Copy(buffer, receivedData, bytesRead);
                        OnPacketReceived?.Invoke(receivedData);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    OnError?.Invoke($"[TCP Receive Error] {ex.Message}");
                }
            }
        }

        public async Task SendAsync(byte[] data, string destIp, int destPort)
        {
            try
            {
                // TCP의 경우 송신 시 일회성 연결을 맺고 끊도록 간단하게 구성했습니다.
                // Packet Sender의 일반적인 단방향 전송 모의와 일치합니다.
                using TcpClient client = new TcpClient();
                await client.ConnectAsync(destIp, destPort);
                using NetworkStream stream = client.GetStream();
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"[TCP Send Error] {ex.Message}");
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener = null;
        }
    }
}