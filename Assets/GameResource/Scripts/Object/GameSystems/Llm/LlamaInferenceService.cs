using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using LLama;
using LLama.Common;
using LLama.Sampling;
using UnityEngine;

namespace Backend.Object.GameSystems.Llm
{
    /// <summary>
    /// LlamaSharp(llama.cpp) 기반 로컬 LLM 추론 서비스.
    /// StreamingAssets/Models 하위 GGUF 모델을 로드하여 텍스트를 생성한다.
    /// </summary>
    public sealed class LlamaInferenceService : IDisposable
    {
        private static readonly string ImEnd = "<|" + "im_end" + "|>";

        private LLamaWeights _weights;
        private ModelParams _modelParams;
        private bool _isLoaded;

        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// GGUF 모델을 비동기로 로드한다. GpuLayerCount=0 으로 CPU 전용 추론을 사용한다.
        /// </summary>
        public async UniTask LoadAsync(string modelPath, uint contextSize = 2048, CancellationToken ct = default)
        {
            if (_isLoaded)
                return;

            _modelParams = new ModelParams(modelPath)
            {
                ContextSize = contextSize,
                GpuLayerCount = 0,
            };

            _weights = await LLamaWeights.LoadFromFileAsync(_modelParams, ct);
            _isLoaded = true;
            Debug.Log($"[LlamaInferenceService] Model loaded. parameters={_weights.ParameterCount}, contextSize={contextSize}");
        }

        /// <summary>
        /// 프롬프트로부터 텍스트를 생성한다. Qwen Instruct용 ChatML 로 감싼다.
        /// </summary>
        public async UniTask<string> GenerateAsync(
            string prompt,
            int maxTokens = 256,
            float temperature = 0.7f,
            Action<string> onToken = null,
            Grammar grammar = null,
            CancellationToken ct = default)
        {
            if (!_isLoaded)
                throw new InvalidOperationException("[LlamaInferenceService] Model is not loaded. Call LoadAsync first.");

            var executor = new StatelessExecutor(_weights, _modelParams);

            var samplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = temperature,
                Grammar = grammar
            };

            var inferenceParams = new InferenceParams
            {
                MaxTokens = maxTokens,
                AntiPrompts = new[] { ImEnd, "<|endoftext|>" },
                SamplingPipeline = samplingPipeline,
            };

            var chatPrompt = WrapChatMl(prompt);
            var builder = new StringBuilder();
            await foreach (var token in executor.InferAsync(chatPrompt, inferenceParams, ct))
            {
                builder.Append(token);
                onToken?.Invoke(token);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Qwen2.5-Instruct ChatML 템플릿. 미적용 시 모델이 JSON을 반복 생성하기 쉽다.
        /// </summary>
        private static string WrapChatMl(string userContent)
        {
            return
                "<|im_start|>system\n" +
                "Follow the user instructions exactly. When asked for JSON, reply with exactly one JSON object and nothing else.\n" +
                ImEnd + "\n" +
                "<|im_start|>user\n" +
                userContent + "\n" +
                ImEnd + "\n" +
                "<|im_start|>assistant\n";
        }

        public void Dispose()
        {
            _weights?.Dispose();
            _weights = null;
            _isLoaded = false;
        }
    }
}
