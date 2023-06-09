using Redirection;
using UnityEngine;


public class S2ORedirector : SteerToRedirector
{
    public float S2OTargetRadius = 5.0f; //Target orbit radius for Steer-to-Orbit algorithm (meters)


    private const float S2OTargetGenerationAngleInDegrees = 60;


    public override void PickRedirectionTarget()
    {
        var trackingAreaPosition = Utilities.FlattenedPos3D(RedirectionManager.trackedSpace.position);
        var userToCenter = trackingAreaPosition - RedirectionManager.currPos;

        //Compute steering target for S2O
        if (NoTmpTarget)
        {
            TMPTarget = new GameObject("S2O Target");
            CurrentTarget = TMPTarget.transform;
            NoTmpTarget = false;
        }

        //Step One: Compute angles for direction from center to potential targets
        float alpha;

        //Where is user relative to desired orbit?
        if (userToCenter.magnitude < S2OTargetRadius) //Inside the orbit
        {
            alpha = S2OTargetGenerationAngleInDegrees;
        }
        else
        {
            //Use tangents of desired orbit
            alpha = Mathf.Acos(S2OTargetRadius / userToCenter.magnitude) * Mathf.Rad2Deg;
        }

        //Step Two: Find directions to two petential target positions
        var dir1 = Quaternion.Euler(0, alpha, 0) * -userToCenter.normalized;
        var targetPosition1 = trackingAreaPosition + S2OTargetRadius * dir1;
        var dir2 = Quaternion.Euler(0, -alpha, 0) * -userToCenter.normalized;
        var targetPosition2 = trackingAreaPosition + S2OTargetRadius * dir2;

        //Step Three: Evaluate difference in direction
        // We don't care about angle sign here
        var angle1 = Vector3.Angle(RedirectionManager.currDir, targetPosition1 - RedirectionManager.currPos);
        var angle2 = Vector3.Angle(RedirectionManager.currDir, targetPosition2 - RedirectionManager.currPos);

        CurrentTarget.transform.position = angle1 <= angle2 ? targetPosition1 : targetPosition2;
    }
}