using Backend.Object.GameSystems.Gameplay;
using Backend.Object.GameSystems.Llm;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Backend.Object.UI
{
    public class CardCreationPanelPresenter : UIPresenter<CardCreationPanel>
    {
        private static readonly string[] LoadingMessages =
        {
            "AI가 카드를 분석 중...",
            "마력 패턴을 계산 중...",
            "밸런스를 검토 중...",
            "고유 능력을 생성 중...",
            "카드를 완성하는 중...",
        };

        private CompositeDisposable _disposables;
        private int _loadingIndex;

        public override void OnOpen()
        {
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            if (View == null)
                return;

            if (View.GenerateButton != null)
            {
                View.GenerateButton.OnClickAsObservable()
                    .Subscribe(_ => Generate().Forget())
                    .AddTo(_disposables);
            }

            if (View.SkipButton != null)
            {
                View.SkipButton.OnClickAsObservable()
                    .Subscribe(_ => CardCreationSystem.SkipAndReturnToMap())
                    .AddTo(_disposables);
            }

            if (View.AddButton != null)
            {
                View.AddButton.OnClickAsObservable()
                    .Subscribe(_ => CardCreationSystem.ConfirmAddSelected())
                    .AddTo(_disposables);
            }

            if (View.DiscardButton != null)
            {
                View.DiscardButton.OnClickAsObservable()
                    .Subscribe(_ => CardCreationSystem.SkipAndReturnToMap())
                    .AddTo(_disposables);
            }

            if (View.CandidateAButton != null)
            {
                View.CandidateAButton.OnClickAsObservable()
                    .Subscribe(_ => CardCreationSystem.SelectCandidate(0))
                    .AddTo(_disposables);
            }

            if (View.CandidateBButton != null)
            {
                View.CandidateBButton.OnClickAsObservable()
                    .Subscribe(_ => CardCreationSystem.SelectCandidate(1))
                    .AddTo(_disposables);
            }

            CardCreationSystem.OnStateChanged.Subscribe(_ => Refresh()).AddTo(_disposables);
            Refresh();
        }

        public override void OnClose()
        {
            _disposables?.Dispose();
            _disposables = null;
        }

        private async UniTaskVoid Generate()
        {
            if (View == null || CardCreationSystem.IsGenerating || CardCreationSystem.HasGenerated)
                return;

            var concept = View.ConceptInput != null ? View.ConceptInput.text : string.Empty;
            View.SetInputInteractable(false);
            View.SetSkipVisible(false);
            View.SetLoading(true, LoadingMessages[0]);

            RunLoadingLoopAsync().Forget();
            await CardCreationSystem.GenerateAsync(concept);
            if (View != null)
                View.SetLoading(false);
            Refresh();
        }

        private async UniTaskVoid RunLoadingLoopAsync()
        {
            _loadingIndex = 0;
            while (CardCreationSystem.IsGenerating && View != null)
            {
                View.SetLoading(true, LoadingMessages[_loadingIndex % LoadingMessages.Length]);
                _loadingIndex++;
                await UniTask.Delay(700);
            }
        }

        private void Refresh()
        {
            if (View == null)
                return;

            var elite = CardCreationSystem.IsEliteDraft;
            View.SetHeader(
                elite ? "☠️ 정예 보상 — AI 카드 생성" : "🃏 AI 카드 생성",
                elite
                    ? "원하는 카드를 묘사하세요. 후보 2장 중 한 장만 가져갈 수 있습니다."
                    : "원하는 카드를 자유롭게 묘사하세요. AI가 게임 밸런스에 맞춰 생성합니다.");

            var generating = CardCreationSystem.IsGenerating;
            var generated = CardCreationSystem.HasGenerated;

            View.SetInputInteractable(!generating && !generated);
            View.SetSkipVisible(!generating && !generated);
            View.SetLoading(generating);
            View.SetResultVisible(generated);
            View.SetDraftVisible(generated && elite && CardCreationSystem.Candidates.Count >= 2);

            if (!generated)
                return;

            var selected = CardCreationSystem.SelectedCard;
            if (selected == null)
                return;

            CardCreationSystem.GetThreatPreview(out var curThreat, out var nextThreat, out var curTier, out var nextTier);
            var cps = selected.power_score;
            var threatLine = cps <= 0.01f
                ? "☠ 위협도 변화 없음"
                : $"☠ 위협도  {curThreat:0.#} → {nextThreat:0.#}   (Tier {curTier} → {nextTier})";

            var description = CardDescriptionFormatter.EnsureDamageFormKeywords(
                selected.description,
                selected.effects);
            View.SetResult(
                selected.name,
                CardDescriptionFormatter.ColorizeDamageFormKeywords(description),
                $"마나 {selected.mana_cost}  ·  {selected.card_type}  ·  CPS {cps:0.#}",
                threatLine);

            if (elite && CardCreationSystem.Candidates.Count >= 2)
            {
                var a = CardCreationSystem.Candidates[0];
                var b = CardCreationSystem.Candidates[1];
                var sel = CardCreationSystem.SelectedIndex;
                View.SetCandidateLabels(
                    $"{(sel == 0 ? "▶ " : "")}{a.name} (CPS {a.power_score:0.#})",
                    $"{(sel == 1 ? "▶ " : "")}{b.name} (CPS {b.power_score:0.#})");
            }
        }
    }
}
