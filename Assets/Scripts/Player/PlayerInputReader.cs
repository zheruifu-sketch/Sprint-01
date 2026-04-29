using UnityEngine;
using Nenn.InspectorEnhancements.Runtime.Attributes;

[DisallowMultipleComponent]
public class PlayerInputReader : MonoBehaviour
{
    [LabelText("跑局控制器")]
    [SerializeField] private GameSessionController sessionController;

    public float HorizontalInput { get; private set; }
    public float VerticalInput { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool JumpPressedThisFrame { get; private set; }
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
            SprintHeld = false;
            return;
        }

        HorizontalInput = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            HorizontalInput -= 1f;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            HorizontalInput += 1f;
        }

        VerticalInput = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            VerticalInput += 1f;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            VerticalInput -= 1f;
        }

        SprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        JumpPressedThisFrame = Input.GetKeyDown(KeyCode.Space);

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            RequestedFormThisFrame = PlayerFormType.Human;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            RequestedFormThisFrame = PlayerFormType.Car;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            RequestedFormThisFrame = PlayerFormType.Plane;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            RequestedFormThisFrame = PlayerFormType.Boat;
        }
    }
}
