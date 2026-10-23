using System;
using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WPF_MockServer.Models;

namespace WPF_MockServer.Core
{
    public class SmartResponseEngine
    {
        public ObservableCollection<SmartRuleModel> Rules { get; } = new();
        private readonly MockDevice _parentDevice;

        // JSON 포맷팅 옵션 지정[cite: 3]
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public SmartResponseEngine(MockDevice parentDevice)
        {
            _parentDevice = parentDevice;
        }

        // .json 파일 로드 로직
        public async Task LoadRulesFromJsonAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;

                string jsonString = await File.ReadAllTextAsync(filePath);
                var loadedRules = JsonSerializer.Deserialize<SmartRuleModel[]>(jsonString, _jsonOptions);

                if (loadedRules != null)
                {
                    string fileName = Path.GetFileName(filePath);
                    foreach (var rule in loadedRules)
                    {
                        rule.SourceFileName = fileName;
                        Rules.Add(rule);
                    }
                }
            }
            catch (Exception ex)
            {
                _parentDevice.Log($"JSON 로드 실패: {ex.Message}");
            }
        }

        // .json 파일 저장 로직
        public async Task SaveRulesToJsonAsync(string filePath)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(Rules.ToArray(), _jsonOptions);
                await File.WriteAllTextAsync(filePath, jsonString);
                _parentDevice.Log($"규칙이 {Path.GetFileName(filePath)}에 저장되었습니다.");
            }
            catch (Exception ex)
            {
                _parentDevice.Log($"JSON 저장 실패: {ex.Message}");
            }
        }

        // 패킷 평가 및 지연 송신 (Task 분리)
        public void EvaluateAndRespond(ushort receivedControlWord, CancellationToken token)
        {
            // 스레드 안전하게 규칙 스냅샷 복사
            SmartRuleModel[] matchingRules;
            lock (Rules)
            {
                matchingRules = Rules.Where(r => r.MatchControlWord == receivedControlWord && r.IsEnabled).ToArray();
            }

            foreach (var rule in matchingRules)
            {
                // 백그라운드 응답 실행
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (rule.ResendDelay > 0)
                        {
                            await Task.Delay(rule.ResendDelay, token);
                        }
                        token.ThrowIfCancellationRequested();

                        byte[] packetToSend = BuildPacket(rule.ResponseControlWord, rule.ResponseDataHex);
                        await _parentDevice.SendAsync(packetToSend);
                        _parentDevice.Log($"Smart 응답 송신: CW={rule.ResponseControlWord:X4}");
                    }
                    // TaskCanceledException 대신 상위 클래스인 OperationCanceledException을 catch
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _parentDevice.Log($"Smart 응답 에러: {ex.Message}");
                    }
                }, token);
            }
        }

        private byte[] BuildPacket(ushort controlWord, string hexData)
        {
            // CW(2바이트) + 페이로드(1024바이트) = 총 1026바이트
            byte[] packet = new byte[1026];

            // 1. ControlWord를 빅 엔디안으로 안전하게 기록
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(0, 2), controlWord);

            // 2. Hex 문자열이 있는 경우 Span을 사용해 무할당 변환
            if (!string.IsNullOrWhiteSpace(hexData))
            {
                // Convert.FromHexString(string) 사용 (C# 8.0+/net6+)
                byte[] data = Convert.FromHexString(hexData);
                int copyLen = Math.Min(data.Length, 1024);
                Array.Copy(data, 0, packet, 2, copyLen);
            }

            return packet;
        }
    }
}