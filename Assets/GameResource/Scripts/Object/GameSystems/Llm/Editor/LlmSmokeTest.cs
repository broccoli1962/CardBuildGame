using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Backend.Object.GameSystems.Llm;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Backend.Object.GameSystems.Llm.EditorTools
{
    /// <summary>
    /// LlamaSharp 로컬 추론 스모크 테스트.
    /// </summary>
    public static class LlmSmokeTest
    {
        private const string ModelFileName = "Qwen2.5-1.5B-Instruct-Q4_K_M.gguf";

        [MenuItem("Tools/CardBuildGame/LLM/Run Smoke Test")]
        public static void RunSmokeTest()
        {
            var modelPath = Path.Combine(Application.streamingAssetsPath, "Models", ModelFileName);
            if (!File.Exists(modelPath))
            {
                Debug.LogError($"[LlmSmokeTest] Model not found: {modelPath}");
                return;
            }

            Debug.Log("[LlmSmokeTest] Started. Running inference on a background thread...");
            _ = Task.Run(async () => await RunAsync(modelPath));
        }

        private static async Task RunAsync(string modelPath)
        {
            try
            {
                using var service = new LlamaInferenceService();

                var sw = Stopwatch.StartNew();
                await service.LoadAsync(modelPath);
                var loadMs = sw.ElapsedMilliseconds;

                const string prompt =
                    "You are a card game designer. Return ONLY JSON: {\"name\":\"test\",\"description\":\"d\",\"card_type\":\"attack\",\"mana_cost\":2,\"effects\":[{\"type\":\"DEAL_DAMAGE\",\"value\":5}]}\n" +
                    "Player request: simple attack card\n";

                sw.Restart();
                var result = await service.GenerateAsync(prompt, maxTokens: 128, temperature: 0.7f);
                var genMs = sw.ElapsedMilliseconds;

                Debug.Log(
                    $"[LlmSmokeTest] SUCCESS. Load={loadMs}ms, Generate={genMs}ms\n" +
                    $"--- OUTPUT ---\n{result.Trim()}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LlmSmokeTest] FAILED: {e.GetType().Name}: {e.Message}\n{e}");
            }
        }
    }
}
