using UnityEngine;
using UnityEngine.UI;
using LitMotion;

//그저 연출용 스크립트
public class SpinIcon : MonoBehaviour
{
    [SerializeField] private float _spinDuration = 2f;

    private Transform _spinTarget;
    private MotionHandle _spinHandle;

    private void OnDestroy()
    {
        StopIconSpin();
    }

    public void Init()
    {
        var image = GetComponentInChildren<Image>();
        _spinTarget = image != null ? image.transform : transform;

        StartIconSpin();
    }

    private void StartIconSpin()
    {
        StopIconSpin();

        var baseEuler = _spinTarget.localEulerAngles;
        _spinHandle = LMotion.Create(0f, 360f, _spinDuration)
            .WithEase(Ease.Linear)
            .WithLoops(-1, LoopType.Restart)
            .Bind(y => _spinTarget.localEulerAngles = new Vector3(baseEuler.x, baseEuler.y + y, baseEuler.z));
    }

    /// <summary>
    /// 아이콘 Y축 회전 연출을 정지한다.
    /// </summary>
    public void StopIconSpin()
    {
        if (_spinHandle.IsActive())
            _spinHandle.Cancel();
    }
}
