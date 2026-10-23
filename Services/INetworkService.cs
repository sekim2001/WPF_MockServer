using System;
using System.Threading;
using System.Threading.Tasks;

namespace WPF_MockServer.Services
{
    public interface INetworkService
    {
        // 패킷 수신 이벤트 (byte[] 원시 데이터 반환)
        event Action<byte[]> OnPacketReceived;
        // 통신 에러 로그용 이벤트
        event Action<string> OnError;

        void Start(string bindIp, int bindPort);
        void Stop();
        Task SendAsync(byte[] data, string destIp, int destPort);
    }
}