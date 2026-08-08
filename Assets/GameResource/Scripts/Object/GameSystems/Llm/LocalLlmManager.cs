using System;
using System.IO;
using System.Threading;
using Backend.Object.Management;
using Backend.Util;
using Backend.Util.Management;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Backend.Object.GameSystems.Llm
{
    /// <summary>
    /// LlamaSharp 로컬 LLM 추론을 관리한다. GGUF 모델을 StreamingAssets 에서 로드한다.
    /// </summary>
    public sealed class LocalLlmManager : SingletonGameObject<LocalLlmManager>
    {
        private const string ModelFileName = "Qwen2.5-1.5B-Instruct-Q4_K_M.gguf";
        private const int DefaultMaxTokens = 192;

        private readonly SemaphoreSlim _inferenceLock = new(1, 1);
        private LlamaInferenceService _service;
        private bool _isModelReady;
        private bool _isModelLoading;

        public static bool IsModelReady => !GameStateUtil.IsQuitting && Instance._isModelReady;

        protected override void OnAwake()
        {
            base.OnAwake();
            LoadModelAsync().Forget();
        }

        protected override void OnApplicationQuit()
        {
            _service?.Dispose();
            _service = null;
            base.OnApplicationQuit();
        }

        /// <summary>
        /// 싱글톤을 미리 생성해 모델 로드를 시작한다.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (GameStateUtil.IsQuitting)
                return;

            _ = Instance;
        }

        /// <summary>
        /// 로컬 LLM 으로 텍스트를 생성한다. 모델 미준비 시 null 을 반환한다.
        /// </summary>
        public static async UniTask<string> GenerateAsync(
            string prompt,
            float? temperatureOverride = null,
            CancellationToken cancellationToken = default)
        {
            if (GameStateUtil.IsQuitting || string.IsNullOrWhiteSpace(prompt))
                return null;

            return await Instance.GenerateInternalAsync(prompt, temperatureOverride, cancellationToken);
        }

        private async UniTask<string> GenerateInternalAsync(
            string prompt,
            float? temperatureOverride,
            CancellationToken cancellationToken)
        {
            var timeoutSeconds = TableManager.GetInt(TableManager.BalanceKey.LlmTimeout, 60);
            var temperature = temperatureOverride
                ?? TableManager.GetFloat(TableManager.BalanceKey.LlmTemperature, 0.7f);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            if (!_isModelReady || _service == null)
            {
                if (_isModelLoading)
                {
                    try
                    {
                        await UniTask.WaitUntil(
                            () => _isModelReady || !_isModelLoading,
                            cancellationToken: timeoutCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.LogWarning("[LocalLlmManager] Timed out waiting for model load.");
                        return null;
                    }
                }

                if (!_isModelReady || _service == null)
                {
                    Debug.LogWarning("[LocalLlmManager] Model is not ready.");
                    return null;
                }
            }

            await _inferenceLock.WaitAsync(timeoutCts.Token);
            try
            {
                return await UniTask.RunOnThreadPool(async () =>
                    await _service.GenerateAsync(
                        prompt,
                        DefaultMaxTokens,
                        temperature,
                        ct: timeoutCts.Token));
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[LocalLlmManager] Inference cancelled or timed out.");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LocalLlmManager] Inference failed: {e.Message}");
                return null;
            }
            finally
            {
                _inferenceLock.Release();
            }
        }

        private async UniTaskVoid LoadModelAsync()
        {
            if (_isModelLoading || _isModelReady)
                return;

            _isModelLoading = true;
            var modelPath = Path.Combine(Application.streamingAssetsPath, "Models", ModelFileName);

            if (!File.Exists(modelPath))
            {
                Debug.LogWarning($"[LocalLlmManager] Model not found: {modelPath}. LLM disabled; fallback will be used.");
                _isModelLoading = false;
                return;
            }

            try
            {
                await UniTask.RunOnThreadPool(async () =>
                {
                    _service = new LlamaInferenceService();
                    await _service.LoadAsync(modelPath);
                });

                _isModelReady = true;
                Debug.Log("[LocalLlmManager] Model ready.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalLlmManager] Model load failed: {e.Message}");
                _service?.Dispose();
                _service = null;
            }
            finally
            {
                _isModelLoading = false;
            }
        }
    }
}
