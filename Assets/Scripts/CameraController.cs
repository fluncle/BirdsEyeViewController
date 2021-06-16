using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
	[SerializeField]
	private Transform _distanceT;

	[SerializeField]
	private float _trackSpeed = 4f;

	[SerializeField]
	private float _trackMaxAngleOfView = 5f;

	[SerializeField]
	private float _trackMaxSpeedAngle = 6f;

	/// <summary> 最小X角度 </summary>
	[SerializeField]
	private int minAngleX = 0;

	/// <summary> 最大X角度 </summary>
	[SerializeField]
	private int maxAngleX = 55;

	[SerializeField]
	private int minDistance = 7;

	[SerializeField]
	private int maxDistance = 30;

	private Transform _trackTarget;

	/// <summary> 角度自動補正シーケンス </summary>
	private Sequence _seqRoll;

	public Vector3 Angles => transform.localEulerAngles;

	private float Distance => -_distanceT.localPosition.z;

	public void SetTrackTarget(Transform trackTarget)
	{
		_trackTarget = trackTarget;
	}

	private void Update()
	{
		if (_trackTarget == null)
		{
			return;
		}

		// 真上から投影したときの注視対象と現在注視点の角度差
		var trackTargetAngle = Vector3.Angle(Vector3.down, _trackTarget.position - (transform.position + Vector3.up * Distance));
		if (trackTargetAngle <= _trackMaxAngleOfView)
		{
			// 既に目標視野に入っている
			return;
		}

		var diffAngle = trackTargetAngle - _trackMaxAngleOfView;
		// 目標視野に入れるために必要な移動距離
		var targetDistance = Mathf.Tan(diffAngle * Mathf.Deg2Rad) * Distance;
		var vector = _trackTarget.position - transform.position;
		var deltaDistance = Mathf.Lerp(0f, _trackSpeed, diffAngle / _trackMaxSpeedAngle) * Time.deltaTime;
		transform.position += vector.normalized * Mathf.Min(deltaDistance, targetDistance);
	}

	public void SetDistance(float delta)
	{
		var localPosition = _distanceT.localPosition + Vector3.forward * delta;
		var distance = Mathf.Clamp(-localPosition.z, minDistance, maxDistance);
		localPosition.z = -distance;
		_distanceT.localPosition = localPosition;
	}

	/// <summary>
	/// 角度を指定した分だけ回転させる
	/// </summary>
	/// <param name="delta">変化量</param>
	public void SetRotate(Vector2 delta)
	{
		// 自動補正シーケンスが働いていたら停止
		_seqRoll?.Kill();

		var localEulerAngles = transform.localEulerAngles + (Vector3)delta;
		// パラメータの範囲以上に傾かないように補正
		localEulerAngles.x = Mathf.Clamp(localEulerAngles.x, minAngleX, maxAngleX);
		transform.localEulerAngles = localEulerAngles;
	}

	/// <summary>
	/// 指定したY角度まで自動的に回転
	/// </summary>
	/// <param name="duration">回転にかける時間</param>
	/// <param name="ease">イージングタイプ</param>
	public void AlignAngleY(float targetAngleY, float duration = 0.5f, Ease ease = Ease.OutCubic)
	{
		var targetAngles = transform.localEulerAngles;
		targetAngles.y = targetAngleY;
		_seqRoll?.Kill();
		_seqRoll = DOTween.Sequence();
		_seqRoll.Append(transform.DOLocalRotate(targetAngles, duration).SetEase(ease));
		_seqRoll.Play();
	}
}