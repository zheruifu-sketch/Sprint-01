using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerFormRoot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D playerRigidbody;
    [SerializeField] private PlayerFormView formView;

    [Header("State")]
    [SerializeField] private PlayerFormType startingForm = PlayerFormType.Human;

    public PlayerFormType CurrentForm { get; private set; } = PlayerFormType.Human;
    public Rigidbody2D PlayerRigidbody => playerRigidbody;
    public PlayerFormView FormView => formView;

    private bool hasFacingDirection;
    private bool facingLeft;

    private void Reset()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
        formView = GetComponentInChildren<PlayerFormView>(true);
    }

    private void Awake()
    {
        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody2D>();
        }

        CurrentForm = startingForm;
        if (formView != null)
        {
            formView.ShowForm(CurrentForm);
        }
    }

    public void SetForm(PlayerFormType nextForm)
    {
        CurrentForm = nextForm;
        if (formView != null)
        {
            formView.ShowForm(CurrentForm);
            formView.SetRunState(CurrentForm, false);

            if (hasFacingDirection)
            {
                formView.SetFacing(facingLeft);
            }
        }
    }

    public void SetFacingFromHorizontal(float horizontalInput)
    {
        if (formView == null || Mathf.Approximately(horizontalInput, 0f))
        {
            return;
        }

        facingLeft = horizontalInput < 0f;
        hasFacingDirection = true;
        formView.SetFacing(facingLeft);
    }

    public void SetRunState(bool isRunning)
    {
        if (formView == null)
        {
            return;
        }

        formView.SetRunState(CurrentForm, isRunning);
    }
}
