using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterCollider : MonoBehaviour
{
    private BoxCollider2D _col = null;
    private Bounds _characterBounds => _col.bounds;
    [SerializeField]
    private LayerMask _groundLayer = 0;
    [SerializeField]
    private int _detectorCount = 3;
    [SerializeField]
    private float _detectionRayLength = 0f;
    [SerializeField, Range(0f, 1f)]
    private float _rayBuffer = 0.1f;

    private RayRange _raysUp, _raysRight, _raysDown, _raysLeft;
    private bool _colUp, _colRight, _colDown, _colLeft;

    private bool _landingThisFrame = false;
    private float _timeLeftGrounded = 0f;

    // coyoteTime은 땅을 벗어났을 때 점프할 수 있는 것이 유지되는 시간
    private bool _coyoteUseable = false;
    [SerializeField]
    private float _coyoteTime = 0.05f;
    private float _coyoteTimer = 0f;

    private void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        CheckCollision();
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (_col == null)
        {
            _col = GetComponent<BoxCollider2D>();
        }
        if (_col == null)
        {
            return;
        }
        CalculateRayRanged();
#endif
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + (Vector3)_col.offset, _col.size);
        ShootDebugRay(_raysUp);
        ShootDebugRay(_raysDown);
        ShootDebugRay(_raysLeft);
        ShootDebugRay(_raysRight);
    }

    /// <summary>
    /// boundType 방향으로 충돌이 되었는지 검사합니다. collisionUpdate를 true로 설정하면 충돌 재검사를 시행합니다.
    /// </summary>
    /// <param name="asd"></param>
    public bool GetCollision(EBoundType boundType, bool collisionUpdate)
    {
        if(collisionUpdate)
        {
            CheckCollision();
        }
        switch (boundType)
        {
            case EBoundType.None:
                break;
            case EBoundType.Up:
                return _colUp;
            case EBoundType.Down:
                return _colDown;
            case EBoundType.Left:
                return _colLeft;
            case EBoundType.Right:
                return _colRight;
            default:
                break;
        }
        return false;
    }

    private void CheckCollision()
    {
        // 상,하,좌,우 RayRange 계산
        CalculateRayRanged();

        // Raycast 쏘기
        var groundedCheck = CheckDetection(_raysDown);
        _landingThisFrame = false;
        // 저번 프레임에 땅에 닿았고, 이번 프레임에 땅에서 나왔을 때
        if(_colDown && !groundedCheck)
        {
            _timeLeftGrounded = Time.time;
        }
        // 저번 프레임에 공중에 있었고, 이번 프레임에 땅에 닿았을 때
        else if (!_colDown && groundedCheck)
        {
            _coyoteUseable = true;
            _landingThisFrame = true;
        }
        _colUp = CheckDetection(_raysUp);
        _colLeft = CheckDetection(_raysLeft);
        _colRight = CheckDetection(_raysRight);
    }

    private bool CheckDetection(RayRange range)
    {
        // EvaluateRayPositions 함수로 추출된 위치에서 range의 Dir 방향으로 Ray를 쏴 맞은 것이 있다면 true 반환
        return EvaluateRayPositions(range).Any(point => Physics2D.Raycast(point, range.Dir, _detectionRayLength, _groundLayer));
    }

    private void ShootDebugRay(RayRange range)
    {
        IEnumerable<Vector2> rayStarts = EvaluateRayPositions(range);
        RaycastHit2D hit;
        foreach (var ray in rayStarts)
        {
            hit = Physics2D.Raycast(ray, range.Dir, _detectionRayLength, _groundLayer);
            if (hit)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(ray, hit.point);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(ray, ray + range.Dir * _detectionRayLength);
            }
        }
    }

    private void CalculateRayRanged()
    {
        // RayRange의 초기화를 진행함
        var b = new Bounds(_characterBounds.center, _characterBounds.size);

        _raysDown = new RayRange(b.min.x + _rayBuffer, b.min.y, b.max.x - _rayBuffer, b.min.y, Vector2.down);
        _raysUp = new RayRange(b.min.x + _rayBuffer, b.max.y, b.max.x - _rayBuffer, b.max.y, Vector2.up);
        _raysLeft = new RayRange(b.min.x, b.min.y + _rayBuffer, b.min.x, b.max.y - _rayBuffer, Vector2.left);
        _raysRight = new RayRange(b.max.x, b.min.y + _rayBuffer, b.max.x, b.max.y - _rayBuffer, Vector2.right);
    }


    private IEnumerable<Vector2> EvaluateRayPositions(RayRange range)
    {
        // range의 Start부터 End 위치까지의 위치들을 _detectorCount의 수만큼 등분하여 반환시킴
        for (var i = 0; i < _detectorCount; i++)
        {
            var t = (float)i / (_detectorCount - 1);
            yield return Vector2.Lerp(range.Start, range.End, t);
        }
    }
}