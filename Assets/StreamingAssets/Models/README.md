# Local LLM Models

Place GGUF weights here for in-process LlamaSharp inference.

## Default model

`Qwen2.5-1.5B-Instruct-Q4_K_M.gguf`

Used by `LocalLlmManager` and `Tools/CardBuildGame/LLM/Run Smoke Test`.

Download (Hugging Face):

```text
https://huggingface.co/Qwen/Qwen2.5-1.5B-Instruct-GGUF
```

`.gguf` files should stay out of git (large binary). Without a model, card generation uses the rule-based fallback and gameplay continues.
