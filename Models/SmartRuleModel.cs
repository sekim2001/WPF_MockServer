using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace WPF_MockServer.Models
{
    public partial class SmartRuleModel : ObservableObject
    {
        // UI의 토글 버튼(활성화/비활성화)과 양방향 바인딩
        [ObservableProperty]
        private bool isEnabled = true;

        // 수신 조건
        public ushort MatchControlWord { get; set; }

        // 응답 데이터
        public ushort ResponseControlWord { get; set; }

        // JSON 가독성을 위해 Hex 문자열로 저장 ("00FF..." 형태)
        public string ResponseDataHex { get; set; } = string.Empty;

        // 응답 지연 시간 (ms)
        public int ResendDelay { get; set; }

        // UI 전시용 (JSON 직렬화 대상에서는 제외)
        [JsonIgnore]
        public string SourceFileName { get; set; } = string.Empty;
    }
}