using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using Cysharp.Threading.Tasks;
using R3;

namespace Backend.Object.UI
{
    public class GamePanelPresenter : UIPresenter<GamePanel>
    {
        private CompositeDisposable _disposables;

        public override void OnOpen()
        {
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            if (View == null)
                return;

            Observable.CombineLatest(PlayerStateSystem.Hp, PlayerStateSystem.MaxHp, (hp, maxHp) => (hp, maxHp))
                .Subscribe(tuple => View.SetHealth(tuple.hp, tuple.maxHp))
                .AddTo(_disposables);

            Observable.CombineLatest(PlayerStateSystem.Mana, PlayerStateSystem.MaxMana, (mana, maxMana) => (mana, maxMana))
                .Subscribe(tuple => View.SetMana(tuple.mana, tuple.maxMana))
                .AddTo(_disposables);

            if (View.EndPlayerTurnButton != null)
            {
                View.EndPlayerTurnButton.OnClickAsObservable().Subscribe(_ => BattleSystem.EndPlayerTurn())
                    .AddTo(_disposables);
            }

            if (View.OpenMapPreviewButton != null)
            {
                View.OpenMapPreviewButton.OnClickAsObservable()
                    .Subscribe(_ => UIManager.OpenAsync<MapPreviewPopup>().Forget())
                    .AddTo(_disposables);
            }

            if (View.OpenDrawPileButton != null)
            {
                View.OpenDrawPileButton.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        DeckInspectPopup.PendingTab = DeckInspectTab.DrawPile;
                        UIManager.OpenAsync<DeckInspectPopup>().Forget();
                    })
                    .AddTo(_disposables);
            }

            if (View.OpenDiscardPileButton != null)
            {
                View.OpenDiscardPileButton.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        DeckInspectPopup.PendingTab = DeckInspectTab.DiscardPile;
                        UIManager.OpenAsync<DeckInspectPopup>().Forget();
                    })
                    .AddTo(_disposables);
            }
        }

        public override void OnClose()
        {
            _disposables?.Dispose();
            _disposables = null;
        }
    }
}
