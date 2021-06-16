using UnityEngine;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    /// <summary>
    /// 起動時
    /// </summary>
    private void Awake()
    {
        InitMove();
        InitCamera();
    }

    /// <summary>
    /// 更新
    /// </summary>
    private void Update()
    {
        UpdateMove();
        UpdatePinch();
    }

    /// <summary>
    /// 渡された角度を指定の分割数で補正
    /// </summary>
    private float CorrectAngle(float dragAngle, int sepCount = 8)
    {
        var sepAngle = 360f / sepCount;
        var sepAngleHalf = sepAngle / 2f;
        var ret = Mathf.Floor((Mathf.Abs(dragAngle) + sepAngleHalf) / sepAngle) * sepAngle * Mathf.Sign(dragAngle);
        return ret;
    }

    private void OnValidate()
    {
        _controller.SetDragMaxDistance(_dragMaxDistance);
    }

    //////////////////////////////
    /// 移動操作
    //////////////////////////////
    #region Move
    /// <summary> I○○○Handlerを継承したタッチ領域 </summary>
    [SerializeField]
    private DragHandler _dragHandler;

    [SerializeField]
    private OctagonControllerView _controller;

    /// <summary> カメラのTransform </summary>
    [SerializeField]
    private Transform _camera;

    /// <summary> ドラッグ操作の起点 </summary>
    private Vector2 _basePosition;

    /// <summary> ドラッグ操作のタッチ座標 </summary>
    private Vector2 _dragPosition;

    /// <summary> 最大移動速度(m/秒) </summary>
    [SerializeField]
    private float _speed = 3f;

    /// <summary> 最大回転速度(°/秒) </summary>
    [SerializeField]
    private float _angularVelocity = 360f;

    /// <summary> 最大移動速度に必要なドラッグ量(pixel) </summary>
    [SerializeField]
    private int _dragMaxDistance = 180;

    private bool _isDrag;

    /// <summary>
    /// 移動操作初期化
    /// </summary>
    private void InitMove()
    {
        _dragHandler.OnPointerDownEvent = OnPointerDown;
        _dragHandler.OnBeginDragEvent = OnBeginDrag;
        _dragHandler.OnDragEvent = OnDrag;
        _dragHandler.OnEndDragEvent = OnEndDrag;
        _controller.SetDragMaxDistance(_dragMaxDistance);
    }

    /// <summary>
    /// 移動操作領域タッチ時
    /// </summary>
    private void OnPointerDown(PointerEventData e)
    {
        _basePosition = _dragPosition = _controller.GetLocalPoint(e.position);
    }

    /// <summary>
    /// 移動操作領域ドラッグ開始時
    /// </summary>
    private void OnBeginDrag(PointerEventData e)
    {
        if (IsPinch)
        {
            return;
        }
        _dragPosition = _controller.GetLocalPoint(e.position);
        _controller.Begin(_dragPosition, _basePosition);
        _isDrag = true;
    }

    /// <summary>
    /// 移動操作領域ドラッグ時
    /// </summary>
    private void OnDrag(PointerEventData e)
    {
        if (IsPinch)
        {
            return;
        }
        _dragPosition = _controller.GetLocalPoint(e.position);

        var dragVector = _dragPosition - _basePosition;
        var dragDistance = dragVector.magnitude;

        // 大きくドラッグ操作したとき、反対方向へすぐ入力が効くように起点とドラッグ座標が一定距離以上離れないようにする
        if (dragDistance > _dragMaxDistance)
        {
            _basePosition += dragVector.normalized * (dragDistance - _dragMaxDistance);
        }

        _controller.SetDragVector(dragVector, _basePosition);
    }

    /// <summary>
    /// 移動操作領域ドラッグ終了時
    /// </summary>
    private void OnEndDrag(PointerEventData e)
    {
        if (IsPinch)
        {
            return;
        }
        _basePosition = _dragPosition = Vector2.zero;
        _controller.End();
        _isDrag = false;
    }

    private void UpdateMove()
    {
        if(_basePosition == _dragPosition)
        {
            // 入力がない時は何もしない
            return;
        }

        var dragVector = _dragPosition - _basePosition;
        float dragDistance = dragVector.magnitude;

        // 操作量が一定以上のときのみ補正を有効にする
        if (dragDistance >= _dragMaxDistance - 10)
        {
            // ドラッグ入力ベクトルの向きを8方向に補正する
            float dragAngle = Vector2.Angle(Vector2.up, dragVector) * Mathf.Sign(-dragVector.x);
            dragAngle = CorrectAngle(dragAngle);
            dragVector = Quaternion.Euler(0f, 0f, dragAngle) * Vector2.up;
        }

        // 入力量に応じて移動速度を変える
        var speedRate = Mathf.Clamp01(dragDistance / _dragMaxDistance);
        dragVector = dragVector.normalized * Mathf.Lerp(0f, _speed, speedRate);

        // カメラの現在角度を加味して、移動方向をX,Z軸基準に補正する（＝グリッドに沿わせる）
        var correctedCameraAngle = CorrectAngle(_camera.eulerAngles.y);
        var moveVector = Quaternion.Euler(0f, correctedCameraAngle, 0f) * new Vector3(dragVector.x, 0f, dragVector.y) * Time.deltaTime;
        transform.position += moveVector;

        // 移動方向へTrasformの向きを徐々に変える
        var eulerAngles = transform.eulerAngles;
        var moveAngles = Quaternion.LookRotation(moveVector).eulerAngles;
        var angleDiff = Mathf.DeltaAngle(eulerAngles.y, moveAngles.y);
        var angularVelocity = Mathf.Lerp(0f, _angularVelocity, Mathf.Clamp01(Mathf.Abs(angleDiff) / 90f));
        eulerAngles.y += Mathf.Min(angularVelocity * Time.deltaTime, Mathf.Abs(angleDiff)) * Mathf.Sign(angleDiff);
        transform.eulerAngles = eulerAngles;

        _controller.SetAngleAxisOut(correctedCameraAngle - transform.eulerAngles.y);
    }
    #endregion

    //////////////////////////////
    /// カメラ回転操作
    //////////////////////////////
    #region CameraRoll
    /// <summary> I○○○Handlerを継承したタッチ領域（カメラ操作） </summary>
    [SerializeField]
    private DragHandler _cameraDragHandler;

    /// <summary> カメラ制御 </summary>
    [SerializeField]
    private CameraController _cameraCtr;

    /// <summary> カメラ1°回転に必要な操作量(pixel) </summary>
    [SerializeField]
    private int _pixelPerCameraRoll = 10;

    /// <summary> 前回のカメラ回転ドラッグ操作座標 </summary>
    private Vector2 _preDragPositionCamera;

    private bool _isDragCamera;

    /// <summary>
    /// カメラ操作初期化
    /// </summary>
    private void InitCamera()
    {
        _cameraDragHandler.OnBeginDragEvent = OnBeginDragCamera;
        _cameraDragHandler.OnDragEvent = OnDragCamera;
        _cameraDragHandler.OnEndDragEvent = OnEndDragCamera;
        _cameraCtr.SetTrackTarget(transform);
    }

    /// <summary>
    /// カメラ操作領域タッチ開始時
    /// </summary>
    private void OnBeginDragCamera(PointerEventData e)
    {
        if (IsPinch)
        {
            return;
        }
        _preDragPositionCamera = _controller.GetLocalPoint(e.position);
        _isDragCamera = true;
    }

    /// <summary>
    /// カメラ操作領域タッチ時
    /// </summary>
    private void OnDragCamera(PointerEventData e)
    {
        if (IsPinch)
        {
            return;
        }
        var dragPosition = _controller.GetLocalPoint(e.position);
        var dragVector = dragPosition - _preDragPositionCamera;
        var deltaRotate = new Vector2(-dragVector.y, dragVector.x) / _pixelPerCameraRoll;
        _cameraCtr.SetRotate(deltaRotate);
        _preDragPositionCamera = dragPosition;
    }

    /// <summary>
    /// カメラ操作領域タッチ終了時
    /// </summary>
    private void OnEndDragCamera(PointerEventData e)
    {
        if (IsPinch)
        {
            return;
        }
        var correctedCameraAngle = CorrectAngle(_cameraCtr.Angles.y);
        // カメラ角度を8方向に補正
        _cameraCtr.AlignAngleY(correctedCameraAngle);
        _isDragCamera = false;
    }
    #endregion

    //////////////////////////////
    /// カメラズーム操作
    //////////////////////////////
    #region CameraZoom
    [SerializeField]
    private int _pixelPerCameraDistance = 20;

    /// <summary> 前フレームのピンチ操作距離 </summary>
    private float _prePinchDistance;

    private bool IsPinch => _prePinchDistance >= 0;

#if UNITY_EDITOR
    /// <summary> ピンチ操作の起点 </summary>
    private Vector2 _pinchBeginPosition;
#endif

    private void UpdatePinch()
    {
        // ドラッグ操作中はピンチ操作をしない
        if (_isDrag || _isDragCamera)
        {
            _prePinchDistance = -1;
            return;
        }

        var pinchDelta = 0f;

#if UNITY_EDITOR
        // Pキーが押されていなければピンチ操作をしない
        if (!Input.GetKey(KeyCode.P))
        {
            _prePinchDistance = -1;
            return;
        }

        var mouseLocalPos = _controller.GetLocalPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            _pinchBeginPosition = _controller.GetLocalPoint(new Vector2(Screen.width / 2, Screen.height / 2));
            _prePinchDistance = Vector2.Distance(_pinchBeginPosition, mouseLocalPos);
        }

        // マウスがクリックされていなければピンチ操作をしない
        if (!Input.GetMouseButton(0))
        {
            _prePinchDistance = -1;
            return;
        }

        var pinchDistance = Vector2.Distance(_pinchBeginPosition, mouseLocalPos);
        pinchDelta = (pinchDistance - _prePinchDistance) / _pixelPerCameraDistance;
        _prePinchDistance = pinchDistance;
#else
        if (Input.touchCount < 2)
        {
            _prePinchDistance = -1;
            return;
        }
        var touch0 = _controller.GetLocalPoint(Input.GetTouch(0).position);
        var touch1 = _controller.GetLocalPoint(Input.GetTouch(1).position);
        var pinchDistance = Vector3.Distance(touch0, touch1);

        if (Input.GetTouch(1).phase == TouchPhase.Began)
        {
            _prePinchDistance = pinchDistance;
        }

        pinchDelta = (pinchDistance - _prePinchDistance) / _pixelPerCameraDistance;
        _prePinchDistance = pinchDistance;
#endif

        _cameraCtr.SetDistance(pinchDelta);
    }
    #endregion
}