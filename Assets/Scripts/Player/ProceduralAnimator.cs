using UnityEngine;

/// Drives primitive-mesh limbs with procedural animations based on PlayerController state.
public class ProceduralAnimator : MonoBehaviour
{
    [Header("Limb References")]
    public Transform body;
    public Transform head;
    public Transform leftArm;
    public Transform rightArm;
    public Transform leftLeg;
    public Transform rightLeg;

    [Header("Settings")]
    public float limbSwingAmount = 32f;
    public float runSwingMultiplier = 1.6f;
    public float bobAmplitude = 0.04f;

    private PlayerController pc;
    private float limbTimer;
    private float idleTimer;
    private Vector3 bodyRestPos;
    private Quaternion bodyRestRot;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        if (body)
        {
            bodyRestPos = body.localPosition;
            bodyRestRot = body.localRotation;
        }
    }

    void Update()
    {
        if (pc == null) return;

        switch (pc.CurrentState)
        {
            case PlayerController.PlayerState.Idle:       AnimateIdle();             break;
            case PlayerController.PlayerState.Walking:    AnimateWalk(1f, false);    break;
            case PlayerController.PlayerState.CrouchWalking: AnimateWalk(0.7f, true); break;
            case PlayerController.PlayerState.Running:    AnimateRun();              break;
            case PlayerController.PlayerState.Jumping:    AnimateJump();             break;
            case PlayerController.PlayerState.Crouching:  AnimateCrouch();           break;
        }
    }

    void AnimateIdle()
    {
        idleTimer += Time.deltaTime * 1.2f;
        float breathe = Mathf.Sin(idleTimer) * 0.008f;

        if (body)
        {
            body.localPosition = Vector3.Lerp(body.localPosition, bodyRestPos + Vector3.up * breathe, Time.deltaTime * 8f);
            body.localRotation = Quaternion.Slerp(body.localRotation, bodyRestRot, Time.deltaTime * 8f);
        }
        if (head)
            head.localRotation = Quaternion.Slerp(head.localRotation,
                Quaternion.Euler(Mathf.Sin(idleTimer * 0.4f) * 3f, 0f, 0f), Time.deltaTime * 3f);

        ReturnLimbs(4f);
    }

    void AnimateWalk(float speedMult, bool crouching)
    {
        limbTimer += Time.deltaTime * 7f * speedMult;
        float swing = Mathf.Sin(limbTimer) * limbSwingAmount * speedMult;
        float bob   = Mathf.Abs(Mathf.Sin(limbTimer)) * bobAmplitude;

        if (body)
        {
            Vector3 target = bodyRestPos + Vector3.up * (bob + (crouching ? -0.28f : 0f));
            body.localPosition = Vector3.Lerp(body.localPosition, target, Time.deltaTime * 12f);
            Quaternion targetRot = crouching ? Quaternion.Euler(18f, 0f, 0f) : bodyRestRot;
            body.localRotation = Quaternion.Slerp(body.localRotation, targetRot, Time.deltaTime * 10f);
        }

        SetLimbRotations(swing, 0f);
    }

    void AnimateRun()
    {
        limbTimer += Time.deltaTime * 11f;
        float swing = Mathf.Sin(limbTimer) * limbSwingAmount * runSwingMultiplier;
        float bob   = Mathf.Abs(Mathf.Sin(limbTimer)) * bobAmplitude * 2.2f;

        if (body)
        {
            Vector3 target = bodyRestPos + Vector3.up * bob + Vector3.forward * 0.06f;
            body.localPosition = Vector3.Lerp(body.localPosition, target, Time.deltaTime * 12f);
            body.localRotation = Quaternion.Slerp(body.localRotation, Quaternion.Euler(12f, 0f, 0f), Time.deltaTime * 10f);
        }

        SetLimbRotations(swing, 12f);
    }

    void AnimateJump()
    {
        float t = Time.deltaTime * 9f;
        if (body) body.localRotation = Quaternion.Slerp(body.localRotation, Quaternion.Euler(-12f, 0f, 0f), t);
        if (leftLeg)  leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation,  Quaternion.Euler(-35f, 0f,  8f), t);
        if (rightLeg) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(-35f, 0f, -8f), t);
        if (leftArm)  leftArm.localRotation  = Quaternion.Slerp(leftArm.localRotation,  Quaternion.Euler(-50f, 0f, -18f), t);
        if (rightArm) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler(-50f, 0f,  18f), t);
    }

    void AnimateCrouch()
    {
        float t = Time.deltaTime * 10f;
        if (body)
        {
            body.localPosition = Vector3.Lerp(body.localPosition, bodyRestPos + Vector3.down * 0.3f, t);
            body.localRotation = Quaternion.Slerp(body.localRotation, Quaternion.Euler(22f, 0f, 0f), t);
        }
        if (leftLeg)  leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation,  Quaternion.Euler(68f, 0f,  14f), t);
        if (rightLeg) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(68f, 0f, -14f), t);
        ReturnArms(t);
    }

    void SetLimbRotations(float swing, float armSplay)
    {
        float t = Time.deltaTime * 12f;
        if (leftLeg)  leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation,  Quaternion.Euler( swing, 0f, 0f), t);
        if (rightLeg) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.Euler(-swing, 0f, 0f), t);
        if (leftArm)  leftArm.localRotation  = Quaternion.Slerp(leftArm.localRotation,  Quaternion.Euler(-swing * 0.6f, 0f, -armSplay), t);
        if (rightArm) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.Euler( swing * 0.6f, 0f,  armSplay), t);
    }

    void ReturnLimbs(float speed)
    {
        float t = Time.deltaTime * speed;
        if (leftLeg)  leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation,  Quaternion.identity, t);
        if (rightLeg) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.identity, t);
        ReturnArms(t);
    }

    void ReturnArms(float t)
    {
        if (leftArm)  leftArm.localRotation  = Quaternion.Slerp(leftArm.localRotation,  Quaternion.identity, t);
        if (rightArm) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.identity, t);
    }
}
