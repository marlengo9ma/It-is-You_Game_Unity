using UnityEngine;

namespace ItIsYou.UI
{
    /// <summary>
    /// 마우스 커서 상태.
    ///
    /// 기획서: 상호작용 가능한 오브젝트 위에서는 "기존-펜촉 &gt; 깜빡이는 눈의 형태"로 바뀌고,
    /// 심문 중 리액션이 나타났을 때 용의자 위에서도 커서가 일시적으로 바뀐다.
    ///
    /// 커서가 이 게임에서는 "지금 볼 수 있다"는 유일한 신호라, 상태를 한 곳에서 관리한다.
    /// 여러 곳에서 동시에 요청이 들어올 수 있으므로 우선순위를 둔다.
    /// </summary>
    [AddComponentMenu("It is You/Cursor Controller")]
    public class CursorController : MonoBehaviour
    {
        public enum CursorState
        {
            /// <summary>기본 상태 — 만년필 펜촉.</summary>
            Pen = 0,
            /// <summary>상호작용 가능 — 깜빡이는 눈.</summary>
            Eye = 1,
            /// <summary>리액션 수집 가능 — 가장 강한 신호.</summary>
            Reaction = 2
        }

        [Header("커서 이미지")]
        [SerializeField] private Texture2D penCursor;
        [SerializeField] private Texture2D eyeCursor;
        [SerializeField] private Texture2D reactionCursor;

        [Header("핫스팟")]
        [SerializeField] private Vector2 penHotspot = Vector2.zero;
        [SerializeField] private Vector2 eyeHotspot = new Vector2(16f, 16f);
        [SerializeField] private Vector2 reactionHotspot = new Vector2(16f, 16f);

        [Header("깜빡임")]
        [Tooltip("눈 커서가 깜빡이는 주기(초). 0이면 깜빡이지 않는다.")]
        [SerializeField] private float blinkInterval = 0.6f;

        [SerializeField] private Texture2D eyeCursorBlink;

        private CursorState _state = CursorState.Pen;
        private bool _hoverInteractable;
        private bool _reactionAvailable;
        private float _blinkTimer;
        private bool _blinkOn;

        public CursorState State => _state;

        private void Start() => Apply(true);

        /// <summary>상호작용 가능한 대상 위에 있는지.</summary>
        public void SetHoveringInteractable(bool value)
        {
            if (_hoverInteractable == value) return;
            _hoverInteractable = value;
            Resolve();
        }

        /// <summary>리액션 수집 창이 열려 있고 커서가 그 대상 위에 있는지.</summary>
        public void SetReactionAvailable(bool value)
        {
            if (_reactionAvailable == value) return;
            _reactionAvailable = value;
            Resolve();
        }

        /// <summary>페이즈 전환 등에서 모든 상태를 내린다.</summary>
        public void ResetState()
        {
            _hoverInteractable = false;
            _reactionAvailable = false;
            Resolve();
        }

        private void Resolve()
        {
            // 리액션이 가장 강한 신호다. 놓치면 되돌릴 수 없는 수집 기회라서.
            var next = _reactionAvailable ? CursorState.Reaction
                : _hoverInteractable ? CursorState.Eye
                : CursorState.Pen;

            if (next == _state) return;
            _state = next;
            _blinkTimer = 0f;
            _blinkOn = false;
            Apply(true);
        }

        private void Update()
        {
            if (_state != CursorState.Eye || blinkInterval <= 0f || eyeCursorBlink == null) return;

            _blinkTimer += Time.unscaledDeltaTime;
            if (_blinkTimer < blinkInterval) return;

            _blinkTimer = 0f;
            _blinkOn = !_blinkOn;
            Apply(false);
        }

        private void Apply(bool force)
        {
            switch (_state)
            {
                case CursorState.Reaction:
                    SetCursor(reactionCursor, reactionHotspot);
                    break;
                case CursorState.Eye:
                    var tex = (_blinkOn && eyeCursorBlink != null) ? eyeCursorBlink : eyeCursor;
                    SetCursor(tex, eyeHotspot);
                    break;
                default:
                    SetCursor(penCursor, penHotspot);
                    break;
            }
        }

        private static void SetCursor(Texture2D texture, Vector2 hotspot)
        {
            // 텍스처가 아직 없어도 게임은 돌아가야 한다. 기본 커서로 떨어진다.
            Cursor.SetCursor(texture, texture != null ? hotspot : Vector2.zero, CursorMode.Auto);
        }
    }
}
