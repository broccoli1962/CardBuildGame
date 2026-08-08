using Backend.Object.GameSystems.Gameplay;
using Backend.Object.Management;
using R3;

namespace Backend.Object.UI
{
    public class MapPanelPresenter : UIPresenter<MapPanel>
    {
        private CompositeDisposable _disposables;

        public override void OnOpen()
        {
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();

            if (View == null)
                return;

            View.GraphView?.SetInteractable(true);

            MapSystem.OnMapChanged.Subscribe(_ => Refresh()).AddTo(_disposables);
            MapSystem.Nodes.Subscribe(_ => Refresh()).AddTo(_disposables);
            PlayerStateSystem.Hp.Subscribe(_ => RefreshStatus()).AddTo(_disposables);
            PlayerStateSystem.Mana.Subscribe(_ => RefreshStatus()).AddTo(_disposables);

            if (View.EnterButton != null)
            {
                View.EnterButton.OnClickAsObservable()
                    .Subscribe(_ => MapSystem.ConfirmEnter())
                    .AddTo(_disposables);
            }

            if (View.ClearSelectionButton != null)
            {
                View.ClearSelectionButton.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        MapSystem.ClearSelection();
                        RefreshDetail();
                    })
                    .AddTo(_disposables);
            }

            Refresh();
        }

        public override void OnClose()
        {
            _disposables?.Dispose();
            _disposables = null;
        }

        private void Refresh()
        {
            if (View == null)
                return;

            View.GraphView?.Bind(MapSystem.Nodes.CurrentValue, OnNodeClicked);
            View.SetHeader($"CHAPTER {MapSystem.Chapter.CurrentValue} · FLOOR {MapSystem.CurrentFloor.CurrentValue} / {TableManager.GetMapFloorCount(MapSystem.Chapter.CurrentValue)}");
            RefreshStatus();
            RefreshDetail();
        }

        private void RefreshStatus()
        {
            if (View == null)
                return;

            var tier = MapSystem.ThreatTier;
            var threat = tier > 0 ? $"T{tier}" : "T0";
            View.SetStatus(
                $"HP {PlayerStateSystem.Hp.CurrentValue}/{PlayerStateSystem.MaxHp.CurrentValue}  ·  MP {PlayerStateSystem.Mana.CurrentValue}/{PlayerStateSystem.MaxMana.CurrentValue}  ·  {threat}");
        }

        private void RefreshDetail()
        {
            if (View == null)
                return;

            if (!MapSystem.TryGetSelectedNode(out var node))
            {
                View.SetDetail(string.Empty, false);
                View.SetEnterInteractable(false, "진입");
                return;
            }

            View.SetDetail(BuildDetail(node), true);
            View.SetEnterInteractable(true, GetEnterLabel(node.NodeType));
        }

        private void OnNodeClicked(int floor, int slot)
        {
            if (!MapSystem.TrySelectNode(floor, slot))
            {
                if (MapSystem.TryGetNode(floor, slot, out var node) && node.State == MapNodeState.Locked)
                    View.SetDetail("현재 위치에서 갈 수 없는 길입니다", true);
                return;
            }

            RefreshDetail();
        }

        private static string BuildDetail(MapNode node)
        {
            var typeData = TableManager.GetMapNodeType(node.NodeType);
            var title = typeData != null ? typeData.name_key.GetLocalizeText() : node.NodeType.ToString();

            // Combat nodes: node type + reward only. Do not reveal the upcoming monster.
            return node.NodeType switch
            {
                MapNodeType.Battle => $"{title}\n보상: 카드 생성 기회",
                MapNodeType.Elite => $"{title}\n보상: 후보 2장 중 1장 · 최대 체력 +2",
                MapNodeType.Boss => $"{title}\n챕터 보스전",
                MapNodeType.Rest => $"{title}\n최대 체력의 절반 회복",
                MapNodeType.Event => $"{title}\n무슨 일이 일어날지 알 수 없다",
                MapNodeType.Treasure => $"{title}\n체력 또는 마나 영구 강화 1택",
                _ => title,
            };
        }

        private static string GetEnterLabel(MapNodeType type) => type switch
        {
            MapNodeType.Elite => "정예와 교전",
            MapNodeType.Boss => "보스와 교전",
            _ => "진입",
        };
    }
}
