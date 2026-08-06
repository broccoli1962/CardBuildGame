using System.Collections.Generic;
using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using R3;

namespace Backend.Object.UI
{
    public class DeckInspectPopupPresenter : UIPresenter<DeckInspectPopup>
    {
        private CompositeDisposable _disposables;
        private DeckInspectTab _currentTab;

        public override void OnOpen()
        {
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            if (View == null)
                return;

            _currentTab = DeckInspectPopup.PendingTab;

            if (View.CloseButton != null)
            {
                View.CloseButton.OnClickAsObservable()
                    .Subscribe(_ => UIManager.Close(View))
                    .AddTo(_disposables);
            }

            if (View.DrawTabButton != null)
            {
                View.DrawTabButton.OnClickAsObservable()
                    .Subscribe(_ => ShowTab(DeckInspectTab.DrawPile))
                    .AddTo(_disposables);
            }

            if (View.DiscardTabButton != null)
            {
                View.DiscardTabButton.OnClickAsObservable()
                    .Subscribe(_ => ShowTab(DeckInspectTab.DiscardPile))
                    .AddTo(_disposables);
            }

            DeckSystem.OnHandChanged
                .Subscribe(_ => Refresh())
                .AddTo(_disposables);

            ShowTab(_currentTab);
        }

        public override void OnClose()
        {
            View?.ClearRows();
            _disposables?.Dispose();
            _disposables = null;
        }

        private void ShowTab(DeckInspectTab tab)
        {
            _currentTab = tab;
            DeckInspectPopup.PendingTab = tab;
            Refresh();
        }

        private void Refresh()
        {
            if (View == null)
                return;

            View.SetTabVisual(_currentTab);

            if (_currentTab == DeckInspectTab.DrawPile)
            {
                View.SetHeader("남은 덱");
                View.SetCount($"{DeckSystem.DrawPileCount}장");
                View.ShowCards(CopyDrawPileTopFirst());
            }
            else
            {
                View.SetHeader("무덤");
                View.SetCount($"{DeckSystem.DiscardPileCount}장");
                View.ShowCards(CopyDiscardNewestFirst());
            }
        }

        /// <summary>
        /// 다음에 뽑힐 카드가 목록 상단에 오도록 복사합니다.
        /// </summary>
        private static List<RuntimeCard> CopyDrawPileTopFirst()
        {
            var source = DeckSystem.DrawPile;
            var list = new List<RuntimeCard>(source.Count);
            for (var i = source.Count - 1; i >= 0; i--)
                list.Add(source[i]);
            return list;
        }

        /// <summary>
        /// 최근에 버린 카드가 목록 상단에 오도록 복사합니다.
        /// </summary>
        private static List<RuntimeCard> CopyDiscardNewestFirst()
        {
            var source = DeckSystem.DiscardPile;
            var list = new List<RuntimeCard>(source.Count);
            for (var i = source.Count - 1; i >= 0; i--)
                list.Add(source[i]);
            return list;
        }
    }
}
