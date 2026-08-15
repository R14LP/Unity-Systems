using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    public Animator animator;
    public PlayerMovement playerMovement;

    private void OnEnable()
    {
        playerMovement.OnJump += HandleJump;
    }

    private void OnDisable()
    {
        playerMovement.OnJump -= HandleJump;
    }

    private void Update()
    {
        Vector3 localVel = playerMovement.orientation.InverseTransformDirection(playerMovement.Velocity);
        Vector3 flatLocalVel = new Vector3(localVel.x, 0f, localVel.z);
        float speed = flatLocalVel.magnitude;

        animator.SetFloat("MoveSpeed", speed);

        Vector3 dir = flatLocalVel.normalized;
        animator.SetFloat("VelocityX", dir.x);
        animator.SetFloat("VelocityZ", dir.z);

        animator.SetBool("Grounded", playerMovement.AnimGrounded);
    }
    
    private void HandleJump()
    {
        animator.SetTrigger("Jump");
    }
}
