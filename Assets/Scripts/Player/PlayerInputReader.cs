using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class PlayerInputReader : MonoBehaviour
{
    [LabelText("跑局控制器")]
    [SerializeField] private GameSessionController sessionController;

    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }
    public bool IsSprintHeld { get; private set; }
    public bool JumpPressedThisFrame { get; private set; }
    public bool JumpHeld { get; private set; }
    public PlayerFormType? RequestedFormThisFrame { get; private set; }

    private void Reset()
    {
        sessionController = FindObjectOfType<GameSessionController>();
    }

    private void Awake()
    {
        if (sessionController == null)
        {
            sessionController = FindObjectOfType<GameSessionController>();
        }
    }

    private void Update()
    {
        JumpPressedThisFrame = false;
        RequestedFormThisFrame = null;

        if (sessionController == null || !sessionController.CanReceiveGameplayInput)
        {
            HorizontalInput = 0f;
            VerticalInput = 0f;
            IsSprintHeld = false;
            JumpHeld = false;
            return;
        }

        bool moveLeftHeld = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool moveRightHeld = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        HorizontalInput = moveLeftHeld == moveRightHeld
            ? 0f
            : (moveRightHeld ? 1f : -1f);
        IsSprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        VerticalInput = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            VerticalInput += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            VerticalInput -= 1f;
        }

        JumpPressedThisFrame = Input.GetKeyDown(KeyCode.Space);
        JumpHeld = Input.GetKey(KeyCode.Space);

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            RequestedFormThisFrame = PlayerFormType.Human;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            RequestedFormThisFrame = PlayerFormType.Car;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            RequestedFormThisFrame = PlayerFormType.Plane;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            RequestedFormThisFrame = PlayerFormType.Boat;
        }
    }
}
