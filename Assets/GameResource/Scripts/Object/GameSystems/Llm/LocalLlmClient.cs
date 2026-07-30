using System.Threading;
using Cysharp.Threading.Tasks;

namespace Backend.Object.GameSystems.Llm
{
    /// <summary>
    /// 로컬 LLM 텍스트 생성 진입점. 내부적으로 LlamaSharp(LocalLlmManager)를 사용한다.
    /// </summary>
    public static class LocalLlmClient
    {
        /// <summary>
        /// 프롬프트로부터 LLM 응답 텍스트를 생성한다. 실패 시 null.
        /// </summary>
        public static UniTask<string> GenerateAsync(
            string prompt,
            float? temperatureOverride = null,
            CancellationToken cancellationToken = default)
        {
            LocalLlmManager.EnsureInitialized();
            return LocalLlmManager.GenerateAsync(prompt, temperatureOverride, cancellationToken);
        }
    }
}
