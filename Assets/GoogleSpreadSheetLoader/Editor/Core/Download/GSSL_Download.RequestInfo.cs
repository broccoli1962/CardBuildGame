using System.Collections.Generic;
using System.Linq;
using GoogleSpreadSheetLoader.Setting;
using UnityEngine.Networking;
using UnityEngine; // Added for Debug.LogWarning

namespace GoogleSpreadSheetLoader.Download
{
    internal static partial class GSSL_Download
    {
        private static List<RequestInfo> GetRequestInfoList(Dictionary<string, List<string>> dicSpreadSheet)
        {
            var listDownloadInfo = new List<RequestInfo>();

            foreach (var (spreadSheetId, listTitle) in dicSpreadSheet)
            {
                listDownloadInfo.AddRange(listTitle.Select(title => new RequestInfo(spreadSheetId, title)));
            }

            return listDownloadInfo;
        }
    }
    
    public class RequestInfo
    {
        private const int MaxRetryCount = 3; // 최대 재시도 횟수
        private const int RetryDelayMs = 1000; // 재시도 간격 (밀리초)
        
        public RequestInfo(string spreadSheetId, string sheetName)
        {
            _spreadSheetId = spreadSheetId;
            _sheetName = sheetName;
            _retryCount = 0;
            _isDisposed = false;

            CreateWebRequest();
        }

        private void CreateWebRequest()
        {
            _url = string.Format(GSSL_URL.DownloadSheetUrl, _spreadSheetId, _sheetName,
                GSSL_Setting.SettingData.apiKey);
            _webRequest = UnityWebRequest.Get(_url);
        }

        public string SpreadSheetId => _spreadSheetId;
        private readonly string _spreadSheetId;
        public string SheetName => _sheetName;
        private readonly string _sheetName;
        public string URL => _url;
        private string _url;

        public bool IsDone => _webRequest?.isDone ?? false;
        public string DownloadText => _webRequest?.downloadHandler?.text ?? "";
        
        // 에러 체크 기능 추가 - 요청이 완료된 후에만 에러로 판단
        public bool HasError => _webRequest != null && _webRequest.isDone && _webRequest.result != UnityWebRequest.Result.Success;
        public string ErrorMessage => _webRequest?.error ?? "";
        public UnityWebRequest.Result Result => _webRequest?.result ?? UnityWebRequest.Result.DataProcessingError;
        
        // 재시도 관련 속성
        public int RetryCount => _retryCount;
        public bool CanRetry => _retryCount < MaxRetryCount && !_isDisposed;
        public bool IsDisposed => _isDisposed;
        
        private UnityWebRequest _webRequest;
        private int _retryCount;
        private bool _isDisposed;
        
        public void SendAndGetAsyncOperation()
        {
            if (_isDisposed) return;
            _webRequest.SendWebRequest();
        }
        
        public async System.Threading.Tasks.Task<bool> RetryAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            if (!CanRetry) return false;
            
            _retryCount++;
            Debug.LogWarning($"시트 '{_sheetName}' 재시도 중... ({_retryCount}/{MaxRetryCount})");
            
            // 이전 요청 정리
            _webRequest?.Dispose();
            
            // 재시도 간격 대기
            await System.Threading.Tasks.Task.Delay(RetryDelayMs, cancellationToken);
            
            // 새로운 요청 생성
            CreateWebRequest();
            SendAndGetAsyncOperation();
            
            return true;
        }
        
        public void Cancel()
        {
            if (_webRequest != null && !_webRequest.isDone)
            {
                _webRequest.Abort();
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            _isDisposed = true;
            Cancel(); // 진행중인 요청 취소
            _webRequest?.Dispose();
        }
    }
}